using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Shared.Contracts.Caching;

/// <summary>
/// Redis-based distributed cache service implementation
/// </summary>
public class DistributedCacheService : IDistributedCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public DistributedCacheService(
        IDistributedCache distributedCache,
        ILogger<DistributedCacheService> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                options.SetSlidingExpiration(TimeSpan.FromHours(1)); // Default 1 hour
            }

            await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Removed cache entry for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Note: This requires Redis-specific implementation
        // For now, we'll log a warning as this isn't supported by IDistributedCache
        _logger.LogWarning("RemoveByPatternAsync is not supported by IDistributedCache. Pattern: {Pattern}", pattern);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _distributedCache.GetAsync(key, cancellationToken);
            return value != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var tasks = keyValuePairs.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiration, cancellationToken));
        await Task.WhenAll(tasks);
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        var tasks = keys.Select(async key => new { Key = key, Value = await GetAsync<T>(key, cancellationToken) });
        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r.Key, r => r.Value);
    }
} 