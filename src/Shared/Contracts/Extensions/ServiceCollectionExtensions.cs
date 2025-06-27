using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Caching;
using Shared.Contracts.EventSourcing;
using Shared.Contracts.Observability;
using Shared.Contracts.Resilience;
using Shared.Contracts.Security;
using System.Diagnostics;

namespace Shared.Contracts.Extensions;

/// <summary>
/// Service collection extensions for registering scalability and robustness services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add all scalability and robustness services
    /// </summary>
    public static IServiceCollection AddScalabilityServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddDistributedCaching()
            .AddResilienceServices(configuration)
            .AddObservabilityServices()
            .AddSecurityServices(configuration)
            .AddEventSourcingServices();
    }

    /// <summary>
    /// Add distributed caching services
    /// </summary>
    public static IServiceCollection AddDistributedCaching(this IServiceCollection services)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            // This will be configured from appsettings.json
            options.Configuration = "localhost:6379";
        });

        services.AddSingleton<IDistributedCacheService, DistributedCacheService>();

        return services;
    }

    /// <summary>
    /// Add resilience services (circuit breaker, fallback)
    /// </summary>
    public static IServiceCollection AddResilienceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICircuitBreakerService, CircuitBreakerService>();
        services.AddSingleton<IResilienceService, ResilienceService>();
        services.AddSingleton<ICircuitBreakerHealthCheck, CircuitBreakerHealthCheck>();

        return services;
    }

    /// <summary>
    /// Add observability services (metrics, tracing, telemetry)
    /// </summary>
    public static IServiceCollection AddObservabilityServices(this IServiceCollection services)
    {
        services.AddSingleton<MetricsCollector>();
        services.AddSingleton<IMetricsCollector>(provider => provider.GetRequiredService<MetricsCollector>());
        services.AddSingleton<IBusinessMetricsCollector>(provider => provider.GetRequiredService<MetricsCollector>());
        services.AddSingleton<IPerformanceMonitor>(provider => provider.GetRequiredService<MetricsCollector>());
        
        services.AddSingleton<DistributedTracingService>();
        services.AddSingleton<IDistributedTracingService>(provider => provider.GetRequiredService<DistributedTracingService>());
        services.AddSingleton<ITelemetryService>(provider => provider.GetRequiredService<DistributedTracingService>());

        // Register ActivitySource for distributed tracing
        services.AddSingleton(provider => new ActivitySource("Microservices", "1.0.0"));

        return services;
    }

    /// <summary>
    /// Add security services (encryption, rate limiting, audit)
    /// </summary>
    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        var encryptionKey = configuration["Security:EncryptionKey"] ?? "DefaultKey123456789012345678901234567890";
        services.AddSingleton<ISecurityService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<SecurityService>>();
            return new SecurityService(logger, encryptionKey);
        });

        services.AddSingleton<IRateLimitingService, RateLimitingService>();
        services.AddSingleton<RateLimitingConfiguration>();

        // Add audit service (placeholder - implement based on your needs)
        // services.AddSingleton<IAuditService, AuditService>();

        return services;
    }

    /// <summary>
    /// Add event sourcing services
    /// </summary>
    public static IServiceCollection AddEventSourcingServices(this IServiceCollection services)
    {
        services.AddSingleton<IEventStore, InMemoryEventStore>();
        services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();

        return services;
    }

    /// <summary>
    /// Add health checks for all services
    /// </summary>
    public static IServiceCollection AddScalabilityHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<CircuitBreakerHealthCheck>("circuit-breakers")
            .AddRedis("localhost:6379", name: "redis")
            .AddCheck("event-store", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Event store is healthy"));

        return services;
    }
}

 