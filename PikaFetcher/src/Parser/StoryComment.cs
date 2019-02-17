using System;

namespace PikaFetcher.Parser
{
    internal class StoryComment
    {
        public string User { get; }
        public string UserAvatarUrl { get; }
        public long CommentId { get; }
        public long ParentId { get; }
        public int? Rating { get; }
        public DateTimeOffset Timestamp { get; }
        public string Body { get; }

        public StoryComment(string user, string userAvatarUrl, long commentId, long parentId, int? rating, DateTimeOffset timestamp, string body)
        {
            User = user;
            UserAvatarUrl = userAvatarUrl;
            CommentId = commentId;
            ParentId = parentId;
            Rating = rating;
            Timestamp = timestamp;
            Body = body;
        }
    }
}