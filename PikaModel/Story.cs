using System;
using System.Collections.Generic;

namespace PikaModel
{
    public class Story
    {
        public int StoryId { get; set; }
        public string Title { get; set; }
        public string AuthorUserName { get; set; }
        public int? Rating { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public DateTime LastScanUtc { get; set; }

        public User Author { get; set; }
        public ICollection<Comment> Comments { get; set; }
    }
}