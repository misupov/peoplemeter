using System;

namespace PikaFetcher.Model
{
    public class Job
    {
        public int JobId { get; set; }
        public string Title { get; set; }
        public TimeSpan Period { get; set; }
        public int Skip { get; set; }
        public int? Top { get; set; }
        public TimeSpan Delay { get; set; }
    }
}