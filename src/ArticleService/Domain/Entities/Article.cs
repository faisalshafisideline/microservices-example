using ArticleService.Domain.ValueObjects;

namespace ArticleService.Domain.Entities;

public sealed class Article
{
    public ArticleId Id { get; private set; } = default!;
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public AuthorId AuthorId { get; private set; } = default!;
    public string AuthorName { get; private set; } = string.Empty;
    public ArticleStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    private readonly List<string> _tags = [];
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();
    
    public ArticleMetadata? Metadata { get; private set; }

    // Required for EF Core
    private Article() { }

    public Article(
        ArticleId id,
        string title,
        string content,
        AuthorId authorId,
        string authorName,
        IEnumerable<string>? tags = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Title = !string.IsNullOrWhiteSpace(title) ? title : throw new ArgumentException("Title cannot be empty", nameof(title));
        Content = !string.IsNullOrWhiteSpace(content) ? content : throw new ArgumentException("Content cannot be empty", nameof(content));
        AuthorId = authorId ?? throw new ArgumentNullException(nameof(authorId));
        AuthorName = !string.IsNullOrWhiteSpace(authorName) ? authorName : throw new ArgumentException("Author name cannot be empty", nameof(authorName));
        Status = ArticleStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        if (tags?.Any() == true)
        {
            _tags.AddRange(tags.Where(t => !string.IsNullOrWhiteSpace(t)));
        }
    }

    public void UpdateContent(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        Content = content;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddTag(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        if (!_tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            _tags.Add(tag);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveTag(string tag)
    {
        if (_tags.Remove(tag))
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Publish()
    {
        if (Status == ArticleStatus.Published)
            return;

        Status = ArticleStatus.Published;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = ArticleStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMetadata(ArticleMetadata metadata)
    {
        Metadata = metadata;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ArticleStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
} 