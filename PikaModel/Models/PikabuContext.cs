using Microsoft.EntityFrameworkCore;

namespace PikaModel.Models
{
    public partial class PikabuContext : DbContext
    {
        public PikabuContext()
        {
        }

        public PikabuContext(DbContextOptions<PikabuContext> options)
            : base(options)
        {
        }

        public virtual DbSet<CommentContent> CommentContents { get; set; }
        public virtual DbSet<Comment> Comments { get; set; }
        public virtual DbSet<FetcherStat> FetcherStats { get; set; }
        public virtual DbSet<Story> Stories { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql(GetDevConnectionString());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CommentContent>(entity =>
            {
                entity.HasKey(e => e.CommentContentId)
                    .HasName("PRIMARY");

                entity.Property(e => e.CommentContentId).HasColumnType("bigint(20)");

                entity.Property(e => e.BodyHtml).HasColumnType("longtext");
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.CommentId)
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.CommentContentId);

                entity.HasIndex(e => e.DateTimeUtc);

                entity.HasIndex(e => e.StoryId);

                entity.HasIndex(e => e.UserName);

                entity.Property(e => e.CommentId).HasColumnType("bigint(20)");

                entity.Property(e => e.CommentContentId).HasColumnType("bigint(20)");

                entity.Property(e => e.ParentId).HasColumnType("bigint(20)");

                entity.Property(e => e.Rating).HasColumnType("int(11)");

                entity.Property(e => e.StoryId).HasColumnType("int(11)");

                entity.Property(e => e.UserName).HasColumnType("varchar(100)");

                entity.HasOne(d => d.CommentContent)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.CommentContentId);

                entity.HasOne(d => d.Story)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.StoryId);

                entity.HasOne(d => d.UserNameNavigation)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(d => d.UserName);
            });

            modelBuilder.Entity<FetcherStat>(entity =>
            {
                entity.HasKey(e => e.FetcherName)
                    .HasName("PRIMARY");

                entity.Property(e => e.FetcherName).HasColumnType("varchar(255)");
            });

            modelBuilder.Entity<Story>(entity =>
            {
                entity.HasKey(e => e.StoryId)
                    .HasName("PRIMARY");

                entity.Property(e => e.StoryId).HasColumnType("int(11)");

                entity.Property(e => e.Author).HasColumnType("longtext");

                entity.Property(e => e.Rating).HasColumnType("int(11)");

                entity.Property(e => e.Title).HasColumnType("longtext");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserName)
                    .HasName("PRIMARY");

                entity.Property(e => e.UserName).HasColumnType("varchar(100)");

                entity.Property(e => e.AvatarUrl).HasColumnType("longtext");
            });
        }
    }
}
