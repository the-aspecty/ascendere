namespace Ascendere.Events;

// Interface for cancellable events
public interface ICancellableEvent : IEvent
{
    bool IsCancelled { get; set; }
}
