using System;
using Microsoft.EntityFrameworkCore;

namespace PikaFetcher.Model
{
    internal class PikabuContext : DbContext
    {
        private readonly DataBaseType _dataBaseType;
        private readonly string _dataBase;

        public PikabuContext()
        {
        }

        public PikabuContext(string dataBase, DataBaseType dataBaseType)
        {
            _dataBaseType = dataBaseType;
            _dataBase = dataBase ?? "pikabu.db";
        }

        public DbSet<Comment> Comments { get; set; }
        public DbSet<Story> Stories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Job> Jobs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            switch (_dataBaseType)
            {
                case DataBaseType.RDS:
                    optionsBuilder.UseMySql(GetRDSConnectionString());
                    break;
                case DataBaseType.MySql:
                    optionsBuilder.UseMySql(_dataBase);
                    break;
                case DataBaseType.Sqlite:
                    optionsBuilder.UseSqlite($"Data Source={_dataBase}");
                    break;
                default:
                    optionsBuilder.UseSqlite($"Data Source=pikabu.db");
                    break;
            }
        }

        public static string GetRDSConnectionString()
        {
            var dbname = Environment.GetEnvironmentVariable("RDS_DB_NAME");
            var username = Environment.GetEnvironmentVariable("RDS_USERNAME");
            var password = Environment.GetEnvironmentVariable("RDS_PASSWORD");
            var hostname = Environment.GetEnvironmentVariable("RDS_HOSTNAME");
            var port = Environment.GetEnvironmentVariable("RDS_PORT");

            if (string.IsNullOrEmpty(dbname)) return null;

            return $"Data Source={hostname},{port};Initial Catalog={dbname};User ID={username};Password={password};";
        }
    }
}