using System.Text.Json.Serialization;

namespace Shared.Contracts.Messages;

/// <summary>
/// Event published when a new article is created
/// </summary>
public sealed record ArticleCreatedEvent
{
    [JsonPropertyName("eventId")]
    public required string EventId { get; init; }
    
    [JsonPropertyName("articleId")]
    public required string ArticleId { get; init; }
    
    [JsonPropertyName("title")]
    public required string Title { get; init; }
    
    [JsonPropertyName("authorId")] 
    public required string AuthorId { get; init; }
    
    [JsonPropertyName("authorName")]
    public required string AuthorName { get; init; }
    
    [JsonPropertyName("category")]
    public string? Category { get; init; }
    
    [JsonPropertyName("tags")]
    public string[] Tags { get; init; } = [];
    
    [JsonPropertyName("estimatedReadTimeMinutes")]
    public int EstimatedReadTimeMinutes { get; init; }
    
    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }
    
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }
    
    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0";
    
    /// <summary>
    /// Creates a new ArticleCreatedEvent with generated EventId and current timestamp
    /// </summary>
    public static ArticleCreatedEvent Create(
        string articleId,
        string title,
        string authorId,
        string authorName,
        string? category = null,
        string[]? tags = null,
        int estimatedReadTimeMinutes = 0,
        string? correlationId = null) => new()
    {
        EventId = Guid.NewGuid().ToString(),
        ArticleId = articleId,
        Title = title,
        AuthorId = authorId,
        AuthorName = authorName,
        Category = category,
        Tags = tags ?? [],
        EstimatedReadTimeMinutes = estimatedReadTimeMinutes,
        CreatedAt = DateTimeOffset.UtcNow,
        CorrelationId = correlationId
    };
} 