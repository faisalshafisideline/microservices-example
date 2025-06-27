namespace Shared.Contracts.UserContext;

/// <summary>
/// Represents the user context that flows through the system
/// </summary>
public record UserContext
{
    public string? UserId { get; init; }
    public string? Username { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();
    public string? TenantId { get; init; }
    public IReadOnlyDictionary<string, string> Claims { get; init; } = new Dictionary<string, string>();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates an empty/anonymous context with only correlation ID
    /// </summary>
    public static UserContext Empty => new() { CorrelationId = Guid.NewGuid().ToString() };

    /// <summary>
    /// Creates a system context for background operations
    /// </summary>
    public static UserContext System => new()
    {
        UserId = "system",
        Username = "system",
        Roles = new[] { "System" },
        CorrelationId = Guid.NewGuid().ToString()
    };

    /// <summary>
    /// Checks if the user has a specific role
    /// </summary>
    public bool HasRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the user has any of the specified roles
    /// </summary>
    public bool HasAnyRole(params string[] roles) => 
        roles.Any(role => HasRole(role));

    /// <summary>
    /// Gets a claim value by key
    /// </summary>
    public string? GetClaim(string key) => 
        Claims.TryGetValue(key, out var value) ? value : null;

    /// <summary>
    /// Creates a new UserContext with additional claims
    /// </summary>
    public UserContext WithClaims(IDictionary<string, string> additionalClaims)
    {
        var newClaims = new Dictionary<string, string>(Claims);
        foreach (var claim in additionalClaims)
        {
            newClaims[claim.Key] = claim.Value;
        }

        return this with { Claims = newClaims };
    }

    /// <summary>
    /// Creates a new UserContext with a new correlation ID (for new request chains)
    /// </summary>
    public UserContext WithNewCorrelationId() => 
        this with { CorrelationId = Guid.NewGuid().ToString() };

    public override string ToString() => 
        $"User: {Username} ({UserId}), Roles: [{string.Join(", ", Roles)}], CorrelationId: {CorrelationId}";
} 