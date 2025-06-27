namespace Shared.Contracts.EventSourcing;

/// <summary>
/// Base interface for domain events
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    string EventType { get; }
    Guid AggregateId { get; }
    string AggregateType { get; }
    int Version { get; }
    DateTimeOffset OccurredAt { get; }
    string? UserId { get; }
    string? CorrelationId { get; }
}

/// <summary>
/// Event store abstraction for event sourcing
/// </summary>
public interface IEventStore
{
    Task SaveEventsAsync(Guid aggregateId, string aggregateType, IEnumerable<IDomainEvent> events, int expectedVersion, CancellationToken cancellationToken = default);
    Task<IEnumerable<IDomainEvent>> GetEventsAsync(Guid aggregateId, string aggregateType, int fromVersion = 0, CancellationToken cancellationToken = default);
    Task<IEnumerable<IDomainEvent>> GetEventsByTypeAsync(string eventType, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default);
    Task<bool> AggregateExistsAsync(Guid aggregateId, string aggregateType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Snapshot store for aggregate state optimization
/// </summary>
public interface ISnapshotStore
{
    Task SaveSnapshotAsync<T>(Guid aggregateId, string aggregateType, T snapshot, int version, CancellationToken cancellationToken = default) where T : class;
    Task<(T? Snapshot, int Version)> GetSnapshotAsync<T>(Guid aggregateId, string aggregateType, CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Base aggregate root for event sourcing
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();
    
    public Guid Id { get; protected set; }
    public int Version { get; protected set; } = -1;
    
    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();
    
    protected void AddEvent(IDomainEvent domainEvent)
    {
        _uncommittedEvents.Add(domainEvent);
        Apply(domainEvent);
        Version++;
    }
    
    public void MarkEventsAsCommitted()
    {
        _uncommittedEvents.Clear();
    }
    
    public void LoadFromHistory(IEnumerable<IDomainEvent> events)
    {
        foreach (var domainEvent in events)
        {
            Apply(domainEvent);
            Version++;
        }
    }
    
    protected abstract void Apply(IDomainEvent domainEvent);
} 