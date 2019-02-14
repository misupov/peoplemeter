using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PikaModel;

namespace PikaFetcher
{
    internal class PerformanceCounter
    {
        private readonly string _name;
        private readonly Queue<DateTimeOffset> _latestHourStats = new Queue<DateTimeOffset>();
        private readonly Queue<DateTimeOffset> _latestMinuteStats = new Queue<DateTimeOffset>();

        public PerformanceCounter(string name)
        {
            _name = name;
        }

        public async Task ProcessStory(CancellationToken cancellationToken)
        {
            var utcNow = DateTimeOffset.UtcNow;

            _latestHourStats.Enqueue(utcNow);
            _latestMinuteStats.Enqueue(utcNow);
            while (_latestHourStats.Peek() < utcNow.AddHours(-1))
            {
                _latestHourStats.Dequeue();
            }

            while (_latestMinuteStats.Peek() < utcNow.AddMinutes(-1))
            {
                _latestMinuteStats.Dequeue();
            }

            using (var db = new PikabuContext())
            {
                var stat = await db.FetcherStats.SingleOrDefaultAsync(s => s.FetcherName == _name, cancellationToken);
                if (stat == null)
                {
                    stat = new FetcherStat();
                    stat.FetcherName = _name;
                    db.FetcherStats.Add(stat);
                }

                if (_latestHourStats.Count >= 2)
                {
                    stat.StoriesPerSecondForLastHour =
                        _latestHourStats.Count / (utcNow - _latestHourStats.Peek()).TotalSeconds;
                }

                if (_latestMinuteStats.Count >= 2)
                {
                    stat.StoriesPerSecondForLastMinute =
                        _latestMinuteStats.Count / (utcNow - _latestMinuteStats.Peek()).TotalSeconds;
                }

                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}