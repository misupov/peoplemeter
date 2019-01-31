using System;

namespace PikaFetcher.Model
{
    public class Comment
    {
        public long CommentId { get; set; }
        public long ParentId { get; set; }
        public User User { get; set; }
        public Story Story { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public string CommentBody { get; set; }
    }
}