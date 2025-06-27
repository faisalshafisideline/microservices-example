using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts.UserContext.Extensions;

namespace Shared.Contracts.UserContext.Filters;

/// <summary>
/// MassTransit consume filter that automatically extracts user context from messages
/// </summary>
public class UserContextConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly IUserContextAccessor _userContextAccessor;
    private readonly ILogger<UserContextConsumeFilter<T>> _logger;

    public UserContextConsumeFilter(
        IUserContextAccessor userContextAccessor,
        ILogger<UserContextConsumeFilter<T>> logger)
    {
        _userContextAccessor = userContextAccessor;
        _logger = logger;
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        try
        {
            var userContext = context.ExtractUserContext();
            _userContextAccessor.SetContext(userContext);

            _logger.LogDebug("Extracted user context from consumed message: {MessageType}, CorrelationId: {CorrelationId}",
                typeof(T).Name, userContext.CorrelationId);

            await next.Send(context);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract user context from consumed message: {MessageType}", typeof(T).Name);
            
            // Set empty context to ensure we don't have stale context
            _userContextAccessor.SetContext(UserContext.Empty);
            await next.Send(context);
        }
        finally
        {
            _userContextAccessor.ClearContext();
        }
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("userContext");
    }
} 