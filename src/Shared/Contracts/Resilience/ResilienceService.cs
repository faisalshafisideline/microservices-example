using Microsoft.Extensions.Logging;

namespace Shared.Contracts.Resilience;

/// <summary>
/// Simple resilience service combining circuit breaker and fallback patterns
/// </summary>
public class ResilienceService : IResilienceService
{
    private readonly ICircuitBreakerService _circuitBreakerService;
    private readonly ILogger<ResilienceService> _logger;

    public ResilienceService(
        ICircuitBreakerService circuitBreakerService,
        ILogger<ResilienceService> logger)
    {
        _circuitBreakerService = circuitBreakerService;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(
        string operationKey,
        Func<Task<T>> operation,
        Func<Task<T>>? fallback = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _circuitBreakerService.ExecuteAsync(operationKey, operation, cancellationToken);
        }
        catch (Exception ex) when (fallback != null)
        {
            _logger.LogWarning(ex, "Operation {OperationKey} failed, executing fallback", operationKey);
            
            try
            {
                return await fallback();
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback for operation {OperationKey} also failed", operationKey);
                throw new AggregateException("Both primary operation and fallback failed", ex, fallbackEx);
            }
        }
    }

    public async Task ExecuteAsync(
        string operationKey,
        Func<Task> operation,
        Func<Task>? fallback = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _circuitBreakerService.ExecuteAsync(operationKey, operation, cancellationToken);
        }
        catch (Exception ex) when (fallback != null)
        {
            _logger.LogWarning(ex, "Operation {OperationKey} failed, executing fallback", operationKey);
            
            try
            {
                await fallback();
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback for operation {OperationKey} also failed", operationKey);
                throw new AggregateException("Both primary operation and fallback failed", ex, fallbackEx);
            }
        }
    }
}

/// <summary>
/// Circuit breaker health check implementation
/// </summary>
public class CircuitBreakerHealthCheck : ICircuitBreakerHealthCheck, Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly ICircuitBreakerService _circuitBreakerService;
    private readonly ILogger<CircuitBreakerHealthCheck> _logger;
    private readonly Dictionary<string, string> _serviceOperations;

    public CircuitBreakerHealthCheck(
        ICircuitBreakerService circuitBreakerService,
        ILogger<CircuitBreakerHealthCheck> logger)
    {
        _circuitBreakerService = circuitBreakerService;
        _logger = logger;
        _serviceOperations = new Dictionary<string, string>
        {
            { "article-service", "article-service-get" },
            { "reporting-service", "reporting-service-get" },
            { "database", "database-query" },
            { "cache", "cache-get" },
            { "message-queue", "message-publish" }
        };
    }

    public async Task<bool> IsHealthyAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        if (!_serviceOperations.TryGetValue(serviceName, out var operationKey))
        {
            _logger.LogWarning("Unknown service name: {ServiceName}", serviceName);
            return false;
        }

        var state = _circuitBreakerService.GetState(operationKey);
        var isHealthy = state == CircuitBreakerState.Closed || state == CircuitBreakerState.HalfOpen;
        
        _logger.LogDebug("Service {ServiceName} health check: {IsHealthy} (State: {State})", 
            serviceName, isHealthy, state);
        
        return await Task.FromResult(isHealthy);
    }

    public async Task<Dictionary<string, CircuitBreakerState>> GetAllStatesAsync(CancellationToken cancellationToken = default)
    {
        var states = new Dictionary<string, CircuitBreakerState>();
        
        foreach (var (serviceName, operationKey) in _serviceOperations)
        {
            states[serviceName] = _circuitBreakerService.GetState(operationKey);
        }
        
        return await Task.FromResult(states);
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var states = await GetAllStatesAsync(cancellationToken);
            var unhealthyServices = states.Where(s => s.Value == CircuitBreakerState.Open).ToList();
            
            if (unhealthyServices.Any())
            {
                var data = new Dictionary<string, object>();
                foreach (var service in states)
                {
                    data[service.Key] = service.Value.ToString();
                }
                
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                    $"Circuit breakers open for: {string.Join(", ", unhealthyServices.Select(s => s.Key))}", 
                    data: data);
            }
            
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("All circuit breakers are healthy");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Circuit breaker health check failed", ex);
        }
    }
} 