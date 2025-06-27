using MassTransit;

namespace Shared.Contracts.UserContext.Extensions;

/// <summary>
/// Extensions for propagating user context through MassTransit messages
/// </summary>
public static class MassTransitUserContextExtensions
{
    /// <summary>
    /// Adds user context to message headers during publishing
    /// </summary>
    public static void SetUserContext(this SendContext context, UserContext userContext)
    {
        if (!string.IsNullOrEmpty(userContext.UserId))
            context.Headers.Set(UserContextConstants.RabbitUserIdKey, userContext.UserId);

        if (!string.IsNullOrEmpty(userContext.Username))
            context.Headers.Set(UserContextConstants.RabbitUsernameKey, userContext.Username);

        if (userContext.Roles.Any())
            context.Headers.Set(UserContextConstants.RabbitRolesKey, UserContextSerializer.SerializeRoles(userContext.Roles));

        context.Headers.Set(UserContextConstants.RabbitCorrelationIdKey, userContext.CorrelationId);

        if (!string.IsNullOrEmpty(userContext.TenantId))
            context.Headers.Set(UserContextConstants.RabbitTenantIdKey, userContext.TenantId);

        if (userContext.Claims.Any())
            context.Headers.Set(UserContextConstants.RabbitClaimsKey, UserContextSerializer.SerializeClaims(userContext.Claims));

        context.Headers.Set(UserContextConstants.RabbitTimestampKey, UserContextSerializer.SerializeTimestamp(userContext.Timestamp));
    }

    /// <summary>
    /// Extracts user context from message headers during consumption
    /// </summary>
    public static UserContext ExtractUserContext(this ConsumeContext context)
    {
        var headers = context.Headers;

        var userId = GetHeaderValue(headers, UserContextConstants.RabbitUserIdKey);
        var username = GetHeaderValue(headers, UserContextConstants.RabbitUsernameKey);
        var rolesString = GetHeaderValue(headers, UserContextConstants.RabbitRolesKey);
        var correlationId = GetHeaderValue(headers, UserContextConstants.RabbitCorrelationIdKey) 
                           ?? context.CorrelationId?.ToString() 
                           ?? Guid.NewGuid().ToString();
        var tenantId = GetHeaderValue(headers, UserContextConstants.RabbitTenantIdKey);
        var claimsString = GetHeaderValue(headers, UserContextConstants.RabbitClaimsKey);
        var timestampString = GetHeaderValue(headers, UserContextConstants.RabbitTimestampKey);

        var roles = UserContextSerializer.DeserializeRoles(rolesString);
        var claims = UserContextSerializer.DeserializeClaims(claimsString);
        var timestamp = UserContextSerializer.DeserializeTimestamp(timestampString);

        return new UserContext
        {
            UserId = userId,
            Username = username,
            Roles = roles,
            CorrelationId = correlationId,
            TenantId = tenantId,
            Claims = claims,
            Timestamp = timestamp
        };
    }

    private static string? GetHeaderValue(Headers headers, string key)
    {
        return headers.TryGetHeader(key, out var value) ? value?.ToString() : null;
    }
} 