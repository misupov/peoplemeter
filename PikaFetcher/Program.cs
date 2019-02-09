using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PikaModel;

namespace PikaFetcher
{
    class Program
    {
        private readonly Random r = new Random();

        private static async Task Main(string[] args)
        {
            var program = new Program();
            await program.OnExecuteAsync();
        }

        private Program()
        {
        }

        private async Task OnExecuteAsync()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var api = new PikabuApi();
            await api.Init();

            var loopTop = LoopTop(api);
            var loopPeriod = LoopPeriod(api);
            await Task.WhenAll(loopTop, loopPeriod);
        }

        private async Task LoopPeriod(PikabuApi api)
        {
            int c = 0;
            var latestStoryId = await api.GetLatestStoryId();
            while (true)
            {
                int storyId = -1;
                try
                {
                    storyId = GetStoryId(latestStoryId);
                    var result = await ProcessStory(api, storyId);
                    Console.WriteLine($"{DateTime.UtcNow} [{FormatTimeSpan(DateTime.UtcNow - result.TimestampUtc)}] ({storyId}) {result.Rating?.ToString("+0;-#") ?? "?"} ({result.NewCommentsCount}/{result.TotalCommentsCount}) {result.StoryTitle}");
                }
                catch (Exception e)
                {
                    await Task.Delay(1000);
                    Console.WriteLine($"ERROR ({storyId}/{latestStoryId}): {e.Message}");
                }

                c++;
                if (c % 100 == 0)
                {
                    latestStoryId = await api.GetLatestStoryId();
                    c = 0;
                }
            }
        }

        private int GetStoryId(int latestStoryId)
        {
            var next = r.NextDouble();
            var skip = 0;
            var range = 200;
            if (next < 0.2)
            {
                return latestStoryId - r.Next(range);
            }

            skip += range;
            range = 1800;
            if (next < 0.5)
            {
                return latestStoryId - skip - r.Next(range);
            }

            skip += range;
            range = 18000;
            if (next < 0.8)
            {
                return latestStoryId - skip - r.Next(range);
            }

            skip += range;
            range = 60000;
            if (next < 1)
            {
                return latestStoryId - skip - r.Next(range);
            }

            skip += range;
            return latestStoryId - skip - r.Next(latestStoryId - range);
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
            int top = 500;
            while (true)
            {
                int[] topStoryIds;
                using (var db = new PikabuContext())
                {
                    topStoryIds = await db.Stories
                        .Where(story => story.DateTimeUtc >= DateTime.UtcNow - TimeSpan.FromDays(7))
                        .OrderByDescending(story => story.Rating)
                        .Select(story => story.StoryId)
                        .Take(top)
                        .ToArrayAsync();
                }

                if (topStoryIds.Length < top)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                for (var index = 0; index < topStoryIds.Length; index++)
                {
                    var storyId = topStoryIds[index];
                    try
                    {
                        var result = await ProcessStory(api, storyId);
                        if (result != null)
                        {
                            Console.WriteLine(
                                $"TOP[{index + 1}] {DateTime.UtcNow} [{FormatTimeSpan(DateTime.UtcNow - result.TimestampUtc)}] ({storyId}) {result.Rating?.ToString("+0;-#") ?? "?"} ({result.NewCommentsCount}/{result.TotalCommentsCount}) {result.StoryTitle}");
                        }
                    }
                    catch (Exception e)
                    {
                        await Task.Delay(1000);
                        Console.WriteLine($"ERROR: {e.Message}");
                    }
                }

                Console.WriteLine("RESTART");
            }
        }

        private async Task<StoryProcessingResult> ProcessStory(PikabuApi api, int storyId)
        {
            throw new Exception();
            using (var db = new PikabuContext())
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
                        Author = storyComments.Author,
                        DateTimeUtc = storyComments.Timestamp.UtcDateTime,
                        Comments = new List<Comment>()
                    };
                    await db.Stories.AddAsync(story);
                }

                story.Rating = storyComments.Rating;
                story.Title = storyComments.StoryTitle;
                story.Author = storyComments.Author;
                story.LastScanUtc = scanTime;

                var userNames = new HashSet<string>(storyComments.Comments.Select(c => c.User));

                var userComments = await db.Comments.Where(c => userNames.Contains(c.User.UserName)).ToArrayAsync();

                var users = new HashSet<string>(userComments.Select(uc => uc.UserName));
                var comments = userComments.ToDictionary(c => c.CommentId);

                foreach (var comment in storyComments.Comments)
                {
                    if (!users.Contains(comment.User))
                    {
                        var user = new User {UserName = comment.User, Comments = new List<Comment>()};
                        await db.Users.AddAsync(user);
                        users.Add(user.UserName);
                    }

                    if (!comments.TryGetValue(comment.CommentId, out var c))
                    {
                        var item = new Comment
                        {
                            CommentId = comment.CommentId,
                            ParentId = comment.ParentId,
                            DateTimeUtc = comment.Timestamp.UtcDateTime,
                            Rating = comment.Rating,
                            Story = story,
                            UserName = comment.User,
                            CommentBody = comment.Body
                        };
                        comments[item.CommentId] = item;
                        await db.Comments.AddAsync(item);
                        newComments++;
                    }
                    else
                    {
                        c.Rating = comment.Rating;
                    }
                }

                await db.SaveChangesAsync();

                return new StoryProcessingResult(story, storyComments.Comments.Count, newComments);
            }
        }
    }
}