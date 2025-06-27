using Shared.Contracts.UserContext;
using System.Security.Claims;

namespace ApiGateway.Middleware;

/// <summary>
/// Middleware that enriches user context from authentication and adds headers for downstream services
/// </summary>
public class UserContextEnrichmentMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserContextEnrichmentMiddleware> _logger;

    public UserContextEnrichmentMiddleware(RequestDelegate next, ILogger<UserContextEnrichmentMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserContextAccessor userContextAccessor)
    {
        try
        {
            var userContext = ExtractUserContextFromAuthentication(context);
            userContextAccessor.SetContext(userContext);

            // Add user context headers for downstream services
            AddUserContextHeaders(context, userContext);

            _logger.LogInformation("User context enriched for gateway request: {UserContext}", userContext);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UserContextEnrichmentMiddleware");
            throw;
        }
        finally
        {
            userContextAccessor.ClearContext();
        }
    }

    private UserContext ExtractUserContextFromAuthentication(HttpContext context)
    {
        // Generate or extract correlation ID
        var correlationId = context.Request.Headers[UserContextConstants.CorrelationIdHeader].FirstOrDefault()
                           ?? Guid.NewGuid().ToString();

        if (!context.User.Identity?.IsAuthenticated == true)
        {
            return new UserContext { CorrelationId = correlationId };
        }

        // Extract user information from authentication claims
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.User.FindFirst("user_id")?.Value;

        var username = context.User.FindFirst(ClaimTypes.Name)?.Value
                      ?? context.User.FindFirst("username")?.Value;

        var roles = context.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        // Extract additional claims
        var additionalClaims = context.User.Claims
            .Where(c => !IsStandardClaim(c.Type))
            .ToDictionary(c => c.Type, c => c.Value);

        return new UserContext
        {
            UserId = userId,
            Username = username,
            Roles = roles,
            CorrelationId = correlationId,
            Claims = additionalClaims,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static void AddUserContextHeaders(HttpContext context, UserContext userContext)
    {
        // Add headers that will be forwarded to downstream services
        var headers = context.Request.Headers;

        if (!string.IsNullOrEmpty(userContext.UserId))
            headers[UserContextConstants.UserIdHeader] = userContext.UserId;

        if (!string.IsNullOrEmpty(userContext.Username))
            headers[UserContextConstants.UsernameHeader] = userContext.Username;

        if (userContext.Roles.Any())
            headers[UserContextConstants.RolesHeader] = UserContextSerializer.SerializeRoles(userContext.Roles);

        headers[UserContextConstants.CorrelationIdHeader] = userContext.CorrelationId;

        if (!string.IsNullOrEmpty(userContext.TenantId))
            headers[UserContextConstants.TenantIdHeader] = userContext.TenantId;

        if (userContext.Claims.Any())
            headers[UserContextConstants.ClaimsHeader] = UserContextSerializer.SerializeClaims(userContext.Claims);

        headers[UserContextConstants.TimestampHeader] = UserContextSerializer.SerializeTimestamp(userContext.Timestamp);
    }

    private static bool IsStandardClaim(string claimType)
    {
        return claimType switch
        {
            ClaimTypes.NameIdentifier or ClaimTypes.Name or ClaimTypes.Role or
            "user_id" or "username" => true,
            _ => false
        };
    }
} 