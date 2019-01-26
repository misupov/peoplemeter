using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PikaFetcher
{
    public class PikabuContext : DbContext
    {
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Story> Stories { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=/data/pikabu.db");
        }
    }

    public class User
    {
        [Key]
        public string UserName { get; set; }

        public ICollection<Comment> Comments { get; set; }
    }

    public class Comment
    {
        public long CommentId { get; set; }
        public long ParentId { get; set; }
        public User User { get; set; }
        public Story Story { get; set; }
        public DateTime DateTimeUtc { get; set; }
    }

    public class Story
    {
        public int StoryId { get; set; }
        public string Title { get; set; }
        public int? Rating { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public DateTime LastScanUtc { get; set; }

        public ICollection<Comment> Comments { get; set; }
    }
}