using System.Text.Json;

namespace Shared.Contracts.UserContext;

/// <summary>
/// Utilities for serializing and deserializing user context for propagation
/// </summary>
public static class UserContextSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes roles to a comma-separated string
    /// </summary>
    public static string SerializeRoles(IReadOnlyList<string> roles)
    {
        return string.Join(UserContextConstants.RolesSeparator, roles);
    }

    /// <summary>
    /// Deserializes roles from a comma-separated string
    /// </summary>
    public static IReadOnlyList<string> DeserializeRoles(string? rolesString)
    {
        if (string.IsNullOrWhiteSpace(rolesString))
            return Array.Empty<string>();

        return rolesString.Split(UserContextConstants.RolesSeparator, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Serializes claims to a string format: key1=value1;key2=value2
    /// </summary>
    public static string SerializeClaims(IReadOnlyDictionary<string, string> claims)
    {
        if (!claims.Any())
            return string.Empty;

        return string.Join(UserContextConstants.ClaimsPairSeparator,
            claims.Select(kvp => $"{kvp.Key}{UserContextConstants.ClaimsKeyValueSeparator}{kvp.Value}"));
    }

    /// <summary>
    /// Deserializes claims from string format: key1=value1;key2=value2
    /// </summary>
    public static IReadOnlyDictionary<string, string> DeserializeClaims(string? claimsString)
    {
        if (string.IsNullOrWhiteSpace(claimsString))
            return new Dictionary<string, string>();

        var claims = new Dictionary<string, string>();

        var pairs = claimsString.Split(UserContextConstants.ClaimsPairSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split(UserContextConstants.ClaimsKeyValueSeparator, 2);
            if (keyValue.Length == 2)
            {
                claims[keyValue[0]] = keyValue[1];
            }
        }

        return claims;
    }

    /// <summary>
    /// Serializes UserContext to JSON (for complex scenarios)
    /// </summary>
    public static string SerializeToJson(UserContext context)
    {
        return JsonSerializer.Serialize(context, JsonOptions);
    }

    /// <summary>
    /// Deserializes UserContext from JSON
    /// </summary>
    public static UserContext? DeserializeFromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<UserContext>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Converts timestamp to string for transmission
    /// </summary>
    public static string SerializeTimestamp(DateTimeOffset timestamp)
    {
        return timestamp.ToString("O"); // ISO 8601 format
    }

    /// <summary>
    /// Parses timestamp from string
    /// </summary>
    public static DateTimeOffset DeserializeTimestamp(string? timestampString)
    {
        if (string.IsNullOrWhiteSpace(timestampString))
            return DateTimeOffset.UtcNow;

        return DateTimeOffset.TryParse(timestampString, out var timestamp) 
            ? timestamp 
            : DateTimeOffset.UtcNow;
    }
} 