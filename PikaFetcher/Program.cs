using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PikaModel;
using PikaModel.Model;

namespace PikaFetcher
{
    class Program
    {
        private readonly Random r = new Random();

        private static async Task Main()
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

            var loopRandom = LoopRandom(api);
            var loopTop = LoopTop(api);
            await Task.WhenAll(loopRandom, loopTop);
        }

        private async Task LoopRandom(PikabuApi api)
        {
            var latestHourStats = new Queue<DateTimeOffset>();
            var latestMinuteStats = new Queue<DateTimeOffset>();
            var c = 0;
            var latestStoryId = await api.GetLatestStoryId();
            var savingTask = Task.CompletedTask;
            while (true)
            {
                int storyId = -1;
                try
                {
                    storyId = GetStoryId(latestStoryId);
                    if (savingTask.IsCanceled || savingTask.IsFaulted)
                    {
                        savingTask = Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    await savingTask;
                    savingTask = await ProcessStory(api, storyId);
                    await UpdateStats(latestHourStats, latestMinuteStats, "Random");

                    c++;

                    if (c % 100 == 0)
                    {
                        latestStoryId = await api.GetLatestStoryId();
                        c = 0;
                    }
                }
                catch (Exception e)
                {
                    await Task.Delay(1000);
                    Console.WriteLine($"{DateTime.UtcNow} ERROR ({storyId}/{latestStoryId}): {e}");
                }
            }
        }

        private async Task LoopTop(PikabuApi api)
        {
            var latestHourStats = new Queue<DateTimeOffset>();
            var latestMinuteStats = new Queue<DateTimeOffset>();

            int top = 500;
            var savingTask = Task.CompletedTask;
            while (true)
            {
                try
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

                    foreach (var storyId in topStoryIds)
                    {
                        try
                        {
                            if (savingTask.IsCanceled || savingTask.IsFaulted)
                            {
                                savingTask = Task.Delay(TimeSpan.FromSeconds(1));
                            }

                            await savingTask;
                            savingTask = await ProcessStory(api, storyId);
                            await UpdateStats(latestHourStats, latestMinuteStats, "Top" + top);
                        }
                        catch (Exception e)
                        {
                            await Task.Delay(1000);
                            Console.WriteLine($"!!!{DateTime.UtcNow} ERROR: {e}");
                        }
                    }

                    Console.WriteLine("!!!{DateTime.UtcNow} RESTART");
                }
                catch (Exception e)
                {
                    await Task.Delay(1000);
                    Console.WriteLine($"!!!{DateTime.UtcNow} ERROR: {e}");
                }
            }
        }

        private async Task<Task> ProcessStory(PikabuApi api, int storyId)
        {
            var storyComments = await api.GetStoryComments(storyId);
            return Task.Run(() => SaveStory(storyComments));
        }

        private async Task SaveStory(StoryComments storyComments)
        {
            using (var db = new PikabuContext())
            using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable))
            {
                var scanTime = DateTime.UtcNow;
                var story = await db.Stories.SingleOrDefaultAsync(s => s.StoryId == storyComments.StoryId);
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

                var storyCommentIds = storyComments.Comments.Select(c => c.CommentId).ToArray();
                var existingComments = await db.Comments.Where(c => storyCommentIds.Contains(c.CommentId)).ToDictionaryAsync(c => c.CommentId);

                var storyUserNames = new HashSet<string>(storyComments.Comments.Select(c => c.User));
                var existingUsers = (await db.Users.Where(c => storyUserNames.Contains(c.UserName)).ToArrayAsync()).ToDictionary(u => u.UserName);

                foreach (var comment in storyComments.Comments)
                {
                    if (!existingUsers.TryGetValue(comment.User, out var user))
                    {
                        user = new User { UserName = comment.User, AvatarUrl = comment.UserAvatarUrl, Comments = new List<Comment>() };
                        await db.Users.AddAsync(user);
                        existingUsers[user.UserName] = user;
                    }
                    else
                    {
                        user.AvatarUrl = comment.UserAvatarUrl;
                    }

                    if (!existingComments.TryGetValue(comment.CommentId, out var c))
                    {
                        var item = new Comment
                        {
                            CommentId = comment.CommentId,
                            ParentId = comment.ParentId,
                            DateTimeUtc = comment.Timestamp.UtcDateTime,
                            Rating = comment.Rating,
                            Story = story,
                            UserName = comment.User,
                            CommentContent = new CommentContent { BodyHtml = comment.Body }
                        };
                        await db.Comments.AddAsync(item);
                        existingComments[item.CommentId] = item;
                    }
                    else
                    {
                        c.Rating = comment.Rating;
                    }
                }

                Console.WriteLine($"{DateTime.UtcNow} ({storyComments.StoryId}) {storyComments.Rating?.ToString("+0;-#") ?? "?"} {storyComments.StoryTitle}");

                await db.SaveChangesAsync();

                transaction.Commit();
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

        private static async Task UpdateStats(Queue<DateTimeOffset> latestHourStats, Queue<DateTimeOffset> latestMinuteStats, string fetcherName)
        {
            var utcNow = DateTimeOffset.UtcNow;

            latestHourStats.Enqueue(utcNow);
            latestMinuteStats.Enqueue(utcNow);
            while (latestHourStats.Peek() < utcNow.AddHours(-1))
            {
                latestHourStats.Dequeue();
            }

            while (latestMinuteStats.Peek() < utcNow.AddMinutes(-1))
            {
                latestMinuteStats.Dequeue();
            }

            try
            {
                using (var db = new PikabuContext())
                {
                    var stat = await db.FetcherStats.SingleOrDefaultAsync(s => s.FetcherName == fetcherName);
                    if (stat == null)
                    {
                        stat = new FetcherStat();
                        stat.FetcherName = fetcherName;
                        db.FetcherStats.Add(stat);
                    }

                    if (latestHourStats.Count >= 2)
                    {
                        stat.StoriesPerSecondForLastHour =
                            latestHourStats.Count / (utcNow - latestHourStats.Peek()).TotalSeconds;
                    }

                    if (latestMinuteStats.Count >= 2)
                    {
                        stat.StoriesPerSecondForLastMinute = latestMinuteStats.Count / (utcNow - latestMinuteStats.Peek()).TotalSeconds;
                    }

                    await db.SaveChangesAsync();
                }
            }
            catch
            {

            }
        }
    }
}