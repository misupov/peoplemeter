using System;
using PikaModel;

namespace PikaFetcher
{
    internal class StoryProcessingResult
    {
        public StoryProcessingResult(Story story, int totalComments, int newComments)
        {
            StoryTitle = story.Title;
            TimestampUtc = story.DateTimeUtc;
            TotalCommentsCount = totalComments;
            NewCommentsCount = newComments;
            Rating = story.Rating;
        }

        public string StoryTitle { get; }
        public DateTime TimestampUtc { get; }
        public int TotalCommentsCount { get; }
        public int NewCommentsCount { get; }
        public int? Rating { get; }
    }
}