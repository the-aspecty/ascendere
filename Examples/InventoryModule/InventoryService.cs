using System.Collections.Generic;
using Godot;

namespace Examples.InventoryModule;

// Optionally mark with [Service] if you want attribute-driven auto-registration
[Service(typeof(IInventoryService))]
public partial class InventoryService : Node, IInventoryService
{
    // Map by owner instance id -> Inventory
    private readonly Dictionary<ulong, Inventory> _inventories = new();

    public Inventory GetInventory(Node owner)
    {
        if (owner == null)
            return null;

        var id = owner.GetInstanceId();
        if (!_inventories.TryGetValue(id, out var inv))
        {
            inv = new Inventory();
            _inventories[id] = inv;

            // Optional: hook cleanup when the owner leaves the tree
            owner.TreeExited += () => UnregisterEntity(owner);
        }

        return inv;
    }

    public IReadOnlyList<string> GetItems(Node owner)
    {
        var inv = GetInventory(owner);
        return inv?.Items ?? new List<string>();
    }

    public bool AddItem(Node owner, string item)
    {
        var inv = GetInventory(owner);
        if (inv == null)
            return false;
        inv.Add(item);
        return true;
    }

    public bool RemoveItem(Node owner, string item)
    {
        var inv = GetInventory(owner);
        if (inv == null)
            return false;
        return inv.Remove(item);
    }

    public void RegisterEntity(Node owner)
    {
        if (owner == null)
            return;
        var id = owner.GetInstanceId();
        if (!_inventories.ContainsKey(id))
            _inventories[id] = new Inventory();
    }

    public void UnregisterEntity(Node owner)
    {
        if (owner == null)
            return;
        var id = owner.GetInstanceId();
        if (_inventories.ContainsKey(id))
        {
            _inventories[id].Clear();
            _inventories.Remove(id);
        }
    }
}
