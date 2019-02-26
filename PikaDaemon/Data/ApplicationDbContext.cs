using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PikaDaemon.Data
{
    public class PikabuDbContext : IdentityDbContext<
        PikabuUser, PikabuRole, string,
        PikabuUserClaim, PikabuUserRole, PikabuUserLogin,
        PikabuRoleClaim, PikabuUserToken>
    {
        public PikabuDbContext(DbContextOptions<PikabuDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PikabuUser>(b =>
            {
                b.ToTable("Users");

                // Each User can have many UserClaims
                b.HasMany(e => e.Claims)
                    .WithOne(e => e.User)
                    .HasForeignKey(uc => uc.UserId)
                    .IsRequired();

                // Each User can have many UserLogins
                b.HasMany(e => e.Logins)
                    .WithOne(e => e.User)
                    .HasForeignKey(ul => ul.UserId)
                    .IsRequired();

                // Each User can have many UserTokens
                b.HasMany(e => e.Tokens)
                    .WithOne(e => e.User)
                    .HasForeignKey(ut => ut.UserId)
                    .IsRequired();

                // Each User can have many entries in the UserRole join table
                b.HasMany(e => e.UserRoles)
                    .WithOne(e => e.User)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });

            modelBuilder.Entity<PikabuRole>(b =>
            {
                b.ToTable("Roles");

                // Each Role can have many entries in the UserRole join table
                b.HasMany(e => e.UserRoles)
                    .WithOne(e => e.Role)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();

                // Each Role can have many associated RoleClaims
                b.HasMany(e => e.RoleClaims)
                    .WithOne(e => e.Role)
                    .HasForeignKey(rc => rc.RoleId)
                    .IsRequired();
            });

            modelBuilder.Entity<PikabuUserClaim>(b =>
            {
                b.ToTable("UserClaims");
            });

            modelBuilder.Entity<PikabuUserLogin>(b =>
            {
                b.ToTable("UserLogins");
            });

            modelBuilder.Entity<PikabuUserToken>(b =>
            {
                b.ToTable("UserTokens");
            });

            modelBuilder.Entity<PikabuRoleClaim>(b =>
            {
                b.ToTable("RoleClaims");
            });

            modelBuilder.Entity<PikabuUserRole>(b =>
            {
                b.ToTable("UserRoles");
            });
        }
    }

    public class PikabuRole: IdentityRole
    {
        public virtual ICollection<PikabuUserRole> UserRoles { get; set; }
        public virtual ICollection<PikabuRoleClaim> RoleClaims { get; set; }
    }

    public class PikabuUserRole : IdentityUserRole<string>
    {
        public virtual PikabuUser User { get; set; }
        public virtual PikabuRole Role { get; set; }
    }

    public class PikabuUserClaim : IdentityUserClaim<string>
    {
        public virtual PikabuUser User { get; set; }
    }

    public class PikabuUserLogin : IdentityUserLogin<string>
    {
        public virtual PikabuUser User { get; set; }
    }

    public class PikabuRoleClaim : IdentityRoleClaim<string>
    {
        public virtual PikabuRole Role { get; set; }
    }

    public class PikabuUserToken : IdentityUserToken<string>
    {
        public virtual PikabuUser User { get; set; }
    }

    public class PikabuUser : IdentityUser
    {
        public virtual ICollection<PikabuUserClaim> Claims { get; set; }
        public virtual ICollection<PikabuUserLogin> Logins { get; set; }
        public virtual ICollection<PikabuUserToken> Tokens { get; set; }
        public virtual ICollection<PikabuUserRole> UserRoles { get; set; }
    }
}
