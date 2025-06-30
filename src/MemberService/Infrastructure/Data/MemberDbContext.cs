using MemberService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.Text.Json;

namespace MemberService.Infrastructure.Data;

public class MemberDbContext : DbContext
{
    public MemberDbContext(DbContextOptions<MemberDbContext> options) : base(options)
    {
    }

    public DbSet<Member> Members { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Member Configuration
        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasConversion(
                    v => v.Value,
                    v => new(v));

            entity.Property(e => e.MemberNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => new { e.ClubId, e.MemberNumber })
                .IsUnique();

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Phone)
                .HasMaxLength(50);

            entity.Property(e => e.DateOfBirth)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToDateOnly() : (DateOnly?)null,
                    v => v.HasValue ? LocalDate.FromDateOnly(v.Value) : (LocalDate?)null);

            entity.Property(e => e.Gender)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Nationality)
                .HasMaxLength(100);

            entity.Property(e => e.MembershipType)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.JoinedDate)
                .HasConversion(
                    v => v.ToDateOnly(),
                    v => LocalDate.FromDateOnly(v));

            entity.Property(e => e.ExpiryDate)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToDateOnly() : (DateOnly?)null,
                    v => v.HasValue ? LocalDate.FromDateOnly(v.Value) : (LocalDate?)null);

            entity.Property(e => e.LastRenewalDate)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToDateOnly() : (DateOnly?)null,
                    v => v.HasValue ? LocalDate.FromDateOnly(v.Value) : (LocalDate?)null);

            entity.Property(e => e.MembershipFee)
                .HasColumnType("decimal(10,2)");

            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.Roles)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<MemberRole>>(v, (JsonSerializerOptions?)null) ?? new List<MemberRole>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Permissions)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Sports)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Teams)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Position)
                .HasMaxLength(100);

            entity.Property(e => e.SkillLevel)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.MedicalConditions)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Allergies)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.BloodType)
                .HasMaxLength(10);

            entity.Property(e => e.InsuranceProvider)
                .HasMaxLength(200);

            entity.Property(e => e.LastMedicalCheckup)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToDateOnly() : (DateOnly?)null,
                    v => v.HasValue ? LocalDate.FromDateOnly(v.Value) : (LocalDate?)null);

            entity.Property(e => e.CustomFields)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
                .HasColumnType("nvarchar(max)");

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

            // Emergency Contact Configuration
            entity.OwnsOne(e => e.EmergencyContact, contact =>
            {
                contact.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                contact.Property(c => c.Relationship)
                    .IsRequired()
                    .HasMaxLength(100);

                contact.Property(c => c.Phone)
                    .IsRequired()
                    .HasMaxLength(50);

                contact.Property(c => c.Email)
                    .HasMaxLength(255);
            });

            // Notification Preferences Configuration
            entity.OwnsOne(e => e.NotificationPreferences);
        });
    }
} 