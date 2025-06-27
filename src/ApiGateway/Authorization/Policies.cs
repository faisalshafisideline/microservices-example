using Microsoft.AspNetCore.Authorization;

namespace ApiGateway.Authorization;

public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string ReporterOrAdmin = "ReporterOrAdmin";
    public const string AuthorOrAdmin = "AuthorOrAdmin";
    public const string AuthenticatedUser = "AuthenticatedUser";
    public const string PublicRead = "PublicRead";

    public static void ConfigurePolicies(AuthorizationOptions options)
    {
        // Admin-only access
        options.AddPolicy(AdminOnly, policy =>
            policy.RequireRole("Admin"));

        // Reporter or Admin access
        options.AddPolicy(ReporterOrAdmin, policy =>
            policy.RequireRole("Reporter", "Admin"));

        // Author or Admin access  
        options.AddPolicy(AuthorOrAdmin, policy =>
            policy.RequireRole("Author", "Admin"));

        // Any authenticated user
        options.AddPolicy(AuthenticatedUser, policy =>
            policy.RequireAuthenticatedUser());

        // Public read access (no authentication required)
        options.AddPolicy(PublicRead, policy =>
            policy.RequireAssertion(_ => true));
    }
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Reporter = "Reporter";
    public const string Author = "Author";
    public const string User = "User";
} 