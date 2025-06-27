using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Shared.Contracts.Observability;

/// <summary>
/// Custom metrics collection interface
/// </summary>
public interface IMetricsCollector
{
    void RecordRequestDuration(string operationType, string endpoint, double durationMs, string? status = null);
    void IncrementCounter(string name, string[]? tags = null);
    void RecordGauge(string name, double value, string[]? tags = null);
    void RecordHistogram(string name, double value, string[]? tags = null);
    void RecordUserContextMetrics(string userId, string operation, bool success);
    void RecordCircuitBreakerMetrics(string serviceName, string state, int failureCount);
}

/// <summary>
/// Distributed tracing service
/// </summary>
public interface IDistributedTracingService
{
    Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal);
    void AddTag(string key, string value);
    void AddEvent(string name, DateTimeOffset? timestamp = null, ActivityTagsCollection? tags = null);
    void SetStatus(ActivityStatusCode status, string? description = null);
    void RecordException(Exception exception);
}

/// <summary>
/// Application insights and custom telemetry
/// </summary>
public interface ITelemetryService
{
    void TrackDependency(string dependencyType, string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, bool success);
    void TrackEvent(string eventName, Dictionary<string, string>? properties = null, Dictionary<string, double>? metrics = null);
    void TrackException(Exception exception, Dictionary<string, string>? properties = null);
    void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success);
    void TrackTrace(string message, SeverityLevel severityLevel, Dictionary<string, string>? properties = null);
}

public enum SeverityLevel
{
    Verbose,
    Information,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Business metrics tracking
/// </summary>
public interface IBusinessMetricsCollector
{
    void TrackArticleCreated(string userId, string category);
    void TrackArticleViewed(string articleId, string? userId = null);
    void TrackUserLogin(string userId, bool success, string? provider = null);
    void TrackReportGenerated(string reportType, string userId, TimeSpan duration);
    void TrackApiGatewayRequest(string endpoint, string method, int statusCode, TimeSpan duration);
}

/// <summary>
/// Performance monitoring service
/// </summary>
public interface IPerformanceMonitor
{
    IDisposable StartOperation(string operationName);
    void RecordDatabaseQuery(string queryType, TimeSpan duration, bool success);
    void RecordCacheOperation(string operation, bool hit, TimeSpan duration);
    void RecordMessageProcessing(string messageType, TimeSpan duration, bool success);
    void RecordGrpcCall(string method, TimeSpan duration, string status);
}

/// <summary>
/// Health monitoring service
/// </summary>
public interface IHealthMonitor
{
    Task<HealthStatus> CheckDatabaseHealthAsync(CancellationToken cancellationToken = default);
    Task<HealthStatus> CheckMessageQueueHealthAsync(CancellationToken cancellationToken = default);
    Task<HealthStatus> CheckExternalServiceHealthAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<HealthStatus> CheckCacheHealthAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, HealthStatus>> GetAllHealthStatusesAsync(CancellationToken cancellationToken = default);
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

public class HealthCheckResult
{
    public HealthStatus Status { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object>? Data { get; set; }
    public TimeSpan Duration { get; set; }
} 