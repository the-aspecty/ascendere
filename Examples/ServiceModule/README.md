# Service Module Example

This example demonstrates the recommended pattern: *core defines interfaces* and *modules provide implementations* (Dependency Inversion).

What is included:
- `IInputService.cs` — the service interface (acts like a core contract)
- `InputService.cs` — a concrete implementation (the module provides this)
- `InputModule.cs` — lightweight module that registers/unregisters the service
- `PlayerNode.cs` — a small Node showing how to consume the service via `[Inject]`

Quick test steps

1. Ensure `ModuleManager` and `ServiceLocator` are active in your project (autoload or added to the running scene).
2. Build the project so the example classes are compiled into the game assembly.
3. Run the game (ModuleManager will scan and instantiate modules marked with `[Module(..., AutoLoad=true)]`).
4. The `InputModule.Initialize()` method registers the `IInputService` implementation with the ServiceLocator.
5. Put `PlayerNode` in a test scene and run — it will have `_input` injected after `ServiceLocator.InjectMembers(this)`.

Notes

- This example shows both attribute-driven (`[Service]`) and manual registration patterns so you can choose which workflow fits your project.
- The pattern keeps the public contract (interface) in a stable place and lets modules supply implementations.

Enjoy — ask if you want a runnable scene file included as well!