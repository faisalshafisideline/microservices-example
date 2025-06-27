namespace ArticleService.Domain.ValueObjects;

public sealed record ArticleMetadata
{
    public string? Category { get; }
    public int EstimatedReadTimeMinutes { get; }
    public string? Summary { get; }
    public string? FeaturedImageUrl { get; }

    public ArticleMetadata(
        string? category = null,
        int EstimatedReadTimeMinutes = 0,
        string? summary = null,
        string? featuredImageUrl = null)
    {
        if (EstimatedReadTimeMinutes < 0)
            throw new ArgumentException("Estimated read time cannot be negative", nameof(EstimatedReadTimeMinutes));

        if (!string.IsNullOrWhiteSpace(featuredImageUrl) && !Uri.TryCreate(featuredImageUrl, UriKind.Absolute, out _))
            throw new ArgumentException("Featured image URL must be a valid URL", nameof(featuredImageUrl));

        Category = category?.Trim();
        this.EstimatedReadTimeMinutes = EstimatedReadTimeMinutes;
        Summary = summary?.Trim();
        FeaturedImageUrl = featuredImageUrl?.Trim();
    }

    public static ArticleMetadata Create(
        string? category = null,
        string? summary = null,
        string? featuredImageUrl = null,
        string? content = null)
    {
        var estimatedReadTime = CalculateReadTime(content);
        return new ArticleMetadata(category, estimatedReadTime, summary, featuredImageUrl);
    }

    private static int CalculateReadTime(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        // Average reading speed: 200-250 words per minute
        const int wordsPerMinute = 225;
        var wordCount = content.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling((double)wordCount / wordsPerMinute));
    }
} 