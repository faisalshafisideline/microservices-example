using ArticleService.Domain.Entities;
using ArticleService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace ArticleService.Infrastructure.Data;

public sealed class ArticleDbContext : DbContext
{
    public DbSet<Article> Articles { get; set; } = default!;

    public ArticleDbContext(DbContextOptions<ArticleDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        ConfigureArticleEntity(modelBuilder);
    }

    private static void ConfigureArticleEntity(ModelBuilder modelBuilder)
    {
        var articleEntity = modelBuilder.Entity<Article>();

        // Configure table
        articleEntity.ToTable("Articles");

        // Configure primary key
        articleEntity.HasKey(a => a.Id);

        // Configure ArticleId as value object
        articleEntity.Property(a => a.Id)
            .HasConversion(
                id => id.Value,
                value => new ArticleId(value))
            .HasMaxLength(100);

        // Configure AuthorId as value object
        articleEntity.Property(a => a.AuthorId)
            .HasConversion(
                id => id.Value,
                value => new AuthorId(value))
            .HasMaxLength(100);

        // Configure string properties
        articleEntity.Property(a => a.Title)
            .HasMaxLength(500)
            .IsRequired();

        articleEntity.Property(a => a.Content)
            .IsRequired();

        articleEntity.Property(a => a.AuthorName)
            .HasMaxLength(200)
            .IsRequired();

        // Configure enum
        articleEntity.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Configure timestamp properties
        articleEntity.Property(a => a.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        articleEntity.Property(a => a.UpdatedAt);

        // Configure Tags as JSON
        articleEntity.Property<List<string>>("_tags")
            .HasColumnName("Tags")
            .HasConversion(
                tags => JsonSerializer.Serialize(tags, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("nvarchar(max)");

        // Configure ArticleMetadata as owned entity (JSON column)
        articleEntity.OwnsOne(a => a.Metadata, metadata =>
        {
            metadata.ToJson();
            metadata.Property(m => m.Category).HasMaxLength(100);
            metadata.Property(m => m.Summary).HasMaxLength(1000);
            metadata.Property(m => m.FeaturedImageUrl).HasMaxLength(500);
        });

        // Configure indexes
        articleEntity.HasIndex(a => a.AuthorId);
        articleEntity.HasIndex(a => a.Status);
        articleEntity.HasIndex(a => a.CreatedAt);
    }
} 