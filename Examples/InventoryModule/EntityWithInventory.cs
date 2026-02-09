using Godot;

namespace Examples.InventoryModule;

public partial class EntityWithInventory : Node
{
    private InventoryComponent _inventoryComp;

    public override void _Ready()
    {
        // Add component at runtime for the example
        _inventoryComp = new InventoryComponent();
        AddChild(_inventoryComp);

        // Add sample items
        _inventoryComp.AddItem("Sword");
        _inventoryComp.AddItem("Potion");

        GD.Print($"[EntityWithInventory] Items: {string.Join(", ", _inventoryComp.GetItems())}");
    }

    public void GiveItem(string item)
    {
        _inventoryComp.AddItem(item);
    }
}
