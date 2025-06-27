using System.Text.Json.Serialization;

namespace Shared.Contracts.Messages;

/// <summary>
/// Event published when an article is viewed (for analytics tracking)
/// </summary>
public sealed record ArticleViewedEvent
{
    [JsonPropertyName("eventId")]
    public required string EventId { get; init; }
    
    [JsonPropertyName("articleId")]
    public required string ArticleId { get; init; }
    
    [JsonPropertyName("userId")]
    public string? UserId { get; init; }
    
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; init; }
    
    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; init; }
    
    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; init; }
    
    [JsonPropertyName("referrer")]
    public string? Referrer { get; init; }
    
    [JsonPropertyName("viewedAt")]
    public required DateTimeOffset ViewedAt { get; init; }
    
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }
    
    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0";
    
    /// <summary>
    /// Creates a new ArticleViewedEvent with generated EventId and current timestamp
    /// </summary>
    public static ArticleViewedEvent Create(
        string articleId,
        string? userId = null,
        string? sessionId = null,
        string? userAgent = null,
        string? ipAddress = null,
        string? referrer = null,
        string? correlationId = null) => new()
    {
        EventId = Guid.NewGuid().ToString(),
        ArticleId = articleId,
        UserId = userId,
        SessionId = sessionId,
        UserAgent = userAgent,
        IpAddress = ipAddress,
        Referrer = referrer,
        ViewedAt = DateTimeOffset.UtcNow,
        CorrelationId = correlationId
    };
} 