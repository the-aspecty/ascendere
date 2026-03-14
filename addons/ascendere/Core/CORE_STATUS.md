# Core Modules — Status Analysis

> Generated: March 13, 2026

---

## Overview

| Module | Version | Status | Issues |
|---|---|---|---|
| ModulesCore | 1.0.0 | ✅ Functional | Minor — commented-out integration |
| Events | 1.0.0 | ✅ Functional | Minor — orphan code, missing namespaces |
| ServiceLocator | 1.0.0 | ✅ Functional | Depends on `editorruntime` |

---

## 1. ModulesCore

**Path:** `addons/ascendere/Core/ModulesCore/`  
**Version:** 1.0.0  
**Category:** Core  
**Dependencies:** None

### Components

| File | Role |
|---|---|
| `IModule.cs` | Base interface: `Name`, `IsInitialized`, `Initialize()`, `Cleanup()` |
| `ModuleManager.cs` | Singleton `Node`, auto-discovers and manages module lifecycle |
| `Attributes/ModuleAttribute.cs` | `[Module(name, autoLoad, loadOrder)]` marker attribute |

### What's Working

- Assembly scanning at startup via `AppDomain.CurrentDomain.GetAssemblies()`
- Attribute-driven auto-discovery with `[Module]`
- Load-order sorting via `ModuleAttribute.LoadOrder`
- `AutoLoad = false` support to opt out of automatic instantiation
- Modules that extend `Node` are added to the scene tree as children of `ModuleManager`
- Full lifecycle: `Initialize()` on entry, `Cleanup()` on `_ExitTree()`
- `GetModule<T>()` for typed retrieval
- `GetAllModules()` for editor/inspector access
- Singleton guard — duplicate `ModuleManager` instances are freed

### Known Issues / Notes

- `RegisterModule()` has a commented-out call to `ServiceLocator.RegisterServiceInferType(module)` — planned integration with the ServiceLocator is not yet active.
- `ModuleManager` does not currently expose a per-module error state; a failed `Initialize()` is only logged, not tracked.
- No dependency ordering between modules — if Module B depends on Module A being initialized first, it relies solely on `LoadOrder` being set correctly by the developer.

---

## 2. Events

**Path:** `addons/ascendere/Core/Events/`  
**Version:** 1.0.0  
**Category:** Core  
**Dependencies:** None

### Components

| File | Role |
|---|---|
| `Core/IEvent.cs` | Interface: `ToGodotDict()` / `FromGodotDict()` |
| `Core/ICancellableEvent.cs` | Extends `IEvent` with `IsCancelled` |
| `Core/Event.cs` | Contains `IEventit` and `PlayerJoinedEvent` — orphan/dead code |
| `Core/EventBus.cs` | Full centralized event bus implementation |
| `Attributes/EventHandlerAttribute.cs` | `[EventHandler(typeof(T), Priority)]` for method-based subscriptions |
| `Attributes/SignalHandlerAttribute.cs` | `[SignalHandler(signalName, nodePath?)]` for Godot signal bridging |
| `Utils/EventHistoryEntry.cs` | History record: type, timestamp, data, subscriber count |
| `Utils/EventStats.cs` | Profiling record: call count, total/avg execution time |
| `ExampleEvents.cs` | Reference event structs + subscriber examples |

### What's Working

- **Attribute subscriptions** — `Subscribe(Node)` scans methods for `[EventHandler]` and `[SignalHandler]` automatically
- **Runtime subscriptions** — `SubscribeToEvent<T>(Action<T>, priority)` and `UnsubscribeFromEvent<T>`
- **Priority ordering** — handlers sorted descending by priority; higher priority runs first
- **Cancellable events** — `ICancellableEvent.IsCancelled` is checked between handlers; cancelled events stop propagation
- **Queued dispatch** — `QueueEvent<T>()` defers events to `_Process()` (next frame)
- **Godot signal bridge** — `EventFired` signal emitted on every published event for GDScript interop
- **History** — configurable ring buffer (`MaxHistorySize`, toggleable via `EnableHistory`)
- **Profiling** — per-event-type stats (total calls, avg/total execution time µs, subscriber count) toggleable via `EnableProfiling`
- **Clean teardown** — `Unsubscribe(Node)` removes all method handlers and disconnects all Godot signals for that node

