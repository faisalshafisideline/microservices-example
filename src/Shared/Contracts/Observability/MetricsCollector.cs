using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Shared.Contracts.Observability;

/// <summary>
/// Simplified metrics collector implementation
/// </summary>
public class MetricsCollector : IMetricsCollector, IBusinessMetricsCollector, IPerformanceMonitor, IDisposable
{
    private readonly ILogger<MetricsCollector> _logger;
    private readonly Meter _meter;

    // Counters
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _userActionCounter;
    private readonly Counter<long> _circuitBreakerCounter;

    // Histograms for duration tracking
    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<double> _databaseQueryDuration;
    private readonly Histogram<double> _cacheOperationDuration;
    private readonly Histogram<double> _messageProcessingDuration;
    private readonly Histogram<double> _grpcCallDuration;

    // Gauges (using UpDownCounter as approximation)
    private readonly UpDownCounter<long> _activeConnections;
    private readonly UpDownCounter<long> _queueLength;

    public MetricsCollector(ILogger<MetricsCollector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _meter = new Meter("MicroservicesExample", "1.0.0");

        // Initialize counters
        _requestCounter = _meter.CreateCounter<long>("requests_total", "requests", "Total number of requests");
        _userActionCounter = _meter.CreateCounter<long>("user_actions_total", "actions", "Total number of user actions");
        _circuitBreakerCounter = _meter.CreateCounter<long>("circuit_breaker_events_total", "events", "Circuit breaker events");

        // Initialize histograms
        _requestDuration = _meter.CreateHistogram<double>("request_duration_ms", "ms", "Request duration in milliseconds");
        _databaseQueryDuration = _meter.CreateHistogram<double>("database_query_duration_ms", "ms", "Database query duration");
        _cacheOperationDuration = _meter.CreateHistogram<double>("cache_operation_duration_ms", "ms", "Cache operation duration");
        _messageProcessingDuration = _meter.CreateHistogram<double>("message_processing_duration_ms", "ms", "Message processing duration");
        _grpcCallDuration = _meter.CreateHistogram<double>("grpc_call_duration_ms", "ms", "gRPC call duration");

        // Initialize gauges
        _activeConnections = _meter.CreateUpDownCounter<long>("active_connections", "connections", "Number of active connections");
        _queueLength = _meter.CreateUpDownCounter<long>("queue_length", "items", "Queue length");

        _logger.LogInformation("MetricsCollector initialized with meter: {MeterName}", _meter.Name);
    }

