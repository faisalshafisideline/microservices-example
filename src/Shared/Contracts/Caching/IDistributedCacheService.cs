namespace Shared.Contracts.Caching;

/// <summary>
/// Abstraction for distributed caching across microservices
/// </summary>
public interface IDistributedCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Cache key builder for consistent key generation
/// </summary>
public static class CacheKeyBuilder
{
    private const string Separator = ":";
    
    public static string BuildKey(string service, string entity, params object[] identifiers)
    {
        var parts = new List<string> { service.ToLowerInvariant(), entity.ToLowerInvariant() };
        parts.AddRange(identifiers.Select(id => id.ToString()!.ToLowerInvariant()));
        return string.Join(Separator, parts);
    }
    
    public static string BuildUserKey(string userId, string dataType)
        => BuildKey("user", dataType, userId);
    
    public static string BuildArticleKey(Guid articleId)
        => BuildKey("article", "details", articleId);
    
    public static string BuildUserArticlesKey(string userId)
        => BuildKey("user", "articles", userId);
    
    public static string BuildReportKey(string reportType, params object[] parameters)
        => BuildKey("report", reportType, parameters);
} 