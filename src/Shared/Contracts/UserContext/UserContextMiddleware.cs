using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Shared.Contracts.UserContext;

/// <summary>
/// Middleware to capture user context from HTTP headers and authentication claims
/// </summary>
public class UserContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserContextMiddleware> _logger;

    public UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserContextAccessor userContextAccessor)
    {
        try
        {
            var userContext = ExtractUserContext(context);
            userContextAccessor.SetContext(userContext);

            _logger.LogDebug("User context set: {UserContext}", userContext);

            // Add correlation ID to response headers for debugging
            context.Response.Headers.TryAdd(UserContextConstants.CorrelationIdHeader, userContext.CorrelationId);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UserContextMiddleware");
            throw;
        }
        finally
        {
            userContextAccessor.ClearContext();
        }
    }

    private UserContext ExtractUserContext(HttpContext context)
    {
        // Try to get correlation ID from headers first, then generate new one
        var correlationId = GetHeaderValue(context, UserContextConstants.CorrelationIdHeader) 
                           ?? Guid.NewGuid().ToString();

        // Extract from headers (set by API Gateway or previous services)
        var userId = GetHeaderValue(context, UserContextConstants.UserIdHeader);
        var username = GetHeaderValue(context, UserContextConstants.UsernameHeader);
        var rolesString = GetHeaderValue(context, UserContextConstants.RolesHeader);
        var tenantId = GetHeaderValue(context, UserContextConstants.TenantIdHeader);
        var claimsString = GetHeaderValue(context, UserContextConstants.ClaimsHeader);
        var timestampString = GetHeaderValue(context, UserContextConstants.TimestampHeader);

        // If headers are not present, try to extract from authentication claims
        if (string.IsNullOrEmpty(userId) && context.User.Identity?.IsAuthenticated == true)
        {
            userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.User.FindFirst("sub")?.Value
                    ?? context.User.FindFirst("user_id")?.Value;

            username = context.User.FindFirst(ClaimTypes.Name)?.Value
                      ?? context.User.FindFirst("preferred_username")?.Value
                      ?? context.User.FindFirst("username")?.Value;

            // Extract roles from claims if not in headers
            if (string.IsNullOrEmpty(rolesString))
            {
                var roleClaims = context.User.FindAll(ClaimTypes.Role)
                    .Concat(context.User.FindAll("role"))
                    .Select(c => c.Value)
                    .ToList();

                if (roleClaims.Any())
                {
                    rolesString = string.Join(UserContextConstants.RolesSeparator, roleClaims);
                }
            }

            // Extract additional claims if not in headers
            if (string.IsNullOrEmpty(claimsString))
            {
                var additionalClaims = context.User.Claims
                    .Where(c => !IsStandardClaim(c.Type))
                    .ToDictionary(c => c.Type, c => c.Value);

                if (additionalClaims.Any())
                {
                    claimsString = UserContextSerializer.SerializeClaims(additionalClaims);
                }
            }
        }

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

    private static string? GetHeaderValue(HttpContext context, string headerName)
    {
        return context.Request.Headers.TryGetValue(headerName, out var values) 
            ? values.FirstOrDefault() 
            : null;
    }

    private static bool IsStandardClaim(string claimType)
    {
        return claimType switch
        {
            ClaimTypes.NameIdentifier or ClaimTypes.Name or ClaimTypes.Role or
            "sub" or "preferred_username" or "username" or "role" or "user_id" => true,
            _ => false
        };
    }
} 