### Known Issues

| Severity | Location | Issue |
|---|---|---|
| Low | `Core/Event.cs` | Contains orphan `IEventit` interface (typo of `IEvent`?) and a `PlayerJoinedEvent` struct that belong in `ExampleEvents.cs` or should be removed |
| Low | `Attributes/SignalHandlerAttribute.cs` | Missing namespace declaration — attribute is in the global namespace instead of `Ascendere.Events` |
| Low | `Utils/EventHistoryEntry.cs` | Missing namespace declaration |
| Low | `Utils/EventStats.cs` | Missing namespace declaration |
| Info | `EventBus.cs` | The fallback `Instance` getter creates a plain `new EventBus()` without adding it to the scene tree. Without autoload, `_Ready()` / `_Process()` will never fire on this fallback instance. The bus should be an autoload or the fallback getter should warn and return `null`. |

### Export Properties (Configurable in Editor)

| Property | Default | Effect |
|---|---|---|
| `EnableHistory` | `true` | Records event history entries |
| `MaxHistorySize` | `100` | Ring buffer capacity |
| `LogEvents` | `true` | Prints event type on each publish |
| `EnableProfiling` | `true` | Tracks per-type execution stats |

---

## 3. ServiceLocator

**Path:** `addons/ascendere/Core/ServiceLocator/`  
**Version:** 1.0.0  
**Category:** Core  
**Dependencies:** `editorruntime`

### Components

| File | Role |
|---|---|
| `Scripts/ServiceLocator.cs` | Core DI container — registration, resolution, middleware, scopes |
| `Scripts/DependencyGraph.cs` | Cycle detection, topological ordering, Mermaid/DOT export |
| `Attributes/ServiceAttribute.cs` | `[Service(interfaceType, lifetime, priority, lazy, name)]` |
| `Attributes/InjectAttribute.cs` | `[Inject(optional, name)]` for property/field injection |
| `Attributes/DecoratorAttribute.cs` | `[Decorator(serviceType, order)]` for decorator auto-registration |
| `Attributes/InitializeAttribute.cs` | `[Initialize]` marks async initialization method |
| `Attributes/PostInjectAttribute.cs` | `[PostInject]` marks post-injection callback |
| `Classes/ServiceContext.cs` | Carries service type, name, and request time through middleware |
| `Classes/ServiceDependency.cs` | Dependency edge data for graph nodes |
| `Classes/ServiceNode.cs` | Graph node: type, implementation, name, lifetime, deps |
| `Types/Enums.cs` | `ServiceLifetime` — Singleton, Transient, Scoped |
| `Types/IServiceDecorator.cs` | `IServiceDecorator<T>` — `Decorate(T)` contract |
| `Types/IServiceMiddleware.cs` | `IServiceMiddleware` — async pipeline middleware contract |

### What's Working

#### Registration
- `Register<TInterface, TImplementation>(lifetime, lazy, name)` — type-based
- `Register<T>(instance, name)` — instance registration; Nodes are automatically added to the scene tree
- `RegisterFactory<T>(Func<T>, lifetime, name)` — factory-based
- `RegisterDecorator<TService, TDecorator>(order)` — manual decorator registration
- `[Service]` attribute — auto-discovery on startup, priority-ordered, supports multiple attributes per class
- `[Decorator]` attribute — auto-discovery, ordered

#### Resolution
- `Get<T>(name)` / `Get(Type, name)` — typed and untyped retrieval
- Named service resolution
- Lazy singletons — resolved on first `Get()`
- Eager singletons — resolved immediately after all registrations complete
- Full middleware pipeline wraps every resolution

#### Lifetimes
- **Singleton** — one instance for the application lifetime
- **Transient** — new instance per `Get()` call
- **Scoped** — one instance per named scope (`CreateScope()` / `EndScope()`)

#### Dependency Injection
- `[Inject]` on properties and fields — resolved automatically after construction
- `[Inject(optional: true)]` — missing optional deps are skipped (no error)
- Named injection via `[Inject(name: "...")]`
- `[PostInject]` method invoked after all injections are resolved
- `[Initialize]` method invoked for async init

