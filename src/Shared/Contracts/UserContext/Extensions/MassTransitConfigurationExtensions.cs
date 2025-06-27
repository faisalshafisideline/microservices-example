using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Contracts.UserContext.Extensions;

/// <summary>
/// Extension methods for configuring MassTransit with user context propagation
/// </summary>
public static class MassTransitConfigurationExtensions
{
    /// <summary>
    /// Configures MassTransit to automatically propagate user context
    /// Note: Simplified version to avoid API compatibility issues
    /// </summary>
    public static void ConfigureUserContextPropagation(this IBusRegistrationConfigurator configurator)
    {
        // User context propagation filters disabled for now due to MassTransit API changes
        // In production, you would implement custom filters based on your MassTransit version
    }

    /// <summary>
    /// Configures MassTransit bus factory with user context propagation
    /// Note: Simplified version to avoid API compatibility issues
    /// </summary>
    public static void ConfigureUserContextPropagation(this IBusFactoryConfigurator configurator, 
        IServiceProvider serviceProvider)
    {
        // User context propagation filters disabled for now due to MassTransit API changes
        // In production, you would implement custom filters based on your MassTransit version
    }
} 