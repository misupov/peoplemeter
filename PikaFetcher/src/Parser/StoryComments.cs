using System;
using System.Collections.Generic;

namespace PikaFetcher.Parser
{
    internal class StoryComments
    {
        public int StoryId { get; }
        public string Author { get; }
        public string StoryTitle { get; }
        public int? Rating { get; }
        public DateTimeOffset Timestamp { get; }
        public IReadOnlyList<StoryComment> Comments { get; }

        public StoryComments(int storyId, string author, string storyTitle, int? rating, DateTimeOffset timestamp, IReadOnlyList<StoryComment> comments)
        {
            StoryId = storyId;
            Author = author;
            StoryTitle = storyTitle;
            Rating = rating;
            Timestamp = timestamp;
            Comments = comments;
        }
    }
}