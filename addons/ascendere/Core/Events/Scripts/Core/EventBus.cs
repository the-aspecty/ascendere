using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace Ascendere.Events;

/// <summary>
/// Centralized event bus for framework-wide event communication.
/// Provides loose coupling between systems and entities.
/// </summary>
//[Module("EventBus")]
public partial class EventBus : Node
{
    private static EventBus _instance;

    //private MetaContext _context;

    /// <summary>
    /// Gets the singleton instance of the EventBus.
    /// </summary>
    public static EventBus Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new EventBus();
            }
            return _instance;
        }
    }
    private readonly Dictionary<Type, List<EventSubscription>> _subscriptions = new();
    private readonly Dictionary<Type, StringName> _signalNames = new();
    private readonly Dictionary<object, List<SignalConnection>> _signalConnections = new();
    private readonly List<EventHistoryEntry> _eventHistory = new();
    private readonly Dictionary<Type, List<object>> _eventQueue = new();

    [Export]
    public bool EnableHistory { get; set; } = true;

    [Export]
    public int MaxHistorySize { get; set; } = 100;

    [Export]
    public bool LogEvents { get; set; } = true;

    [Export]
    public bool EnableProfiling { get; set; } = true;

    private bool _isProcessingQueue = false;
    private readonly Dictionary<Type, EventStats> _eventStats = new();

    [Signal]
    public delegate void EventFiredEventHandler(
        string eventType,
        Godot.Collections.Dictionary eventData
    );

    public override void _Ready()
    {
        _instance = this;
    }

    public override void _Process(double delta)
    {
        ProcessQueuedEvents();
    }

    // Subscribe with priority support
    public void Subscribe(Node subscriber)
    {
        var methods = subscriber
            .GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var method in methods)
        {
            var eventAttr = method.GetCustomAttribute<EventHandlerAttribute>();
            if (eventAttr != null)
            {
                var eventType = eventAttr.EventType;
                if (!_subscriptions.ContainsKey(eventType))
                {
                    _subscriptions[eventType] = new List<EventSubscription>();
                    RegisterSignalForEvent(eventType);
                }

                _subscriptions[eventType]
                    .Add(
                        new EventSubscription
                        {
                            Target = subscriber,
                            Method = method,
                            Priority = eventAttr.Priority,
                        }
                    );

                // Sort by priority (higher first)
                _subscriptions[eventType].Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }

            var signalAttrs = method.GetCustomAttributes<SignalHandlerAttribute>();
            foreach (var signalAttr in signalAttrs)
            {
                ConnectSignalHandler(subscriber, method, signalAttr);
            }
        }
    }

    public void Unsubscribe(Node subscriber)
    {
        foreach (var kvp in _subscriptions)
        {
            kvp.Value.RemoveAll(sub => sub.Target == subscriber);
        }

        if (_signalConnections.TryGetValue(subscriber, out var connections))
        {
            foreach (var conn in connections)
            {
                if (GodotObject.IsInstanceValid(conn.Source))
                {
                    conn.Source.Disconnect(conn.SignalName, conn.Callable);
                }
            }
            _signalConnections.Remove(subscriber);
        }
    }

    // Publish event immediately
    public void Publish<T>(T evt)
        where T : struct, IEvent
    {
        PublishInternal(evt, false);
    }

    // Queue event for next frame (thread-safe pattern)
    public void QueueEvent<T>(T evt)
        where T : struct, IEvent
    {
        var eventType = typeof(T);
        if (!_eventQueue.ContainsKey(eventType))
        {
            _eventQueue[eventType] = new List<object>();
        }
        _eventQueue[eventType].Add(evt);
    }

    // Publish with return value (for cancellable events)
    public T PublishCancellable<T>(T evt)
        where T : struct, ICancellableEvent
    {
        return (T)PublishInternal(evt, false);
    }

    private object PublishInternal<T>(T evt, bool fromQueue)
        where T : struct, IEvent
    {
        var eventType = typeof(T);
        var startTime = EnableProfiling ? Time.GetTicksUsec() : 0UL;

        if (LogEvents)
        {
            GD.Print($"[EventBus] Publishing {eventType.Name}");
        }

        var currentEvent = (object)evt;
        var subscriberCount = 0;

        // Call C# subscribers with priority order
        if (_subscriptions.TryGetValue(eventType, out var subs))
        {
            subscriberCount = subs.Count;
            foreach (var sub in subs)
            {
                try
                {
                    // For cancellable events, check if cancelled before continuing
                    if (currentEvent is ICancellableEvent cancelEvt && cancelEvt.IsCancelled)
                    {
                        if (LogEvents)
                        {
                            GD.Print($"[EventBus] Event {eventType.Name} was cancelled");
                        }
                        break;
                    }

                    var result = sub.Method.Invoke(sub.Target, new[] { currentEvent });

                    // Update event if handler returns modified version
                    if (result != null && result.GetType() == eventType)
                    {
                        currentEvent = result;
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[EventBus] Error invoking handler: {ex.Message}");
                }
            }
        }

        // Emit Godot signal
        if (currentEvent is IEvent iEvt)
        {
            var dict = iEvt.ToGodotDict();
            EmitSignal(SignalName.EventFired, eventType.Name, dict);
        }

        // Record history
        if (EnableHistory && currentEvent is IEvent histEvt)
        {
            RecordEvent(eventType, histEvt, subscriberCount);
        }

        // Record stats
        if (EnableProfiling)
        {
            // Time.GetTicksUsec() returns ulong; cast the difference to long to match RecordStats signature.
            RecordStats(eventType, (long)(Time.GetTicksUsec() - startTime), subscriberCount);
        }

        return currentEvent;
    }

    // Non-generic dispatch helper for queued events (boxed structs implementing IEvent)
    private object PublishInternal(IEvent evt, bool fromQueue)
    {
        if (evt == null)
            return null;

        try
        {
            var genericMethod = GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name == nameof(PublishInternal)
                    && m.IsGenericMethodDefinition
                    && m.GetGenericArguments().Length == 1
                );

            if (genericMethod == null)
            {
                GD.PrintErr(
                    "[EventBus] Could not find generic PublishInternal method via reflection."
                );
                return evt;
            }

            var concrete = genericMethod.MakeGenericMethod(evt.GetType());
            return concrete.Invoke(this, new object[] { evt, fromQueue });
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[EventBus] Error dispatching queued event: {ex.Message}");
            return evt;
        }
    }

    private void ProcessQueuedEvents()
    {
        if (_isProcessingQueue || _eventQueue.Count == 0)
            return;

        _isProcessingQueue = true;

        var eventsCopy = new Dictionary<Type, List<object>>(_eventQueue);
        _eventQueue.Clear();

        foreach (var kvp in eventsCopy)
        {
            foreach (var evt in kvp.Value)
            {
                if (evt is IEvent iEvt)
                {
                    PublishInternal(iEvt, true);
                }
            }
        }

        _isProcessingQueue = false;
    }

    // Subscribe to specific event type at runtime
    public void SubscribeToEvent<T>(Action<T> handler, int priority = 0)
        where T : struct, IEvent
    {
        var eventType = typeof(T);
        if (!_subscriptions.ContainsKey(eventType))
        {
            _subscriptions[eventType] = new List<EventSubscription>();
            RegisterSignalForEvent(eventType);
        }

        // Create a wrapper method info for the action
        var sub = new EventSubscription
        {
            Target = handler.Target,
            Method = handler.Method,
            Priority = priority,
            IsAction = true,
            Action = handler,
        };

        _subscriptions[eventType].Add(sub);
        _subscriptions[eventType].Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    // Unsubscribe specific handler
    public void UnsubscribeFromEvent<T>(Action<T> handler)
        where T : struct, IEvent
    {
        var eventType = typeof(T);
        if (_subscriptions.TryGetValue(eventType, out var subs))
        {
            subs.RemoveAll(s => s.Action?.Equals(handler) == true);
        }
    }

    // Get event history
    public List<EventHistoryEntry> GetEventHistory(string eventTypeFilter = null)
    {
        if (string.IsNullOrEmpty(eventTypeFilter))
        {
            return new List<EventHistoryEntry>(_eventHistory);
        }
        return _eventHistory.Where(e => e.EventType == eventTypeFilter).ToList();
    }

    // Get event statistics
    public Dictionary<string, EventStats> GetEventStats()
    {
        return _eventStats.ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value);
    }

    // Clear event history
    public void ClearHistory()
    {
        _eventHistory.Clear();
    }

    // Clear statistics
    public void ClearStats()
    {
        _eventStats.Clear();
    }

    // Check if anyone is listening to an event type
    public bool HasSubscribers<T>()
        where T : struct, IEvent
    {
        var eventType = typeof(T);
        return _subscriptions.TryGetValue(eventType, out var subs) && subs.Count > 0;
    }

    // Get subscriber count for event type
    public int GetSubscriberCount<T>()
        where T : struct, IEvent
    {
        var eventType = typeof(T);
        return _subscriptions.TryGetValue(eventType, out var subs) ? subs.Count : 0;
    }

    private void RecordEvent(Type eventType, IEvent evt, int subscriberCount)
    {
        _eventHistory.Add(
            new EventHistoryEntry
            {
                EventType = eventType.Name,
                Timestamp = Time.GetTicksMsec() / 1000.0,
                Data = evt.ToGodotDict(),
                SubscriberCount = subscriberCount,
            }
        );

        if (_eventHistory.Count > MaxHistorySize)
        {
            _eventHistory.RemoveAt(0);
        }
    }

    private void RecordStats(Type eventType, long executionTimeMicros, int subscriberCount)
    {
        if (!_eventStats.ContainsKey(eventType))
        {
            _eventStats[eventType] = new EventStats { EventTypeName = eventType.Name };
        }

        var stats = _eventStats[eventType];
        stats.TotalCalls++;
        stats.TotalExecutionTimeMicros += executionTimeMicros;
        stats.LastSubscriberCount = subscriberCount;
        stats.AverageExecutionTimeMicros = stats.TotalExecutionTimeMicros / stats.TotalCalls;
    }

    private void ConnectSignalHandler(
        Node subscriber,
        MethodInfo method,
        SignalHandlerAttribute attr
    )
    {
        try
        {
            Node sourceNode = string.IsNullOrEmpty(attr.NodePath)
                ? subscriber
                : subscriber.GetNode(attr.NodePath);

            if (sourceNode == null)
            {
                GD.PrintErr($"Cannot find node at path: {attr.NodePath}");
                return;
            }

            var callable = new Callable(subscriber, method.Name);
            sourceNode.Connect(attr.SignalName, callable);

            if (!_signalConnections.ContainsKey(subscriber))
            {
                _signalConnections[subscriber] = new List<SignalConnection>();
            }

            _signalConnections[subscriber]
                .Add(
                    new SignalConnection
                    {
                        Source = sourceNode,
                        SignalName = attr.SignalName,
                        Callable = callable,
                    }
                );
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error connecting signal: {ex.Message}");
        }
    }

    private void RegisterSignalForEvent(Type eventType)
    {
        var signalName = new StringName($"Event_{eventType.Name}");
        _signalNames[eventType] = signalName;
    }

    private class EventSubscription
    {
        public object Target;
        public MethodInfo Method;
        public int Priority;
        public bool IsAction;
        public object Action;
    }

    private class SignalConnection
    {
        public GodotObject Source;
        public StringName SignalName;
        public Callable Callable;
    }
}
