using Godot;

namespace Examples.ServiceModule;

public partial class PlayerNode : Node
{
    [Inject]
    private IInputService _input;

    public override void _Ready()
    {
        // Inject members so _input is populated by ServiceLocator
        ServiceLocator.InjectMembers(this);

        if (_input == null)
            GD.PrintErr(
                "[PlayerNode] IInputService was not injected — ensure InputModule is registered"
            );
        else
            GD.Print("[PlayerNode] IInputService injected, ready to use");
    }
}
