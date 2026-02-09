using Godot;

namespace Examples.ServiceModule;

/// <summary>
/// Example input service contract (interface). In a real project this would live in core.
/// </summary>
public interface IInputService
{
    bool IsActionPressed(string action);
    void RegisterAction(string name, Key key);
}
