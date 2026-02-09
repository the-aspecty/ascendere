using System.Collections.Generic;
using Godot;

namespace Examples.InventoryModule;

public interface IInventoryService
{
    Inventory GetInventory(Node owner);
    IReadOnlyList<string> GetItems(Node owner);
    bool AddItem(Node owner, string item);
    bool RemoveItem(Node owner, string item);
    void RegisterEntity(Node owner);
    void UnregisterEntity(Node owner);
}
