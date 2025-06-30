using ClubService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.Text.Json;

namespace ClubService.Infrastructure.Data;

public class ClubDbContext : DbContext
{
    public ClubDbContext(DbContextOptions<ClubDbContext> options) : base(options)
    {
    }

    public DbSet<Club> Clubs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Club Configuration
        modelBuilder.Entity<Club>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasConversion(
                    v => v.Value,
                    v => new(v));

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.Code)
                .IsUnique();

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.Country)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.Timezone)
                .IsRequired()
                .HasConversion(
                    v => v.Id,
                    v => DateTimeZoneProviders.Tzdb[v])
                .HasMaxLength(100);

            entity.Property(e => e.DefaultLanguage)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.DefaultVatRate)
                .HasColumnType("decimal(5,4)");

            entity.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.Tier)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.PrimaryContactEmail)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.PrimaryContactName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.PrimaryContactPhone)
                .HasMaxLength(50);

            entity.Property(e => e.SubscriptionPlan)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.SubscriptionExpiresAt)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToDateOnly() : (DateOnly?)null,
                    v => v.HasValue ? LocalDate.FromDateOnly(v.Value) : (LocalDate?)null);

            entity.Property(e => e.CreatedAt)
                .HasConversion(
                    v => v.ToDateTimeUtc(),
                    v => Instant.FromDateTimeUtc(DateTime.SpecifyKind(v, DateTimeKind.Utc)));

            entity.Property(e => e.UpdatedAt)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToDateTimeUtc() : (DateTime?)null,
                    v => v.HasValue ? Instant.FromDateTimeUtc(DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)) : (Instant?)null);

            entity.Property(e => e.CreatedBy)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100);

            // Address Configuration
            entity.OwnsOne(e => e.Address, address =>
            {
                address.Property(a => a.Street)
                    .IsRequired()
                    .HasMaxLength(200);

                address.Property(a => a.City)
                    .IsRequired()
                    .HasMaxLength(100);

                address.Property(a => a.State)
                    .IsRequired()
                    .HasMaxLength(100);

                address.Property(a => a.PostalCode)
                    .IsRequired()
                    .HasMaxLength(20);

                address.Property(a => a.Country)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            // Settings Configuration
            entity.OwnsOne(e => e.Settings, settings =>
            {
                settings.Property(s => s.CustomDomain)
                    .HasMaxLength(255);

                settings.Property(s => s.LogoUrl)
                    .HasMaxLength(500);

                settings.Property(s => s.BrandColor)
                    .HasMaxLength(10);

                settings.Property(s => s.CustomFields)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
                    .HasColumnType("nvarchar(max)");
            });
        });
    }
} 