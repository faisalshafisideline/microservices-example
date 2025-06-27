namespace Shared.Contracts.Resilience;

/// <summary>
/// Circuit breaker states
/// </summary>
public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}

/// <summary>
/// Circuit breaker service for resilient service calls
/// </summary>
public interface ICircuitBreakerService
{
    Task<T> ExecuteAsync<T>(string operationKey, Func<Task<T>> operation, CancellationToken cancellationToken = default);
    Task ExecuteAsync(string operationKey, Func<Task> operation, CancellationToken cancellationToken = default);
    CircuitBreakerState GetState(string operationKey);
    void Reset(string operationKey);
}

/// <summary>
/// Resilience policies configuration
/// </summary>
public class ResiliencePoliciesOptions
{
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
    public RetryOptions Retry { get; set; } = new();
    public TimeoutOptions Timeout { get; set; } = new();
    public BulkheadOptions Bulkhead { get; set; } = new();
}

public class CircuitBreakerOptions
{
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);
    public int MinimumThroughput { get; set; } = 10;
}

public class RetryOptions
{
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public bool UseJitter { get; set; } = true;
}

public class TimeoutOptions
{
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public Dictionary<string, TimeSpan> OperationTimeouts { get; set; } = new();
}

public class BulkheadOptions
{
    public int MaxParallelization { get; set; } = 10;
    public int MaxQueuingActions { get; set; } = 25;
}

/// <summary>
/// Resilience service combining multiple patterns
/// </summary>
public interface IResilienceService
{
    Task<T> ExecuteAsync<T>(
        string operationKey, 
        Func<Task<T>> operation, 
        Func<Task<T>>? fallback = null,
        CancellationToken cancellationToken = default);
        
    Task ExecuteAsync(
        string operationKey, 
        Func<Task> operation, 
        Func<Task>? fallback = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Health check integration for circuit breakers
/// </summary>
public interface ICircuitBreakerHealthCheck
{
    Task<bool> IsHealthyAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<Dictionary<string, CircuitBreakerState>> GetAllStatesAsync(CancellationToken cancellationToken = default);
} 