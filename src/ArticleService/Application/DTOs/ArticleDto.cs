using System.Text.Json.Serialization;

namespace ArticleService.Application.DTOs;

public sealed record ArticleDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("authorId")]
    public required string AuthorId { get; init; }

    [JsonPropertyName("authorName")]
    public required string AuthorName { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("tags")]
    public string[] Tags { get; init; } = [];

    [JsonPropertyName("metadata")]
    public ArticleMetadataDto? Metadata { get; init; }
}

public sealed record ArticleMetadataDto
{
    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("estimatedReadTimeMinutes")]
    public int EstimatedReadTimeMinutes { get; init; }

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("featuredImageUrl")]
    public string? FeaturedImageUrl { get; init; }
}

public sealed record ArticlesPageDto
{
    [JsonPropertyName("articles")]
    public ArticleDto[] Articles { get; init; } = [];

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }

    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; init; }
} 