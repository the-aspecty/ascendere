using System.Collections.Generic;

namespace Examples.InventoryModule;

public class Inventory
{
    private readonly List<string> _items = new();

    public IReadOnlyList<string> Items => _items;

    public void Add(string item)
    {
        _items.Add(item);
    }

    public bool Remove(string item)
    {
        return _items.Remove(item);
    }

    public void Clear()
    {
        _items.Clear();
    }
}
