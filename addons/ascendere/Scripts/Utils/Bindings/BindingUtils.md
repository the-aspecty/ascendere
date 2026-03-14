# Binding Utils — Developer Guide

**Namespace**: `Ascendere.Utils.Bindings`  
**Location**: `addons/ascendere/Scripts/Utils/Bindings/`

Shared infrastructure for every Ascendere module that exposes a reactive, fluent
binding API (`When(...).Do(...)`, `On(...).ItemAdded().Do(...)`). Using these
primitives eliminates duplicated dispatch loops, lifetime-tracking bugs, and
closure pitfalls that existed in the original per-module implementations.

---

## Core Types

### `BindingRecord<TContext>`

The shared storage unit for a single registered callback. Replaces every
`ActiveBinding` inner class that previously lived inside each registry.

```csharp
public sealed class BindingRecord<TContext>
{
    public bool           Alive    = true;   // set false to soft-delete
    public bool           Once;              // remove after first invocation
    public Node           Owner;             // null = unowned (persists until manual removal)
    public Func<TContext, bool>? Filter;     // null = match every context
    public Action<TContext>     Callback;

    public bool ShouldPrune();               // !Alive || owner freed
}
```

**Key rules**:
- Capture only what the filter/callback needs — not the builder object itself. This
  allows the builder to be GC'd after `Do()` is called.
- `Filter = null` means "always invoke" — do not use `_ => true` (allocates a closure).

---

### `BindingList<TContext>`

A managed list that owns the dispatch loop. All modules delegate their iteration
and pruning here rather than maintaining their own backwards-iterate loops.

```csharp
public sealed class BindingList<TContext>
{
    public void Add(BindingRecord<TContext> record);
    public void RemoveAllByOwner(Node owner);       // eager removal (called from OwnerLifetimeTracker)
    public void SoftRemoveAllByOwner(Node owner);   // flags Alive=false without list mutation
    public void Clear();
    public void Dispatch(TContext context);
    public int  Count { get; }
}
```

**`Dispatch` guarantees**:
1. **Re-entrancy guard** — if a callback triggers another `Dispatch()` on the same
   `BindingList`, the inner call is blocked and an error is logged. Schedule
   mutations with `CallDeferred()` instead.
2. **Backwards iteration** — safe for in-place removal without index shifts.
3. **Prune-dead** — records whose `ShouldPrune()` returns `true` are removed.
4. **Once-remove** — records marked `Once` are removed after their callback fires.
5. **Exception isolation** — a throwing callback logs the error and removes that
   record; it does not break iteration for remaining callbacks.

---

### `OwnerLifetimeTracker`

Manages `TreeExiting` signal hookups for Node owners. Replaces the untrackable
inline lambda pattern `owner.TreeExiting += () => RemoveOwner(owner)`.

```csharp
public sealed class OwnerLifetimeTracker
{
    public void Track(Node owner, Action onExit);   // idempotent; accumulates actions
    public void Forget(Node owner);                 // disconnects signal, removes tracking
    public void ForgetAll();                        // call from registry ClearAll()
}
```

**Behaviour**:
- Multiple `Track()` calls for the **same node** combine their actions — only one
  `TreeExiting` subscription is ever made per node.
- `Forget()` disconnects the stored delegate (preventing the auto-cleanup from
  double-running if the owner manually calls `RemoveOwner`).
- `Track()` silently ignores freed nodes (`IsInstanceValid` check).

---

### `FluentBuilderBase<TSelf>`

CRTP abstract base for fluent builder chains. Provides `Once()` and `OwnedBy()`
that are identical across all modules.

```csharp
public abstract class FluentBuilderBase<TSelf>
    where TSelf : FluentBuilderBase<TSelf>
{
    protected bool IsOnce { get; }
    protected Node Owner  { get; }

    public TSelf Once();
    public TSelf OwnedBy(Node owner);
}
```

---

## Creating a New Module With Fluent Bindings

