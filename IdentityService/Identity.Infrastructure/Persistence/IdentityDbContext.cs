using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence
{
    // DbContext is the main class that connects our C# code to the SQL Server database.
    // Each DbSet becomes a table, and EF Core handles all the SQL generation for us.
    public class IdentityDbContext : DbContext
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
            : base(options)
        {
        }

        // Each DbSet maps to a table in the database
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        // Fluent API — configure table constraints, relationships, and seed data
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Role Table Configuration ──
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).HasDefaultValueSql("NEWID()");

                entity.Property(r => r.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                // Unique constraint — no duplicate role names allowed
                entity.HasIndex(r => r.Name).IsUnique();
            });

            // ── User Table Configuration ──
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).HasDefaultValueSql("NEWID()");

                entity.Property(u => u.FullName).IsRequired().HasMaxLength(150);

                entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
                entity.HasIndex(u => u.Email).IsUnique(); // No duplicate emails

                entity.Property(u => u.PasswordHash).IsRequired(); // BCrypt hash, stored as NVARCHAR(MAX)

                entity.Property(u => u.PhoneNumber).HasMaxLength(20); // Nullable by default

                entity.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
                entity.Property(u => u.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
                entity.Property(u => u.UpdatedAt).IsRequired(false);

                // Relationship: Each User belongs to one Role, each Role has many Users
                entity.HasOne(u => u.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict); // Prevent accidental role deletion
            });

            // ── Seed Data — insert default roles during migration ──
            // Fixed GUIDs ensure migrations are deterministic and don't create duplicates
            var userRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var adminRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = userRoleId, Name = "User" },
                new Role { Id = adminRoleId, Name = "Admin" }
            );
        }
    }
}
