using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PikaFetcher
{
    class Program
    {
        private readonly Options _options;

        private static async Task Main(string[] args)
        {
            foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
            {
                Console.Out.WriteLine(variable.Key + "=" + variable.Value);
            }

            var options = Options.FromEnv();
            //var options = Options.Parse(args);

            Console.Out.WriteLine($"Period: {options.Period}");
            Console.Out.WriteLine($"Skip: {options.Skip}");
            Console.Out.WriteLine($"DataBaseType: {options.DataBaseType}");
            Console.Out.WriteLine($"DataBase: {options.DataBase}");
            Console.Out.WriteLine($"Top: {options.Top}");
            Console.Out.WriteLine($"Delay: {options.Delay}");
            Console.Out.WriteLine($"Proxy: {options.Proxy}");

            var program = new Program(options);
            await program.OnExecuteAsync();
        }

        private Program(Options options)
        {
            _options = options;
        }

        private async Task OnExecuteAsync()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var context = new PikabuContext(_options.DataBase, _options.DataBaseType))
            {
                context.Database.Migrate();
            }

            var api = new PikabuApi(_options.Proxy);
            await api.Init();

            if (_options.Top != null)
            {
                await LoopTop(api);
            }
            else
            {
                await LoopPeriod(api);
            }
        }

        private async Task LoopPeriod(PikabuApi api)
        {
            while (true)
            {
                var latestStoryId = await api.GetLatestStoryId() - (_options.Skip ?? 0);
                await LoopSpan(api, latestStoryId, _options.Period, _options.Delay ?? TimeSpan.Zero);

                Console.WriteLine($"RESTART");
            }
        }

        private async Task LoopSpan(PikabuApi api, int storyId, TimeSpan span, TimeSpan delay)
        {
            var breakLoop = false;
            do
            {
                try
                {
                    var result = await ProcessStory(api, storyId);
                    Console.WriteLine($"{DateTime.UtcNow} [{FormatTimeSpan(DateTime.UtcNow - result.TimestampUtc)}] ({storyId}) {result.Rating?.ToString("+0;-#") ?? "?"} ({result.NewCommentsCount}/{result.TotalCommentsCount}) {result.StoryTitle}");
                    breakLoop = (DateTime.UtcNow - result.TimestampUtc) > span;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                storyId--;

                await Task.Delay(delay);

            } while (!breakLoop);
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
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

        private async Task LoopTop(PikabuApi api)
        {
            int top = _options.Top.Value;
            while (true)
            {
                Story[] topStories;
                using (var db = new PikabuContext(_options.DataBase, _options.DataBaseType))
                {
                    topStories = await db.Stories
                        .Where(story => story.DateTimeUtc >= DateTime.UtcNow - _options.Period)
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
                    try
                    {
                        var result = await ProcessStory(api, story.StoryId);
                        if (result != null)
                        {
                            Console.WriteLine($"{DateTime.UtcNow} [{FormatTimeSpan(DateTime.UtcNow - result.TimestampUtc)}] ({story.StoryId}) {result.Rating?.ToString("+0;-#") ?? "?"} ({result.NewCommentsCount}/{result.TotalCommentsCount}) {result.StoryTitle}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"ERROR: {e.Message}");
                    }

                    await Task.Delay(1000);
                }

                Console.WriteLine($"RESTART");
            }
        }

        private async Task<StoryProcessingResult> ProcessStory(PikabuApi api, int storyId)
        {
            using (var db = new PikabuContext(_options.DataBase, _options.DataBaseType))
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var scanTime = DateTime.UtcNow;
                var newComments = 0;
                var story = await db.Stories.SingleOrDefaultAsync(s => s.StoryId == storyId);
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
                            User = user,
                            CommentBody = comment.Body
                        };
                        user.Comments.Add(item);
                        await db.Comments.AddAsync(item);
                        newComments++;
                    }
                }

                await db.SaveChangesAsync();
                transaction.Commit();

                return new StoryProcessingResult(story, storyComments.Comments.Count, newComments);
            }
        }
    }
}