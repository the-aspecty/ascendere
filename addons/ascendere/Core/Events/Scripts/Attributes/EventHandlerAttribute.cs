using System;

namespace Ascendere.Events;

// Attribute to mark event handler methods
[AttributeUsage(AttributeTargets.Method)]
public class EventHandlerAttribute : Attribute
{
    public Type EventType { get; }
    public int Priority { get; set; } = 0; // Higher priority = executes first

    public EventHandlerAttribute(Type eventType)
    {
        EventType = eventType;
    }
}
