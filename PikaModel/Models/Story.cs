using System;
using System.Collections.Generic;

namespace PikaModel.Models
{
    public partial class Story
    {
        public Story()
        {
            Comments = new HashSet<Comment>();
        }

        public int StoryId { get; set; }
        public string Title { get; set; }
        public int? Rating { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public DateTime LastScanUtc { get; set; }
        public string Author { get; set; }

        public virtual ICollection<Comment> Comments { get; set; }
    }
}
