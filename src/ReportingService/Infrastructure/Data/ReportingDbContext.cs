using Microsoft.EntityFrameworkCore;
using ReportingService.Domain.Entities;
using ReportingService.Domain.ValueObjects;
using System.Text.Json;

namespace ReportingService.Infrastructure.Data;

public sealed class ReportingDbContext : DbContext
{
    public DbSet<ArticleReport> ArticleReports { get; set; } = default!;

    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        ConfigureArticleReportEntity(modelBuilder);
    }

    private static void ConfigureArticleReportEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ArticleReport>();

        // Configure table
        entity.ToTable("ArticleReports");

        // Configure primary key
        entity.HasKey(ar => ar.Id);

        // Configure ArticleReportId as value object
        entity.Property(ar => ar.Id)
            .HasConversion(
                id => id.Value,
                value => new ArticleReportId(value))
            .HasMaxLength(100);

        // Configure ArticleId as value object
        entity.Property(ar => ar.ArticleId)
            .HasConversion(
                id => id.Value,
                value => new ArticleId(value))
            .HasMaxLength(100);

        // Configure string properties
        entity.Property(ar => ar.Title)
            .HasMaxLength(500)
            .IsRequired();

        entity.Property(ar => ar.AuthorId)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(ar => ar.AuthorName)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(ar => ar.Category)
            .HasMaxLength(100);

        // Configure timestamp properties
        entity.Property(ar => ar.CreatedAt);
        entity.Property(ar => ar.LastViewedAt);

        // Configure numeric properties
        entity.Property(ar => ar.ViewCount)
            .HasDefaultValue(0);

        entity.Property(ar => ar.EstimatedReadTimeMinutes)
            .HasDefaultValue(0);

        // Configure Tags as JSON
        entity.Property<List<string>>("_tags")
            .HasColumnName("Tags")
            .HasConversion(
                tags => JsonSerializer.Serialize(tags, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("nvarchar(max)");

        // Configure indexes
        entity.HasIndex(ar => ar.ArticleId)
            .IsUnique();

        entity.HasIndex(ar => ar.AuthorId);
        entity.HasIndex(ar => ar.Category)
            .HasFilter("[Category] IS NOT NULL");
        entity.HasIndex(ar => ar.CreatedAt);
        entity.HasIndex(ar => ar.ViewCount);
    }
} 