using Microsoft.AspNetCore.Builder;

namespace Shared.Contracts.UserContext.Extensions;

/// <summary>
/// Extension methods for configuring user context middleware
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds user context middleware to the pipeline
    /// Should be added early in the pipeline, after authentication but before authorization
    /// </summary>
    public static IApplicationBuilder UseUserContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<UserContextMiddleware>();
    }
} 