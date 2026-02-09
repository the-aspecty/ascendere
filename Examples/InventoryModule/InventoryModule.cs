using Godot;

namespace Examples.InventoryModule;

[Module("ExampleInventory", autoLoad: true, loadOrder: 15)]
public partial class InventoryModule : Node, IModule
{
    public bool IsInitialized { get; private set; }

    string IModule.Name => "ExampleInventory";

    public void Initialize()
    {
        GD.Print("[InventoryModule] Initialize - registering IInventoryService");

        try
        {
            ServiceLocator.Register<IInventoryService, InventoryService>(ServiceLifetime.Singleton);
            IsInitialized = true;
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"[InventoryModule] Failed to register service: {e.Message}");
        }
    }

    public void Cleanup()
    {
        GD.Print("[InventoryModule] Cleanup - unregistering IInventoryService");
        ServiceLocator.Unregister<IInventoryService>();
        IsInitialized = false;
    }
}
