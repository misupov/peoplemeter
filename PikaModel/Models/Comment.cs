using System;
using System.Collections.Generic;

namespace PikaModel.Models
{
    public partial class Comment
    {
        public long CommentId { get; set; }
        public long ParentId { get; set; }
        public int StoryId { get; set; }
        public int? Rating { get; set; }
        public string UserName { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public long? CommentContentId { get; set; }

        public virtual CommentContent CommentContent { get; set; }
        public virtual Story Story { get; set; }
        public virtual User UserNameNavigation { get; set; }
    }
}
