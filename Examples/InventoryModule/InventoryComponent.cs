using System.Collections.Generic;
using Godot;

namespace Examples.InventoryModule;

public partial class InventoryComponent : Node
{
    [Inject]
    private IInventoryService _inventoryService;

    private Inventory _inventory;

    public override void _Ready()
    {
        ServiceLocator.InjectMembers(this);

        if (_inventoryService == null)
        {
            GD.PrintErr(
                "[InventoryComponent] IInventoryService not available; ensure InventoryModule is loaded"
            );
            return;
        }

        _inventoryService.RegisterEntity(this);
        _inventory = _inventoryService.GetInventory(this);
    }

    public void AddItem(string item)
    {
        _inventoryService?.AddItem(this, item);
    }

    public bool RemoveItem(string item)
    {
        return _inventoryService?.RemoveItem(this, item) ?? false;
    }

    public IReadOnlyList<string> GetItems()
    {
        return _inventory?.Items ?? new List<string>();
    }

    public override void _ExitTree()
    {
        _inventoryService?.UnregisterEntity(this);
    }
}