Follow this checklist to add the binding pattern to a new module. Each step has
a one-liner description of what it replaces.

### 1. Define your context type

```csharp
// The value dispatched on every relevant event. Use a struct for zero-allocation dispatch.
public readonly struct MyEvent
{
    public readonly string Key;
    public readonly int    Value;
    public MyEvent(string key, int value) { Key = key; Value = value; }
}
```

### 2. Create the registry

```csharp
// addons/ascendere/Modules/MyModule/Scripts/Fluent/MyBindingRegistry.cs
internal static class MyBindingRegistry
{
    private static readonly BindingList<MyEvent>      _list    = new();
    private static readonly OwnerLifetimeTracker      _tracker = new();

    internal static void Register(
        Node                    owner,
        Func<MyEvent, bool>     filter,
        Action<MyEvent>         callback,
        bool                    once)
    {
        _list.Add(new BindingRecord<MyEvent>
        {
            Filter   = filter,
            Callback = callback,
            Once     = once,
            Owner    = owner,
        });

        if (owner != null)
            _tracker.Track(owner, () => _list.RemoveAllByOwner(owner));
    }

    internal static void Dispatch(MyEvent evt) => _list.Dispatch(evt);

    internal static void ClearAll()
    {
        _list.Clear();
        _tracker.ForgetAll();
    }

    internal static int Count => _list.Count;
}
```

### 3. Create the fluent builder

```csharp
// addons/ascendere/Modules/MyModule/Scripts/Fluent/MyBinding.cs
public sealed class MyBinding : FluentBuilderBase<MyBinding>
{
    private string _keyFilter; // module-specific fields only

    internal MyBinding() { }

    public MyBinding WithKey(string key) { _keyFilter = key; return this; }

    // Terminal
    public MyBinding Do(Action<MyEvent> callback)
    {
        var key = _keyFilter; // capture scalar, not 'this'
        MyBindingRegistry.Register(
            owner:    Owner,
            filter:   evt => key == null || evt.Key == key,
            callback: callback,
            once:     IsOnce
        );
        return this;
    }
}
```

### 4. Expose `On()` from your service / manager

```csharp
public MyBinding On() => new MyBinding();
```

### 5. Fire events from your manager's mutating methods

```csharp
MyBindingRegistry.Dispatch(new MyEvent(key, newValue));
```

### 6. Wire `ClearAll()` as needed

Call `MyBindingRegistry.ClearAll()` during scene resets or module shutdown if the
registry is static and bindings should not persist across scene changes.

---

## Migration Guide (Existing Modules)

| Old pattern | New pattern |
|---|---|
| `ActiveBinding` inner class | `BindingRecord<TContext>` |
| Backwards-iterate `for` loop with prune + once | `BindingList<TContext>.Dispatch()` |
| `private bool _once; private Node _owner;` in builder | Inherit `FluentBuilderBase<TSelf>` |
| `public T Once() { _once = true; return this; }` | Removed — provided by base |
| `public T OwnedBy(Node n)` in builder | Removed — provided by base |
| `owner.TreeExiting += () => RemoveOwner(owner)` | `_tracker.Track(owner, () => ...)` |
| `IsInstanceValid` check inline in loop | Handled by `BindingRecord.ShouldPrune()` |
| Bare `List.RemoveAt(i)` in dispatch loop | Handled by `BindingList.Dispatch()` |

---

## Analyzer

> This section documents **planned** Roslyn analyzers and runtime diagnostics for
> the binding pattern. Items prefixed `ASCB` are candidates for a future
> `Ascendere.Analyzers` NuGet package.

### ASCB001 — `Do()` without `OwnedBy()` on a Node

**Severity**: Warning  
**Description**: When `Do()` is called on a builder instance inside a Godot Node
subclass without a preceding `OwnedBy(this)`, the binding will persist until
`ClearAll()` is called, even after the node is freed. The prune cycle will
eventually clean it up, but callbacks can fire on the freed node between events.

