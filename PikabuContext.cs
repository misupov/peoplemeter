using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PikaFetcher
{
    internal class PikabuContext : DbContext
    {
        private DataBaseType _dataBaseType;
        private readonly string _dataBase;

        public PikabuContext(string dataBase, DataBaseType dataBaseType)
        {
            _dataBaseType = dataBaseType;
            _dataBase = dataBase ?? "pikabu.db";
        }

        public DbSet<Comment> Comments { get; set; }
        public DbSet<Story> Stories { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            switch (_dataBaseType)
            {
                case DataBaseType.MySql:
                    optionsBuilder.UseMySQL(_dataBase);
                    break;
                case DataBaseType.Sqlite:
                    optionsBuilder.UseSqlite($"Data Source={_dataBase}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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