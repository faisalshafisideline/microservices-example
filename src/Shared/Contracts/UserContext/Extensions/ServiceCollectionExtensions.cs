using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Contracts.UserContext.Interceptors;

namespace Shared.Contracts.UserContext.Extensions;

/// <summary>
/// Extension methods for registering user context services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers user context services (IUserContextAccessor and implementation)
    /// </summary>
    public static IServiceCollection AddUserContext(this IServiceCollection services)
    {
        services.TryAddSingleton<IUserContextAccessor, UserContextAccessor>();
        return services;
    }

    /// <summary>
    /// Registers gRPC interceptors for user context propagation
    /// </summary>
    public static IServiceCollection AddUserContextGrpcInterceptors(this IServiceCollection services)
    {
        services.AddUserContext();
        services.TryAddSingleton<UserContextClientInterceptor>();
        services.TryAddSingleton<UserContextServerInterceptor>();
        return services;
    }
} 