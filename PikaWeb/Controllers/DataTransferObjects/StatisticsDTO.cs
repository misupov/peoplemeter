namespace PikaWeb.Controllers.DataTransferObjects
{
    public class StatisticsDTO
    {
        public int CommentsCount { get; set; }
        public int UsersCount { get; set; }
        public int StoriesCount { get; set; }
        public StatisticsFetchersDTO[] Fetchers { get; set; }

        public class StatisticsFetchersDTO
        {
            public string FetcherName { get; set; }
            public double StoriesPerSecondForLastHour { get; set; }
            public double StoriesPerSecondForLastMinute { get; set; }
        }
    }
}