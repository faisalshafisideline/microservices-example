using AuthService.Domain.Entities;
using AuthService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserClubRole> UserClubRoles => Set<UserClubRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            
            entity.Property(u => u.Id)
                .HasConversion(
                    id => id.Value,
                    value => UserId.From(value))
                .ValueGeneratedNever();

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(512);

            entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(u => u.PreferredLanguage)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("en-US");

            entity.Property(u => u.TwoFactorSecret)
                .HasMaxLength(256);

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.Property(u => u.LastLoginAt);
            entity.Property(u => u.UpdatedAt);

            // Indexes for performance
            entity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            entity.HasIndex(u => u.Username)
                .IsUnique()
                .HasDatabaseName("IX_Users_Username");

            entity.HasIndex(u => u.IsActive)
                .HasDatabaseName("IX_Users_IsActive");

            // Ignore the ClubRoles navigation property for now - we'll configure it separately
            entity.Ignore(u => u.ClubRoles);
        });

        // UserClubRole entity configuration
        modelBuilder.Entity<UserClubRole>(entity =>
        {
            entity.HasKey(ucr => new { ucr.UserId, ucr.ClubId });

            entity.Property(ucr => ucr.UserId)
                .IsRequired();

            entity.Property(ucr => ucr.ClubId)
                .IsRequired();

            entity.Property(ucr => ucr.Role)
                .IsRequired()
                .HasMaxLength(500); // Support multiple comma-separated roles

            entity.Property(ucr => ucr.AssignedAt)
                .IsRequired();

            entity.Property(ucr => ucr.UpdatedAt);

            // Indexes for performance
            entity.HasIndex(ucr => ucr.UserId)
                .HasDatabaseName("IX_UserClubRoles_UserId");

            entity.HasIndex(ucr => ucr.ClubId)
                .HasDatabaseName("IX_UserClubRoles_ClubId");

            entity.HasIndex(ucr => new { ucr.ClubId, ucr.Role })
                .HasDatabaseName("IX_UserClubRoles_ClubId_Role");
        });

        // Configure the relationship manually
        modelBuilder.Entity<User>()
            .HasMany(u => u.ClubRoles)
            .WithOne()
            .HasForeignKey(ucr => ucr.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
} 