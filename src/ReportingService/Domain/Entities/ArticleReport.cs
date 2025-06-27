using ReportingService.Domain.ValueObjects;

namespace ReportingService.Domain.Entities;

public sealed class ArticleReport
{
    public ArticleReportId Id { get; private set; } = default!;
    public ArticleId ArticleId { get; private set; } = default!;
    public string Title { get; private set; } = string.Empty;
    public string AuthorId { get; private set; } = string.Empty;
    public string AuthorName { get; private set; } = string.Empty;
    public string? Category { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastViewedAt { get; private set; }
    public int ViewCount { get; private set; }
    public int EstimatedReadTimeMinutes { get; private set; }

    private readonly List<string> _tags = [];
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    // Required for EF Core
    private ArticleReport() { }

    public ArticleReport(
        ArticleId articleId,
        string title,
        string authorId,
        string authorName,
        DateTime createdAt,
        string? category = null,
        IEnumerable<string>? tags = null,
        int estimatedReadTimeMinutes = 0)
    {
        Id = ArticleReportId.New();
        ArticleId = articleId ?? throw new ArgumentNullException(nameof(articleId));
        Title = !string.IsNullOrWhiteSpace(title) ? title : throw new ArgumentException("Title cannot be empty", nameof(title));
        AuthorId = !string.IsNullOrWhiteSpace(authorId) ? authorId : throw new ArgumentException("AuthorId cannot be empty", nameof(authorId));
        AuthorName = !string.IsNullOrWhiteSpace(authorName) ? authorName : throw new ArgumentException("AuthorName cannot be empty", nameof(authorName));
        CreatedAt = createdAt;
        LastViewedAt = DateTime.UtcNow;
        ViewCount = 0;
        Category = category;
        EstimatedReadTimeMinutes = Math.Max(0, estimatedReadTimeMinutes);

        if (tags?.Any() == true)
        {
            _tags.AddRange(tags.Where(t => !string.IsNullOrWhiteSpace(t)));
        }
    }

    public void RecordView()
    {
        ViewCount++;
        LastViewedAt = DateTime.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
    }

    public void UpdateCategory(string? category)
    {
        Category = category;
    }

    public void AddTag(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        if (!_tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            _tags.Add(tag);
        }
    }

    public void RemoveTag(string tag)
    {
        _tags.Remove(tag);
    }
} 