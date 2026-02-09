using Godot;

namespace Examples.ServiceModule;

// Automatic registration via Service attribute (ServiceLocator will pick this up)
[Service(typeof(IInputService))]
public partial class InputService : Node, IInputService
{
    public bool IsActionPressed(string action)
    {
        return Input.IsActionPressed(action);
    }

    public void RegisterAction(string name, Key key)
    {
        if (!InputMap.HasAction(name))
            InputMap.AddAction(name);

        // We keep this simple for the example — adding an empty action is fine for testing
        // You can extend to add InputEventKey events as needed
    }
}
