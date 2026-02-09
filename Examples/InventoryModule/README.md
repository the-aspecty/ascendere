# Inventory Module Example

This example demonstrates an Inventory Module that provides entity-scoped inventories via an `IInventoryService`.

Contents

- `IInventoryService.cs` — Service contract (interface)
- `Inventory.cs` — Simple Inventory model (list of items)
- `InventoryService.cs` — Service implementation that manages per-entity inventories
- `InventoryLoggingDecorator.cs` — Example decorator that logs when items are added (shows decorator mode)
- `InventoryModule.cs` — Module that registers/unregisters the service
- `InventoryComponent.cs` — Component to attach to any entity (node) to access its inventory
- `EntityWithInventory.cs` — Example node that uses the component

Quick test

1. Ensure `ModuleManager` and `ServiceLocator` are available in the running scene (autoload or added manually).
2. Run the game; `InventoryModule` will register the service.
3. Add an `EntityWithInventory` node to a test scene; it will use `InventoryComponent` which obtains the inventory for that entity.
4. Use the `AddItem` methods (via code) to see per-entity inventories maintained separately.

Notes & Design

- Inventories are mapped by a node instance id (so each node gets a separate Inventory instance).
- The component calls `ServiceLocator.InjectMembers(this)` to populate the `_inventoryService` reference.
- The example uses manual registration in the module's `Initialize()` so it works even if attribute scanning is disabled.