    // IMetricsCollector Implementation
    public void IncrementCounter(string name, double value = 1, params KeyValuePair<string, object?>[] tags)
    {
        try
        {
            _requestCounter.Add((long)value);
            _logger.LogDebug("Incremented counter: {Name} by {Value}", name, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing counter: {Name}", name);
        }
    }

    public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        try
        {
            _activeConnections.Add((long)value);
            _logger.LogDebug("Recorded gauge: {Name} = {Value}", name, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording gauge: {Name}", name);
        }
    }

    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        try
        {
            _requestDuration.Record(value);
            _logger.LogDebug("Recorded histogram: {Name} = {Value}", name, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording histogram: {Name}", name);
        }
    }

    public void RecordRequestDuration(string operationType, string endpoint, double durationMs, string? status = null)
    {
        try
        {
            _requestDuration.Record(durationMs);
            _requestCounter.Add(1);
            _logger.LogDebug("Recorded request duration: {Duration}ms for {OperationType}:{Endpoint}",
                durationMs, operationType, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording request duration");
        }
    }

    public void IncrementCounter(string name, string[]? tags = null)
    {
        try
        {
            _requestCounter.Add(1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing counter: {Name}", name);
        }
    }

    public void RecordGauge(string name, double value, string[]? tags = null)
    {
        try
        {
            _activeConnections.Add((long)value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording gauge: {Name}", name);
        }
    }

    public void RecordHistogram(string name, double value, string[]? tags = null)
    {
        try
        {
            _requestDuration.Record(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording histogram: {Name}", name);
        }
    }

    public void RecordUserContextMetrics(string userId, string operation, bool success)
    {
        try
        {
            _userActionCounter.Add(1);
            _logger.LogDebug("Recorded user context metrics: {UserId}, {Operation}, Success: {Success}",
                userId, operation, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording user context metrics");
        }
    }

    public void RecordCircuitBreakerMetrics(string serviceName, string state, int failureCount)
    {
        try
        {
            _circuitBreakerCounter.Add(1);
            _logger.LogDebug("Recorded circuit breaker metrics: {ServiceName}, State: {State}, Failures: {FailureCount}",
                serviceName, state, failureCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording circuit breaker metrics");
        }
    }

    // IBusinessMetricsCollector Implementation
    public void TrackArticleCreated(string userId, string category)
    {
        try
        {
            _userActionCounter.Add(1);
            _logger.LogDebug("Tracked article created: User={UserId}, Category={Category}", userId, category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking article created");
        }
    }

    public void TrackArticleViewed(string articleId, string? userId = null)
    {
        try
        {
            _userActionCounter.Add(1);
            _logger.LogDebug("Tracked article viewed: Article={ArticleId}, User={UserId}", articleId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking article viewed");
        }
    }

    public void TrackUserLogin(string userId, bool success, string? provider = null)
    {
        try
        {
            _userActionCounter.Add(1);
            _logger.LogDebug("Tracked user login: User={UserId}, Success={Success}, Provider={Provider}", 
                userId, success, provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking user login");
        }
    }

    public void TrackReportGenerated(string reportType, string userId, TimeSpan duration)
    {
        try
        {
            _userActionCounter.Add(1);
            _requestDuration.Record(duration.TotalMilliseconds);
            _logger.LogDebug("Tracked report generated: Type={ReportType}, User={UserId}, Duration={Duration}ms", 
                reportType, userId, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking report generated");
        }
    }

    public void TrackApiGatewayRequest(string endpoint, string method, int statusCode, TimeSpan duration)
    {
        try
        {
            _requestCounter.Add(1);
            _requestDuration.Record(duration.TotalMilliseconds);
            _logger.LogDebug("Tracked API gateway request: {Method} {Endpoint} -> {StatusCode} ({Duration}ms)", 
                method, endpoint, statusCode, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking API gateway request");
        }
    }

    // IPerformanceMonitor Implementation
    public IDisposable StartOperation(string operationName)
    {
        return new OperationTimer(operationName, this);
    }

    public void RecordDatabaseQuery(string queryType, TimeSpan duration, bool success)
    {
        try
        {
            _databaseQueryDuration.Record(duration.TotalMilliseconds);
            _logger.LogDebug("Recorded database query: Type={QueryType}, Duration={Duration}ms, Success={Success}", 
                queryType, duration.TotalMilliseconds, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording database query");
        }
    }

    public void RecordCacheOperation(string operation, bool hit, TimeSpan duration)
    {
        try
        {
            _cacheOperationDuration.Record(duration.TotalMilliseconds);
            _logger.LogDebug("Recorded cache operation: {Operation}, Hit={Hit}, Duration={Duration}ms", 
                operation, hit, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording cache operation");
        }
    }

    public void RecordMessageProcessing(string messageType, TimeSpan duration, bool success)
    {
        try
        {
            _messageProcessingDuration.Record(duration.TotalMilliseconds);
            _logger.LogDebug("Recorded message processing: Type={MessageType}, Duration={Duration}ms, Success={Success}", 
                messageType, duration.TotalMilliseconds, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording message processing");
        }
    }

    public void RecordGrpcCall(string method, TimeSpan duration, string status)
    {
        try
        {
            _grpcCallDuration.Record(duration.TotalMilliseconds);
            _logger.LogDebug("Recorded gRPC call: Method={Method}, Duration={Duration}ms, Status={Status}", 
                method, duration.TotalMilliseconds, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording gRPC call");
        }
    }

    public void Dispose()
    {
        _meter?.Dispose();
        _logger.LogInformation("MetricsCollector disposed");
    }

    private class OperationTimer : IDisposable
    {
        private readonly string _operationName;
        private readonly MetricsCollector _metricsCollector;
        private readonly Stopwatch _stopwatch;

        public OperationTimer(string operationName, MetricsCollector metricsCollector)
        {
            _operationName = operationName;
            _metricsCollector = metricsCollector;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _metricsCollector.RecordRequestDuration("operation", _operationName, _stopwatch.Elapsed.TotalMilliseconds);
        }
    }
} 