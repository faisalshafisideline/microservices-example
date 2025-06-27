using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts.UserContext.Extensions;

namespace Shared.Contracts.UserContext.Filters;

/// <summary>
/// MassTransit publish filter that automatically injects user context into messages
/// </summary>
public class UserContextPublishFilter<T> : IFilter<PublishContext<T>>
    where T : class
{
    private readonly IUserContextAccessor _userContextAccessor;
    private readonly ILogger<UserContextPublishFilter<T>> _logger;

    public UserContextPublishFilter(
        IUserContextAccessor userContextAccessor,
        ILogger<UserContextPublishFilter<T>> logger)
    {
        _userContextAccessor = userContextAccessor;
        _logger = logger;
    }

    public async Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        try
        {
            var userContext = _userContextAccessor.GetCurrentOrEmpty();
            context.SetUserContext(userContext);

            _logger.LogDebug("Injected user context into published message: {MessageType}, CorrelationId: {CorrelationId}",
                typeof(T).Name, userContext.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to inject user context into published message: {MessageType}", typeof(T).Name);
        }

        await next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("userContext");
    }
} 