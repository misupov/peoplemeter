using System;

namespace PikaWeb.Controllers.DataTransferObjects
{
    public class CommentDTO
    {
        public int StoryId { get; set; }
        public string UserName { get; set; }
        public string AvatarUrl { get; set; }
        public string StoryTitle { get; set; }
        public long CommentId { get; set; }
        public long ParentId { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public string CommentBody { get; set; }
        public bool IsAuthor { get; set; }
        public int? Rating { get; set; }
    }
}