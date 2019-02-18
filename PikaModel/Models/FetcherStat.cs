using System;
using System.Collections.Generic;

namespace PikaModel.Models
{
    public partial class FetcherStat
    {
        public string FetcherName { get; set; }
        public double StoriesPerSecondForLastHour { get; set; }
        public double StoriesPerSecondForLastMinute { get; set; }
    }
}
