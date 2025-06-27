using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Shared.Contracts.Security;

/// <summary>
/// Rate limiting service implementation using sliding window algorithm
/// </summary>
public class RateLimitingService : IRateLimitingService
{
    private readonly ILogger<RateLimitingService> _logger;
    private readonly IDistributedCache? _distributedCache;
    private readonly ConcurrentDictionary<string, RateLimitWindow> _localCache;
    private readonly RateLimitingConfiguration _config;

    public RateLimitingService(
        ILogger<RateLimitingService> logger,
        IDistributedCache? distributedCache = null,
        RateLimitingConfiguration? config = null)
    {
        _logger = logger;
        _distributedCache = distributedCache;
        _localCache = new ConcurrentDictionary<string, RateLimitWindow>();
        _config = config ?? new RateLimitingConfiguration();
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var windowStart = now.Subtract(window);

            RateLimitWindow rateLimitWindow;

            if (_distributedCache != null)
            {
                rateLimitWindow = await GetDistributedRateLimitWindowAsync(key, cancellationToken) ?? new RateLimitWindow();
            }
            else
            {
                rateLimitWindow = _localCache.GetOrAdd(key, _ => new RateLimitWindow());
            }

            // Clean old requests outside the window
            rateLimitWindow.Requests.RemoveAll(r => r < windowStart);

            // Check if limit is exceeded
            if (rateLimitWindow.Requests.Count >= maxRequests)
            {
                var oldestRequest = rateLimitWindow.Requests.Min();
                var retryAfter = oldestRequest.Add(window).Subtract(now);

                _logger.LogWarning("Rate limit exceeded for key: {Key}. Current: {Current}, Max: {Max}",
                    key, rateLimitWindow.Requests.Count, maxRequests);

                return new RateLimitResult
                {
                    IsAllowed = false,
                    RemainingRequests = 0,
                    RetryAfter = retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.Zero,
                    ReasonPhrase = "Rate limit exceeded"
                };
            }

            // Add current request
            rateLimitWindow.Requests.Add(now);

            if (_distributedCache != null)
            {
                await SetDistributedRateLimitWindowAsync(key, rateLimitWindow, window, cancellationToken);
            }

            var remaining = maxRequests - rateLimitWindow.Requests.Count;

            _logger.LogDebug("Rate limit check passed for key: {Key}. Remaining: {Remaining}",
                key, remaining);

            return new RateLimitResult
            {
                IsAllowed = true,
                RemainingRequests = remaining,
                RetryAfter = TimeSpan.Zero
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for key: {Key}", key);
            
            // Fail open - allow request if rate limiting fails
            return new RateLimitResult
            {
                IsAllowed = true,
                RemainingRequests = int.MaxValue,
                RetryAfter = TimeSpan.Zero,
                ReasonPhrase = "Rate limiting service error"
            };
        }
    }

    public async Task<RateLimitResult> CheckUserRateLimitAsync(string userId, string operation, CancellationToken cancellationToken = default)
    {
        var key = $"user:{userId}:{operation}";
        var limits = _config.GetUserLimits(operation);
        return await CheckRateLimitAsync(key, limits.MaxRequests, limits.Window, cancellationToken);
    }

    public async Task<RateLimitResult> CheckIpRateLimitAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var key = $"ip:{ipAddress}";
        var limits = _config.IpLimits;
        return await CheckRateLimitAsync(key, limits.MaxRequests, limits.Window, cancellationToken);
    }

    public async Task ResetRateLimitAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_distributedCache != null)
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);
            }
            else
            {
                _localCache.TryRemove(key, out _);
            }

            _logger.LogInformation("Reset rate limit for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting rate limit for key: {Key}", key);
        }
    }

    private async Task<RateLimitWindow?> GetDistributedRateLimitWindowAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var json = await _distributedCache!.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<RateLimitWindow>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting distributed rate limit window for key: {Key}", key);
            return null;
        }
    }

    private async Task SetDistributedRateLimitWindowAsync(string key, RateLimitWindow window, TimeSpan expiration, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(window);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _distributedCache!.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting distributed rate limit window for key: {Key}", key);
        }
    }
}

public class RateLimitWindow
{
    public List<DateTimeOffset> Requests { get; set; } = new();
}

public class RateLimitingConfiguration
{
    public RateLimitSettings IpLimits { get; set; } = new() { MaxRequests = 1000, Window = TimeSpan.FromHours(1) };
    public Dictionary<string, RateLimitSettings> UserOperationLimits { get; set; } = new()
    {
        ["login"] = new() { MaxRequests = 5, Window = TimeSpan.FromMinutes(15) },
        ["create_article"] = new() { MaxRequests = 10, Window = TimeSpan.FromMinutes(1) },
        ["api_call"] = new() { MaxRequests = 100, Window = TimeSpan.FromMinutes(1) },
        ["report_generation"] = new() { MaxRequests = 5, Window = TimeSpan.FromMinutes(5) }
    };

    public RateLimitSettings GetUserLimits(string operation)
    {
        return UserOperationLimits.TryGetValue(operation, out var limits) 
            ? limits 
            : new RateLimitSettings { MaxRequests = 60, Window = TimeSpan.FromMinutes(1) }; // Default
    }
}

public class RateLimitSettings
{
    public int MaxRequests { get; set; }
    public TimeSpan Window { get; set; }
} 