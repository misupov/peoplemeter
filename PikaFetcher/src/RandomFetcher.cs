using System;
using System.Threading;
using System.Threading.Tasks;
using PikaFetcher.Parser;

namespace PikaFetcher
{
    internal class RandomFetcher : AbstractFetcher
    {
        private readonly Random _random;

        public RandomFetcher(PikabuApi api) : base(api)
        {
            _random = new Random();
        }

        public override async Task FetchLoop()
        {
            var performanceCounter = new PerformanceCounter("Random");
            var c = 0;
            var latestStoryId = await Api.GetLatestStoryId();
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
                    using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                    {
                        savingTask = await ProcessStory(storyId, ' ', cancellationTokenSource.Token);
                        await performanceCounter.ProcessStory(cancellationTokenSource.Token);
                    }

                    c++;

                    if (c % 100 == 0)
                    {
                        latestStoryId = await Api.GetLatestStoryId();
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

        private int GetStoryId(int latestStoryId)
        {
            var next = _random.NextDouble();
            var skip = 0;
            var range = 200;
            if (next < 0.2)
            {
                return latestStoryId - _random.Next(range);
            }

            skip += range;
            range = 2000 - range;
            if (next < 0.5)
            {
                return latestStoryId - skip - _random.Next(range);
            }

            skip += range;
            range = 20000 - range;
            if (next < 0.8)
            {
                return latestStoryId - skip - _random.Next(range);
            }

            skip += range;
            range = 80000 - range;
            if (next < 1)
            {
                return latestStoryId - skip - _random.Next(range);
            }

            skip += range;
            return latestStoryId - skip - _random.Next(latestStoryId - range);
        }
    }
}