using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Shared.Contracts.EventSourcing;

/// <summary>
/// In-memory event store implementation (for production, use EventStore DB or similar)
/// </summary>
public class InMemoryEventStore : IEventStore
{
    private readonly ILogger<InMemoryEventStore> _logger;
    private readonly Dictionary<string, List<EventData>> _events;
    private readonly object _lock = new();

    public InMemoryEventStore(ILogger<InMemoryEventStore> logger)
    {
        _logger = logger;
        _events = new Dictionary<string, List<EventData>>();
    }

    public Task SaveEventsAsync(Guid aggregateId, string aggregateType, IEnumerable<IDomainEvent> events, int expectedVersion, CancellationToken cancellationToken = default)
    {
        var streamId = $"{aggregateType}:{aggregateId}";
        
        lock (_lock)
        {
            if (!_events.TryGetValue(streamId, out var eventList))
            {
                eventList = new List<EventData>();
                _events[streamId] = eventList;
            }

            if (expectedVersion != -1 && eventList.Count != expectedVersion)
            {
                throw new ConcurrencyException($"Expected version {expectedVersion} but stream has {eventList.Count} events");
            }

            var eventDataList = events.Select((e, index) => new EventData
            {
                EventId = Guid.NewGuid(),
                StreamId = streamId,
                EventType = e.GetType().Name,
                Data = JsonSerializer.Serialize(e, e.GetType()),
                Version = eventList.Count + index + 1,
                Timestamp = DateTimeOffset.UtcNow
            }).ToList();

            eventList.AddRange(eventDataList);

            _logger.LogDebug("Saved {EventCount} events to stream {StreamId} at version {Version}",
                eventDataList.Count, streamId, eventList.Count);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<IDomainEvent>> GetEventsAsync(Guid aggregateId, string aggregateType, int fromVersion = 0, CancellationToken cancellationToken = default)
    {
        var streamId = $"{aggregateType}:{aggregateId}";
        
        lock (_lock)
        {
                    if (!_events.TryGetValue(streamId, out var eventList))
        {
            return Task.FromResult(Enumerable.Empty<IDomainEvent>());
        }

            var eventsFromVersion = eventList
                .Where(e => e.Version > fromVersion)
                .OrderBy(e => e.Version)
                .ToList();

            _logger.LogDebug("Retrieved {EventCount} events from stream {StreamId} from version {FromVersion}",
                eventsFromVersion.Count, streamId, fromVersion);

            return Task.FromResult(eventsFromVersion.Select(DeserializeEvent).Where(e => e != null).Cast<IDomainEvent>());
        }
    }

    public Task<IEnumerable<IDomainEvent>> GetEventsByTypeAsync(string eventType, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var allEvents = _events.Values
                .SelectMany(eventList => eventList)
                .Where(e => e.EventType == eventType);

            if (from.HasValue)
                allEvents = allEvents.Where(e => e.Timestamp >= from.Value);

            if (to.HasValue)
                allEvents = allEvents.Where(e => e.Timestamp <= to.Value);

            var events = allEvents
                .OrderBy(e => e.Timestamp)
                .Select(DeserializeEvent)
                .Where(e => e != null)
                .Cast<IDomainEvent>()
                .ToList();

            _logger.LogDebug("Retrieved {EventCount} events of type {EventType}", events.Count, eventType);

            return Task.FromResult<IEnumerable<IDomainEvent>>(events);
        }
    }

    public Task<bool> AggregateExistsAsync(Guid aggregateId, string aggregateType, CancellationToken cancellationToken = default)
    {
        var streamId = $"{aggregateType}:{aggregateId}";
        
        lock (_lock)
        {
                    var exists = _events.ContainsKey(streamId);
        _logger.LogDebug("Aggregate {AggregateType}:{AggregateId} exists: {Exists}", aggregateType, aggregateId, exists);
        return Task.FromResult(exists);
        }
    }

    private IDomainEvent? DeserializeEvent(EventData eventData)
    {
        try
        {
            // In a real implementation, you'd have a proper event type registry
            var eventType = Type.GetType($"Shared.Contracts.Messages.{eventData.EventType}");
            if (eventType == null)
            {
                _logger.LogWarning("Unknown event type: {EventType}", eventData.EventType);
                return null;
            }

            return JsonSerializer.Deserialize(eventData.Data, eventType) as IDomainEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize event {EventType} from stream {StreamId}",
                eventData.EventType, eventData.StreamId);
            return null;
        }
    }
}

/// <summary>
/// Snapshot store implementation
/// </summary>
public class InMemorySnapshotStore : ISnapshotStore
{
    private readonly ILogger<InMemorySnapshotStore> _logger;
    private readonly Dictionary<string, SnapshotData> _snapshots;
    private readonly object _lock = new();

    public InMemorySnapshotStore(ILogger<InMemorySnapshotStore> logger)
    {
        _logger = logger;
        _snapshots = new Dictionary<string, SnapshotData>();
    }

    public Task SaveSnapshotAsync<T>(Guid aggregateId, string aggregateType, T snapshot, int version, CancellationToken cancellationToken = default) where T : class
    {
        var streamId = $"{aggregateType}:{aggregateId}";
        
        lock (_lock)
        {
            var snapshotData = new SnapshotData
            {
                StreamId = streamId,
                Data = JsonSerializer.Serialize(snapshot),
                Version = version,
                Timestamp = DateTimeOffset.UtcNow,
                SnapshotType = typeof(T).Name
            };

            _snapshots[streamId] = snapshotData;

                    _logger.LogDebug("Saved snapshot for stream {StreamId} at version {Version}", streamId, version);
    }

    return Task.CompletedTask;
}

    public Task<(T? Snapshot, int Version)> GetSnapshotAsync<T>(Guid aggregateId, string aggregateType, CancellationToken cancellationToken = default) where T : class
    {
        var streamId = $"{aggregateType}:{aggregateId}";
        
        lock (_lock)
        {
            if (!_snapshots.TryGetValue(streamId, out var snapshotData))
            {
                _logger.LogDebug("No snapshot found for stream {StreamId}", streamId);
                return Task.FromResult<(T?, int)>((null, 0));
            }

            try
            {
                var snapshot = JsonSerializer.Deserialize<T>(snapshotData.Data);
                _logger.LogDebug("Retrieved snapshot for stream {StreamId} at version {Version}",
                    streamId, snapshotData.Version);
                return Task.FromResult<(T?, int)>((snapshot, snapshotData.Version));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize snapshot for stream {StreamId}", streamId);
                return Task.FromResult<(T?, int)>((null, 0));
            }
        }
    }
}

/// <summary>
/// Event data storage model
/// </summary>
public class EventData
{
    public Guid EventId { get; set; }
    public string StreamId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Snapshot data storage model
/// </summary>
public class SnapshotData
{
    public string StreamId { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string SnapshotType { get; set; } = string.Empty;
}

/// <summary>
/// Concurrency exception for event store operations
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
    public ConcurrencyException(string message, Exception innerException) : base(message, innerException) { }
} 