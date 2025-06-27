using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Shared.Contracts.Observability;

/// <summary>
/// Distributed tracing service implementation using .NET Activity API
/// </summary>
public class DistributedTracingService : IDistributedTracingService, ITelemetryService
{
    private readonly ILogger<DistributedTracingService> _logger;
    private readonly ActivitySource _activitySource;
    private readonly string _serviceName;

    public DistributedTracingService(ILogger<DistributedTracingService> logger, string serviceName = "Microservices")
    {
        _logger = logger;
        _serviceName = serviceName;
        _activitySource = new ActivitySource(serviceName, "1.0.0");
    }

    public Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal)
    {
        var activity = _activitySource.StartActivity(operationName, kind);
        
        if (activity != null)
        {
            activity.SetTag("service.name", _serviceName);
            activity.SetTag("service.version", "1.0.0");
            
            _logger.LogDebug("Started activity: {OperationName} with ID: {ActivityId}", 
                operationName, activity.Id);
        }
        else
        {
            _logger.LogDebug("No listener for activity: {OperationName}", operationName);
        }

        return activity;
    }

    public void AddTag(string key, string value)
    {
        var currentActivity = Activity.Current;
        if (currentActivity != null)
        {
            currentActivity.SetTag(key, value);
            _logger.LogTrace("Added tag {Key}={Value} to activity {ActivityId}", 
                key, value, currentActivity.Id);
        }
        else
        {
            _logger.LogTrace("No current activity to add tag {Key}={Value}", key, value);
        }
    }

    public void AddEvent(string name, DateTimeOffset? timestamp = null, ActivityTagsCollection? tags = null)
    {
        var currentActivity = Activity.Current;
        if (currentActivity != null)
        {
            var eventTimestamp = timestamp ?? DateTimeOffset.UtcNow;
            var activityEvent = new ActivityEvent(name, eventTimestamp, tags ?? new ActivityTagsCollection());
            currentActivity.AddEvent(activityEvent);
            
            _logger.LogTrace("Added event {EventName} to activity {ActivityId}", 
                name, currentActivity.Id);
        }
        else
        {
            _logger.LogTrace("No current activity to add event {EventName}", name);
        }
    }

    public void SetStatus(ActivityStatusCode status, string? description = null)
    {
        var currentActivity = Activity.Current;
        if (currentActivity != null)
        {
            currentActivity.SetStatus(status, description);
            _logger.LogTrace("Set status {Status} on activity {ActivityId}: {Description}", 
                status, currentActivity.Id, description);
        }
        else
        {
            _logger.LogTrace("No current activity to set status {Status}", status);
        }
    }

    public void RecordException(Exception exception)
    {
        var currentActivity = Activity.Current;
        if (currentActivity != null)
        {
            currentActivity.SetStatus(ActivityStatusCode.Error, exception.Message);
            
            var tags = new ActivityTagsCollection
            {
                ["exception.type"] = exception.GetType().FullName,
                ["exception.message"] = exception.Message,
                ["exception.stacktrace"] = exception.StackTrace
            };

            currentActivity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, tags));
            
            _logger.LogError(exception, "Recorded exception in activity {ActivityId}", currentActivity.Id);
        }
        else
        {
            _logger.LogError(exception, "Exception occurred but no current activity to record it");
        }
    }

    // ITelemetryService implementation
    public void TrackDependency(string dependencyType, string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, bool success)
    {
        using var activity = StartActivity($"dependency.{dependencyType}", ActivityKind.Client);
        if (activity != null)
        {
            activity.SetTag("dependency.type", dependencyType);
            activity.SetTag("dependency.name", dependencyName);
            activity.SetTag("dependency.data", data);
            activity.SetTag("dependency.success", success.ToString().ToLowerInvariant());
            activity.SetStartTime(startTime.UtcDateTime);
            activity.SetEndTime(startTime.Add(duration).UtcDateTime);
            
            if (!success)
            {
                activity.SetStatus(ActivityStatusCode.Error, "Dependency call failed");
            }
        }

        _logger.LogDebug("Tracked dependency: {DependencyType}.{DependencyName}, Duration: {Duration}ms, Success: {Success}",
            dependencyType, dependencyName, duration.TotalMilliseconds, success);
    }

    public void TrackEvent(string eventName, Dictionary<string, string>? properties = null, Dictionary<string, double>? metrics = null)
    {
        var tags = new ActivityTagsCollection();
        
        if (properties != null)
        {
            foreach (var property in properties)
            {
                tags.Add($"event.{property.Key}", property.Value);
            }
        }
        
        if (metrics != null)
        {
            foreach (var metric in metrics)
            {
                tags.Add($"metric.{metric.Key}", metric.Value.ToString());
            }
        }

        AddEvent(eventName, DateTimeOffset.UtcNow, tags);
        
        _logger.LogInformation("Tracked event: {EventName} with {PropertyCount} properties and {MetricCount} metrics",
            eventName, properties?.Count ?? 0, metrics?.Count ?? 0);
    }

    public void TrackException(Exception exception, Dictionary<string, string>? properties = null)
    {
        RecordException(exception);
        
        if (properties != null)
        {
            foreach (var property in properties)
            {
                AddTag($"exception.{property.Key}", property.Value);
            }
        }
    }

    public void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success)
    {
        using var activity = StartActivity($"request.{name}", ActivityKind.Server);
        if (activity != null)
        {
            activity.SetTag("request.name", name);
            activity.SetTag("request.response_code", responseCode);
            activity.SetTag("request.success", success.ToString().ToLowerInvariant());
            activity.SetStartTime(startTime.UtcDateTime);
            activity.SetEndTime(startTime.Add(duration).UtcDateTime);
            
            if (!success)
            {
                activity.SetStatus(ActivityStatusCode.Error, $"Request failed with code {responseCode}");
            }
        }

        _logger.LogDebug("Tracked request: {RequestName}, Duration: {Duration}ms, Response: {ResponseCode}, Success: {Success}",
            name, duration.TotalMilliseconds, responseCode, success);
    }

    public void TrackTrace(string message, SeverityLevel severityLevel, Dictionary<string, string>? properties = null)
    {
        var tags = new ActivityTagsCollection
        {
            ["trace.message"] = message,
            ["trace.severity"] = severityLevel.ToString()
        };
        
        if (properties != null)
        {
            foreach (var property in properties)
            {
                tags.Add($"trace.{property.Key}", property.Value);
            }
        }

        AddEvent("trace", DateTimeOffset.UtcNow, tags);
        
        var logLevel = severityLevel switch
        {
            SeverityLevel.Verbose => LogLevel.Trace,
            SeverityLevel.Information => LogLevel.Information,
            SeverityLevel.Warning => LogLevel.Warning,
            SeverityLevel.Error => LogLevel.Error,
            SeverityLevel.Critical => LogLevel.Critical,
            _ => LogLevel.Information
        };
        
        _logger.Log(logLevel, "Tracked trace: {Message} (Severity: {Severity})", message, severityLevel);
    }

    public void Dispose()
    {
        _activitySource?.Dispose();
    }
} 