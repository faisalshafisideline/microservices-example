using ApiGateway.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiGateway.Endpoints;

public static class GatewayEndpoints
{
    public static void MapGatewayEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/gateway")
            .WithTags("API Gateway")
            .WithOpenApi();

        group.MapGet("/", GetGatewayInfo)
            .WithName("GetGatewayInfo")
            .WithSummary("Get API Gateway information")
            .AllowAnonymous();

        group.MapGet("/user", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("Get current authenticated user information")
            .RequireAuthorization(Policies.AuthenticatedUser);

        group.MapGet("/health", GetGatewayHealth)
            .WithName("GetGatewayHealth")
            .WithSummary("Get API Gateway health status")
            .AllowAnonymous();

        group.MapPost("/auth/test", TestAuthentication)
            .WithName("TestAuthentication")
            .WithSummary("Test authentication with different roles")
            .RequireAuthorization();

        group.MapGet("/routes", GetAvailableRoutes)
            .WithName("GetAvailableRoutes")
            .WithSummary("Get available routes and their authorization requirements")
            .RequireAuthorization(Policies.AdminOnly);
    }

    private static IResult GetGatewayInfo()
    {
        var info = new
        {
            Name = "Microservices API Gateway",
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            Timestamp = DateTimeOffset.UtcNow,
            Authentication = new
            {
                Scheme = "Basic",
                SupportedRoles = new[] { "Admin", "Reporter", "Author", "User" }
            },
            Services = new
            {
                ArticleService = "/api/articles",
                ReportingService = "/api/reporting"
            }
        };

        return Results.Ok(info);
    }

    private static IResult GetCurrentUser(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var userInfo = new
        {
            Id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            Username = user.FindFirst(ClaimTypes.Name)?.Value,
            Email = user.FindFirst(ClaimTypes.Email)?.Value,
            FullName = user.FindFirst("FullName")?.Value,
            Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
            AuthenticationType = user.Identity.AuthenticationType,
            IsAuthenticated = user.Identity.IsAuthenticated
        };

        return Results.Ok(userInfo);
    }

    private static IResult GetGatewayHealth()
    {
        var health = new
        {
            Status = "Healthy",
            Timestamp = DateTimeOffset.UtcNow,
            Version = "1.0.0",
            Uptime = Environment.TickCount64,
            Services = new
            {
                ArticleService = "Proxied",
                ReportingService = "Proxied"
            }
        };

        return Results.Ok(health);
    }

    private static IResult TestAuthentication(ClaimsPrincipal user, [FromBody] AuthTestRequest? request)
    {
        var result = new
        {
            IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
            Username = user.Identity?.Name,
            Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
            RequestedAction = request?.Action ?? "none",
            HasAdminRole = user.IsInRole(Roles.Admin),
            HasReporterRole = user.IsInRole(Roles.Reporter),
            HasAuthorRole = user.IsInRole(Roles.Author),
            HasUserRole = user.IsInRole(Roles.User),
            TestResult = "Authentication successful"
        };

        return Results.Ok(result);
    }

    private static IResult GetAvailableRoutes()
    {
        var routes = new[]
        {
            new RouteInfo
            {
                Path = "/api/articles",
                Methods = ["GET"],
                Authorization = "Public (no authentication required)",
                Description = "Get articles from Article Service"
            },
            new RouteInfo
            {
                Path = "/api/articles",
                Methods = ["POST"],
                Authorization = "Author or Admin role required",
                Description = "Create new article"
            },
            new RouteInfo
            {
                Path = "/api/articles/{id}",
                Methods = ["PUT", "PATCH"],
                Authorization = "Author or Admin role required", 
                Description = "Update existing article"
            },
            new RouteInfo
            {
                Path = "/api/articles/{id}",
                Methods = ["DELETE"],
                Authorization = "Admin role required",
                Description = "Delete article"
            },
            new RouteInfo
            {
                Path = "/api/reporting",
                Methods = ["GET"],
                Authorization = "Reporter or Admin role required",
                Description = "Access reporting data"
            },
            new RouteInfo
            {
                Path = "/api/reporting",
                Methods = ["POST"],
                Authorization = "Reporter or Admin role required",
                Description = "Create reporting entries"
            },
            new RouteInfo
            {
                Path = "/api/gateway/user",
                Methods = ["GET"],
                Authorization = "Any authenticated user",
                Description = "Get current user information"
            }
        };

        return Results.Ok(routes);
    }
}

public sealed record AuthTestRequest
{
    public string? Action { get; init; }
}

public sealed record RouteInfo
{
    public required string Path { get; init; }
    public required string[] Methods { get; init; }
    public required string Authorization { get; init; }
    public required string Description { get; init; }
} 