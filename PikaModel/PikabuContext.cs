using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PikaModel.Model;

namespace PikaModel
{
    public class PikabuContext : DbContext
    {
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentContent> CommentContents { get; set; }
        public DbSet<Story> Stories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<FetcherStat> FetcherStats { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(GetRDSConnectionString());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>().Property(h => h.UserName).HasMaxLength(100).IsRequired();
        }

        public static string GetRDSConnectionString()
        {
            var dbname = Environment.GetEnvironmentVariable("RDS_DB_NAME");
            var username = Environment.GetEnvironmentVariable("RDS_USERNAME");
            var password = Environment.GetEnvironmentVariable("RDS_PASSWORD");
            var hostname = Environment.GetEnvironmentVariable("RDS_HOSTNAME");
            var port = Environment.GetEnvironmentVariable("RDS_PORT");

            if (string.IsNullOrEmpty(dbname)) return null;

            return $"Server={hostname};Port={port};Database={dbname};Uid={username};Pwd={password}";
        }
    }

    public class FetcherStat
    {
        [Key]
        public string FetcherName { get; set; }

        public double StoriesPerSecondForLastHour { get; set; }

        public double StoriesPerSecondForLastMinute { get; set; }
    }
}