using System.Collections.Generic;
using Godot;

namespace Examples.InventoryModule;

[Decorator(typeof(IInventoryService), order: 0)]
public class InventoryLoggingDecorator
{
    // The ServiceLocator will call this method with the current service instance
    // and expect a decorated instance in return.
    public IInventoryService Decorate(IInventoryService inner)
    {
        return new InventoryLoggingWrapper(inner);
    }
}

public class InventoryLoggingWrapper : IInventoryService
{
    private readonly IInventoryService _inner;

    public InventoryLoggingWrapper(IInventoryService inner)
    {
        _inner = inner;
    }

    public Inventory GetInventory(Node owner) => _inner.GetInventory(owner);

    public IReadOnlyList<string> GetItems(Node owner) => _inner.GetItems(owner);

    public bool AddItem(Node owner, string item)
    {
        var result = _inner.AddItem(owner, item);
        if (result)
        {
            GD.Print(
                $"[Inventory] Added item '{item}' to owner {owner?.Name ?? owner?.GetInstanceId().ToString()}"
            );
        }
        return result;
    }

    public bool RemoveItem(Node owner, string item)
    {
        return _inner.RemoveItem(owner, item);
    }

    public void RegisterEntity(Node owner) => _inner.RegisterEntity(owner);

    public void UnregisterEntity(Node owner) => _inner.UnregisterEntity(owner);
}
