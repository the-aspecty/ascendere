using Godot;

[Module("Input", true)]
public partial class InputModule : Node, IModule
{
    public bool IsInitialized { get; private set; } = false;

    string IModule.Name => Name;

    public void Cleanup()
    {
        //noop
    }

    public void Initialize()
    {
        GD.Print("Input Module Initialized");
    }
}