#### Decorators & Middleware
- Decorator chain applied in `Order` sequence via `IServiceDecorator<T>.Decorate()`
- Async middleware pipeline via `IServiceMiddleware.InvokeAsync()` or delegate overload

#### Observability
- `ServiceLocator.OnServiceRegistered` — C# event fired on each registration
- `ServiceLocator.OnServiceResolved` — C# event fired on each resolution
- `DependencyGraph` — buildable graph with:
  - Circular dependency detection (DFS-based)
  - Topological initialization ordering
  - Mermaid diagram export (`graph TD`)
  - DOT/Graphviz export (`digraph`)

### Known Issues / Notes

| Severity | Location | Issue |
|---|---|---|
| Info | `module.json` | Declares a hard dependency on `editorruntime`. If `editorruntime` is not present, `AutoRegisterServices()` will still run but `ReportService()` (deferred call) may fail. |
| Info | `ServiceLocator.cs` | `Get()` is synchronous but internally calls `async ExecuteMiddlewarePipeline`. The result is obtained via `.GetAwaiter().GetResult()` (blocking). This can cause deadlocks in certain async contexts — acceptable for game-thread use but worth noting. |
| Low | `Enums.cs` | Missing namespace — `ServiceLifetime` is in the global namespace |
| Low | `ServiceDependency.cs`, `ServiceNode.cs`, `ServiceContext.cs` | All in the global namespace |

---

## Cross-Module Integration Status

| Integration | Status | Notes |
|---|---|---|
| ModulesCore → ServiceLocator | ⚠️ Planned / Commented out | `RegisterModule()` has commented `ServiceLocator.RegisterServiceInferType(module)` |
| EventBus → ServiceLocator | ❌ Not connected | EventBus is standalone; could be registered as a service |
| ModuleManager → EventBus | ❌ Not connected | No lifecycle events published on module load/unload |
| ServiceLocator → EditorRuntime | ✅ Active | Reports registered services via `CallDeferred(nameof(ReportService), ...)` |

---

## Namespace Consistency

| Class | Declared Namespace | Expected |
|---|---|---|
| `EventBus` | `Ascendere.Events` | ✅ |
| `IEvent` | `Ascendere.Events` | ✅ |
| `ICancellableEvent` | `Ascendere.Events` | ✅ |
| `EventHandlerAttribute` | `Ascendere.Events` | ✅ |
| `SignalHandlerAttribute` | *(global)* | ⚠️ Should be `Ascendere.Events` |
| `EventHistoryEntry` | *(global)* | ⚠️ Should be `Ascendere.Events` |
| `EventStats` | *(global)* | ⚠️ Should be `Ascendere.Events` |
| `ModuleManager` | `Ascendere.Modular` | ✅ |
| `IModule` | *(global)* | ⚠️ Should be `Ascendere.Modular` |
| `ModuleAttribute` | *(global)* | ⚠️ Should be `Ascendere.Modular` |
| `ServiceLocator` | *(global)* | ⚠️ Should be `Ascendere.ServiceLocator` or `Ascendere.Core` |
| `ServiceLifetime` | *(global)* | ⚠️ Should be namespaced |
| `ServiceContext` | *(global)* | ⚠️ Should be namespaced |

---

## Recommendations

1. **Clean up `Event.cs`** — remove `IEventit` and `PlayerJoinedEvent`; these are dead code or misplaced examples.
2. **Add missing namespaces** — `SignalHandlerAttribute`, `EventHistoryEntry`, `EventStats`, `IModule`, `ModuleAttribute`, and all `ServiceLocator` support types need namespace declarations.
3. **Fix `EventBus` fallback instance** — the property getter `if (_instance == null) _instance = new EventBus()` creates an unparented node. Add a warning log and return `null` instead, or rely purely on autoload.
4. **Activate ModulesCore → ServiceLocator bridge** — uncomment and finalize `ServiceLocator.RegisterServiceInferType(module)` in `ModuleManager.RegisterModule()` to make all modules discoverable as services.
5. **Publish lifecycle events from ModuleManager** — emit EventBus events on module load/unload to allow other systems to react.
