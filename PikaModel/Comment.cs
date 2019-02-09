using System;

namespace PikaModel
{
    public class Comment
    {
        public long CommentId { get; set; }
        public long ParentId { get; set; }
        public int StoryId { get; set; }
        public int? Rating { get; set; }
        public string UserName { get; set; }
        public DateTime DateTimeUtc { get; set; }

        public CommentContent CommentContent { get; set; }
        public User User { get; set; }
        public Story Story { get; set; }
    }

    public class CommentContent
    {
        public long CommentContentId { get; set; }
        public string BodyHtml { get; set; }
    }
}