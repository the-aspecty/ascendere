namespace Ascendere.Events;

// Interface for events to implement
public interface IEvent
{
    Godot.Collections.Dictionary ToGodotDict();
    void FromGodotDict(Godot.Collections.Dictionary dict);
}