**Detection**: The method chain contains `Do(...)` but not `OwnedBy(...)` / `Once()`,
and the enclosing type inherits `Godot.Node`.

**Fix**: Add `.OwnedBy(this)` before `.Do(...)`.

---

### ASCB002 — Closure captures `this` (the builder object)

**Severity**: Info  
**Description**: Inside `RegisterBinding()` / `Do()`, if the filter or callback
lambda captures a field via `this` (e.g. `filter: evt => evt.Key == _key`) rather
than a local copy (`var key = _key; filter: evt => evt.Key == key`), the builder
object is kept alive for the entire lifetime of the binding. For short-lived
builder objects this is a minor memory overhead; it also prevents GC of any other
fields captured transitively.

**Detection**: Lambda inside a method named `RegisterBinding` or ending in `Do`
accesses `this.<field>`.

**Fix**: Capture the required values as local variables before the lambda.

---

### ASCB003 — Modifier called after terminal (`Once()` after `Do()`)

**Severity**: Error  
**Description**: Calling `Once()` or `OwnedBy()` after a terminal method like
`Do()` has no effect because the binding was already committed to the registry.
The method returns `this` (for chaining) but the registry record is immutable.

**Detection**: A call to `Once()` or `OwnedBy()` appears after `Do()` in the
same method-chain expression.

**Fix**: Reorder the chain — modifiers must precede the terminal.

---

### ASCB004 — Static registry binding registered outside `_Ready`

**Severity**: Warning  
**Description**: Static registries (`FlagBindingRegistry`, `InventoryBindingRegistry`)
survive scene changes. Registering a binding during `_Process`, `_PhysicsProcess`,
or event handlers (rather than `_Ready` / `_EnterTree`) may create duplicate
bindings on each call, accumulating unbounded callbacks.

**Detection**: `Register(...)` / `Do(...)` called on a static-registry-backed
builder inside a method other than `_Ready` or `_EnterTree`, without a guarding
field flag.

**Fix**: Move registration to `_Ready`/`_EnterTree`, or guard with a boolean flag
(`if (_bound) return; _bound = true;`).

---

### ASCB005 — Re-entrant mutation (mutating source from callback)

**Severity**: Warning  
**Description**: Calling a mutating method on the same registry from within a
binding callback causes re-entrant dispatch. For example, calling `flags.Set("xp", 0)`
inside a `When("xp").Do(...)` callback will trigger the `_dispatchDepth` guard and
log an error at runtime.

**Detection** (runtime only today, static future): The same registry's `Dispatch`
or mutation method is on the call stack when a binding callback fires. Static
detection would require data-flow analysis across lambda closures.

**Fix**: Schedule the mutation with `CallDeferred(() => flags.Set("xp", 0))`.

---

### ASCB006 — `Filter = null` vs `Filter = _ => true` (performance)

**Severity**: Info  
**Description**: Using `filter: _ => true` in `Register(...)` allocates a heap
closure object and incurs a virtual-call overhead on every dispatch. The
`BindingList` dispatch loop special-cases `Filter == null` to skip the call
entirely.

**Detection**: Lambda body is a literal `true` expression with any parameter.

**Fix**: Pass `filter: null` when no filtering is needed.

---

### ASCB007 — `OwnerLifetimeTracker.Track` called without `BindingList.RemoveAllByOwner`

**Severity**: Warning  
**Description**: `_tracker.Track(owner, someAction)` is only useful when
`someAction` results in the binding being removed from the list. If `someAction`
is a no-op or doesn't remove entries, nodes will still exit tree correctly but
dead records will linger in the list until the next prune cycle.

**Detection**: The lambda passed to `Track` doesn't call `RemoveAllByOwner` or
`SoftRemoveAllByOwner` on the associated `BindingList`.

**Fix**: Pass `() => _list.RemoveAllByOwner(owner)` as the cleanup action.
