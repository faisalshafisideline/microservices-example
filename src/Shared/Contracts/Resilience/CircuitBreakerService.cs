using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Shared.Contracts.Resilience;

/// <summary>
/// Simple circuit breaker service implementation
/// </summary>
public class CircuitBreakerService : ICircuitBreakerService
{
    private readonly ILogger<CircuitBreakerService> _logger;
    private readonly ConcurrentDictionary<string, CircuitBreakerInfo> _circuitBreakers;

    public CircuitBreakerService(ILogger<CircuitBreakerService> logger)
    {
        _logger = logger;
        _circuitBreakers = new ConcurrentDictionary<string, CircuitBreakerInfo>();
    }

    public async Task<T> ExecuteAsync<T>(string operationKey, Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var circuitBreaker = _circuitBreakers.GetOrAdd(operationKey, _ => new CircuitBreakerInfo());
        
        if (circuitBreaker.State == CircuitBreakerState.Open)
        {
            if (DateTimeOffset.UtcNow < circuitBreaker.NextAttempt)
            {
                _logger.LogWarning("Circuit breaker is open for operation: {OperationKey}", operationKey);
                throw new InvalidOperationException($"Circuit breaker is open for operation: {operationKey}");
            }
            
            circuitBreaker.State = CircuitBreakerState.HalfOpen;
            _logger.LogInformation("Circuit breaker is half-open for operation: {OperationKey}", operationKey);
        }

        try
        {
            var result = await operation();
            
            if (circuitBreaker.State == CircuitBreakerState.HalfOpen)
            {
                circuitBreaker.State = CircuitBreakerState.Closed;
                circuitBreaker.FailureCount = 0;
                _logger.LogInformation("Circuit breaker closed for operation: {OperationKey}", operationKey);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            circuitBreaker.FailureCount++;
            
            if (circuitBreaker.FailureCount >= 5) // Simple threshold
            {
                circuitBreaker.State = CircuitBreakerState.Open;
                circuitBreaker.NextAttempt = DateTimeOffset.UtcNow.AddSeconds(30);
                _logger.LogWarning("Circuit breaker opened for operation: {OperationKey} after {FailureCount} failures", 
                    operationKey, circuitBreaker.FailureCount);
            }
            
            _logger.LogError(ex, "Operation failed: {OperationKey}", operationKey);
            throw;
        }
    }

    public async Task ExecuteAsync(string operationKey, Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(operationKey, async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
    }

    public CircuitBreakerState GetState(string operationKey)
    {
        return _circuitBreakers.TryGetValue(operationKey, out var circuitBreaker) 
            ? circuitBreaker.State 
            : CircuitBreakerState.Closed;
    }

    public void Reset(string operationKey)
    {
        if (_circuitBreakers.TryGetValue(operationKey, out var circuitBreaker))
        {
            circuitBreaker.State = CircuitBreakerState.Closed;
            circuitBreaker.FailureCount = 0;
            _logger.LogInformation("Circuit breaker reset for operation: {OperationKey}", operationKey);
        }
    }

    private class CircuitBreakerInfo
    {
        public CircuitBreakerState State { get; set; } = CircuitBreakerState.Closed;
        public int FailureCount { get; set; } = 0;
        public DateTimeOffset NextAttempt { get; set; } = DateTimeOffset.MinValue;
    }
} 