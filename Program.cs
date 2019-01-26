using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PikaFetcher
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var context = new PikabuContext())
            {
                context.Database.Migrate();
            }

            var api = new PikabuApi();
            await api.Init();

            await Task.WhenAll(Loop1h(api), Loop4h(api), Loop1d(api), Loop2w(api), LoopTop(api));
        }

        private static async Task Loop1h(PikabuApi api)
        {
            await Loop(api, "1h", ConsoleColor.White, TimeSpan.FromHours(1), 0);
        }

        private static async Task Loop4h(PikabuApi api)
        {
            await Loop(api, "4h", ConsoleColor.Gray, TimeSpan.FromHours(4), 50);
        }

        private static async Task Loop1d(PikabuApi api)
        {
            await Loop(api, "1d", ConsoleColor.Gray, TimeSpan.FromDays(1), 200);
        }

        private static async Task Loop2w(PikabuApi api)
        {
            await Loop(api, "2w", ConsoleColor.DarkGray, TimeSpan.FromDays(14), 500);
        }

        private static async Task Loop(PikabuApi api, string label, ConsoleColor color, TimeSpan span, int skip)
        {
            while (true)
            {
                WriteLine(color, $"{label} RESTART");

                var latestStoryId = await api.GetLatestStoryId() - skip;
                await LoopSpan(api, label, latestStoryId, color, span);
            }
        }

        private static async Task LoopSpan(PikabuApi api, string label, int storyId, ConsoleColor color, TimeSpan span)
        {
            var breakLoop = false;
            var r = new Random();
            do
            {
                try
                {
                    var result = await PROCESS_STORY(api, storyId);
                    if (result.Processed)
                    {
                        WriteLine(color, $"{label}\t[{FormatTimeSpan(DateTime.UtcNow - result.TimestampUtc)}] ({storyId}) {result.Rating?.ToString("+0;-#") ?? "?"} ({result.NewCommentsCount}/{result.TotalCommentsCount}) {result.StoryTitle}");
                        breakLoop = (DateTime.UtcNow - result.TimestampUtc) > span;
                        storyId--;
                    }
                    else
                    {
                        storyId -= r.Next(100) + 1;
                    }
                }
                catch (Exception e)
                {
                    WriteLine(ConsoleColor.Red, $"[{label}] {e.Message}");
                    storyId -= r.Next(100) + 1;
                }

                await Task.Delay(1000);

            } while (!breakLoop);
        }

        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.FromMinutes(1))
            {
                return $"{(int) timeSpan.TotalSeconds} sec ago";
            }

            if (timeSpan < TimeSpan.FromHours(1))
            {
                return $"{(int)timeSpan.TotalMinutes} min ago";
            }

            if (timeSpan < TimeSpan.FromHours(48))
            {
                return $"{timeSpan.TotalHours:##.##} hours ago";
            }

            return $"{timeSpan.TotalDays:##.##} days ago";
        }

        private static async Task LoopTop(PikabuApi api)
        {
            int top = 50;
            while (true)
            {
                WriteLine(ConsoleColor.Yellow, $"TOP{top} RESTART");

                int counter = 0;
                Story[] topStories;
                using (var db = new PikabuContext())
                {
                    topStories = await db.Stories
                        .Where(story => story.DateTimeUtc >= DateTime.UtcNow - TimeSpan.FromDays(7))
                        .OrderByDescending(story => story.Rating)
                        .Take(top)
                        .ToArrayAsync();
                }

                if (topStories.Length < top)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                foreach (var story in topStories)
                {
                    counter++;
                    try
                    {
                        var result = await PROCESS_STORY(api, story.StoryId);
                        if (result != null)
                        {
                            WriteLine(ConsoleColor.Yellow, $"TOP{counter}\t[{FormatTimeSpan(DateTime.UtcNow - result.TimestampUtc)}] ({story.StoryId}) {result.Rating?.ToString("+0;-#") ?? "?"} ({result.NewCommentsCount}/{result.TotalCommentsCount}) {result.StoryTitle}");
                        }
                    }
                    catch (Exception e)
                    {
                        WriteLine(ConsoleColor.Red, e.Message);
                    }

                    await Task.Delay(1000);
                }
            }
        }

        static void WriteLine(ConsoleColor color, string text)
        {
            var foregroundColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = foregroundColor;
        }

        private static async Task<StoryProcessingResult> PROCESS_STORY(PikabuApi api, int storyId)
        {
            using (var db = new PikabuContext())
            {
                var scanTime = DateTime.UtcNow;
                var newComments = 0;
                var story = await db.Stories.SingleOrDefaultAsync(s => s.StoryId == storyId);
                if (story == null || DateTime.SpecifyKind(story.LastScanUtc, DateTimeKind.Utc) < scanTime - TimeSpan.FromMinutes(1))
                {
                    var storyComments = await api.GetStoryComments(storyId);
                    if (story == null)
                    {
                        story = new Story
                        {
                            StoryId = storyComments.StoryId,
                            Rating = storyComments.Rating,
                            Title = storyComments.StoryTitle,
                            DateTimeUtc = storyComments.Timestamp.UtcDateTime,
                            Comments = new List<Comment>()
                        };
                        await db.Stories.AddAsync(story);
                    }

                    story.Rating = storyComments.Rating;
                    story.Title = storyComments.StoryTitle;
                    story.LastScanUtc = scanTime;

                    var users = new Dictionary<string, User>();

                    foreach (var comment in storyComments.Comments)
                    {
                        if (!users.TryGetValue(comment.User, out var user))
                        {
                            user = await db.Users.Include(u => u.Comments)
                                .SingleOrDefaultAsync(u => u.UserName == comment.User);
                        }

                        if (user == null)
                        {
                            user = new User {UserName = comment.User, Comments = new List<Comment>()};
                            await db.Users.AddAsync(user);
                            users.Add(user.UserName, user);
                        }

                        var existingComments = new HashSet<long>(user.Comments.Select(c => c.CommentId));
                        if (!existingComments.Contains(comment.CommentId))
                        {
                            var item = new Comment
                            {
                                CommentId = comment.CommentId,
                                ParentId = comment.ParentId,
                                DateTimeUtc = comment.Timestamp.UtcDateTime,
                                Story = story,
                                User = user
                            };
                            user.Comments.Add(item);
                            await db.Comments.AddAsync(item);
                            newComments++;
                        }
                    }

                    await db.SaveChangesAsync();

                    return new StoryProcessingResult(story, storyComments.Comments.Count, newComments, true);
                }

                return new StoryProcessingResult(story, 0, 0, false);
            }
        }
    }

    class StoryProcessingResult
    {
        public StoryProcessingResult(Story story, int totalComments, int newComments, bool processed)
        {
            StoryTitle = story.Title;
            TimestampUtc = story.DateTimeUtc;
            TotalCommentsCount = totalComments;
            NewCommentsCount = newComments;
            Rating = story.Rating;
            Processed = processed;
        }

        public string StoryTitle { get; }
        public DateTime TimestampUtc { get; }
        public int TotalCommentsCount { get; }
        public int NewCommentsCount { get; }
        public int? Rating { get; }
        public bool Processed { get; }
    }
}