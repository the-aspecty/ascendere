using Godot;

namespace Examples.ServiceModule;

[Module("ExampleInput", autoLoad: true, loadOrder: 10)]
public partial class InputModule : Node, IModule
{
    public bool IsInitialized { get; private set; }

    public string Name => "ExampleInput";

    public void Initialize()
    {
        GD.Print("[InputModule] Initialize - registering IInputService");

        // Two options: rely on [Service] attribute (auto-registration), or register manually.
        // Here we demonstrate manual registration so the example works even if auto-scan is disabled.
        try
        {
            ServiceLocator.Register<IInputService, InputService>(ServiceLifetime.Singleton);
            IsInitialized = true;
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"[InputModule] Failed to register service: {e.Message}");
        }
    }

    public void Cleanup()
    {
        GD.Print("[InputModule] Cleanup - unregistering IInputService");
        ServiceLocator.Unregister<IInputService>();
        IsInitialized = false;
    }
}
