using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace PikaModel
{
    public class PikabuContext : DbContext
    {
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Story> Stories { get; set; }
        public DbSet<User> Users { get; set; }

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

    public class XXX : ILoggerProvider
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new YYY(categoryName);
        }

        public class YYY : ILogger
        {
            private StreamWriter _file;

            public YYY(string categoryName)
            {
                _file = File.CreateText(categoryName);
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                _file.WriteLine($"{logLevel}: {formatter(state, exception)}");
                _file.WriteLine();
                _file.Flush();
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel >= LogLevel.Debug;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return new ZZZ();
            }

            public class ZZZ : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}