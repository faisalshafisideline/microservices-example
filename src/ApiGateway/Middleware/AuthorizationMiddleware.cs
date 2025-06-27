using ApiGateway.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace ApiGateway.Middleware;

public sealed class RouteBasedAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<RouteBasedAuthorizationMiddleware> _logger;

    public RouteBasedAuthorizationMiddleware(
        RequestDelegate next,
        IAuthorizationService authorizationService,
        ILogger<RouteBasedAuthorizationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var method = context.Request.Method.ToUpperInvariant();

        _logger.LogDebug("Processing request: {Method} {Path}", method, path);

        // Determine required policy based on route and method
        var requiredPolicy = DetermineRequiredPolicy(path, method);

        if (!string.IsNullOrEmpty(requiredPolicy))
        {
            _logger.LogDebug("Applying authorization policy: {Policy} for {Method} {Path}", 
                requiredPolicy, method, path);

            var authResult = await _authorizationService.AuthorizeAsync(
                context.User, 
                requiredPolicy);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("Authorization failed for {Method} {Path}. User: {User}, Policy: {Policy}", 
                    method, path, context.User.Identity?.Name ?? "Anonymous", requiredPolicy);

                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
                else
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Forbidden");
                    return;
                }
            }

            _logger.LogInformation("Authorization successful for {Method} {Path}. User: {User}", 
                method, path, context.User.Identity?.Name);
        }

        await _next(context);
    }

    private static string? DetermineRequiredPolicy(string path, string method)
    {
        return path switch
        {
            // Public endpoints (no authentication required)
            _ when path.StartsWith("/health") => null,
            _ when path.StartsWith("/swagger") => null,
            _ when path.StartsWith("/api/gateway") => null,

            // Article Service endpoints
            _ when path.StartsWith("/api/articles") && method == "GET" => Policies.PublicRead,
            _ when path.StartsWith("/api/articles") && method == "POST" => Policies.AuthorOrAdmin,
            _ when path.StartsWith("/api/articles") && method is "PUT" or "PATCH" => Policies.AuthorOrAdmin,
            _ when path.StartsWith("/api/articles") && method == "DELETE" => Policies.AdminOnly,

            // Reporting Service endpoints
            _ when path.StartsWith("/api/reporting") && method == "GET" => Policies.ReporterOrAdmin,
            _ when path.StartsWith("/api/reporting") && method == "POST" => Policies.ReporterOrAdmin,
            _ when path.StartsWith("/api/reporting") && method is "PUT" or "PATCH" => Policies.AdminOnly,
            _ when path.StartsWith("/api/reporting") && method == "DELETE" => Policies.AdminOnly,

            // Default: require authentication for all other endpoints
            _ => Policies.AuthenticatedUser
        };
    }
} 