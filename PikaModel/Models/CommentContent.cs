using System;
using System.Collections.Generic;

namespace PikaModel.Models
{
    public partial class CommentContent
    {
        public CommentContent()
        {
            Comments = new HashSet<Comment>();
        }

        public long CommentContentId { get; set; }
        public string BodyHtml { get; set; }

        public virtual ICollection<Comment> Comments { get; set; }
    }
}
