using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PikaModel;

namespace PikaFetcher
{
    internal class TopFetcher : AbstractFetcher
    {
        private readonly int _top;
        private readonly TimeSpan _duration;

        public TopFetcher(PikabuApi api, int top, TimeSpan duration) : base(api)
        {
            _top = top;
            _duration = duration;
        }

        public override async Task FetchLoop()
        {
            var performanceCounter = new PerformanceCounter("Top" + _top);
            var savingTask = Task.CompletedTask;
            while (true)
            {
                try
                {
                    int[] topStoryIds;
                    using (var db = new PikabuContext())
                    {
                        topStoryIds = await db.Stories
                            .Where(story => story.DateTimeUtc >= DateTime.UtcNow - _duration)
                            .OrderByDescending(story => story.Rating)
                            .Select(story => story.StoryId)
                            .Take(_top)
                            .ToArrayAsync();
                    }

                    if (topStoryIds.Length < _top)
                    {
                        Console.WriteLine($"!!!{DateTime.UtcNow} topStoryIds.Length < top ({topStoryIds.Length})");
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
                            using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                            {
                                savingTask = await ProcessStory(storyId, '!', cancellationTokenSource.Token);
                                await performanceCounter.ProcessStory(cancellationTokenSource.Token);
                            }
                        }
                        catch (Exception e)
                        {
                            await Task.Delay(1000);
                            Console.WriteLine($"!!!{DateTime.UtcNow} ERROR: {e}");
                        }
                    }

                    Console.WriteLine($"!!!{DateTime.UtcNow} RESTART");
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine($"!!!{DateTime.UtcNow} TERMINATED");
                    throw;
                }
                catch (Exception e)
                {
                    await Task.Delay(1000);
                    Console.WriteLine($"!!!{DateTime.UtcNow} ERROR: {e}");
                }
            }

        }
    }
}