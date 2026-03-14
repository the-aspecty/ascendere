# Migrating Existing Systems to Ascendere

This guide walks through converting the most common manager and system patterns found in Godot C# projects into Ascendere recommended practices. Each section shows a **before** (typical standalone pattern) and **after** (Ascendere equivalent), and explains the reasoning behind each change.

---

## Table of Contents

1. [Singleton Managers → Service Locator](#1-singleton-managers--service-locator)
2. [Direct Cross-System Calls → Event Bus](#2-direct-cross-system-calls--event-bus)
3. [SceneTree.ChangeScene → Scene Manager](#3-scenetreechangescene--scene-manager)
4. [Static Data Dictionaries → Registry System](#4-static-data-dictionaries--registry-system)
5. [Standalone Node Systems → Modules](#5-standalone-node-systems--modules)
6. [GD.Print Debugging → Logger + Debug System](#6-gdprint-debugging--logger--debug-system)
7. [Custom Editor Scripts → Command Palette & Tools](#7-custom-editor-scripts--command-palette--tools)
8. [Gradual Adoption Strategy](#8-gradual-adoption-strategy)
9. [Wrapping GDScript Addons as C# Modules](#9-wrapping-gdscript-addons-as-c-modules)

---

## 1. Singleton Managers → Service Locator

The most pervasive pattern in Godot C# projects is the static singleton manager. It works but creates hidden coupling, makes testing impossible, and breaks when you need to swap implementations.

### Before — Static Singleton

```csharp
// AudioManager.cs
public partial class AudioManager : Node
{
    public static AudioManager Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
    }

    public void PlaySfx(string path) { /* ... */ }
    public void PlayMusic(string path) { /* ... */ }
}

// Usage anywhere in the project
AudioManager.Instance.PlaySfx("res://sfx/coin.wav");
```

**Problems:** tight static coupling, no way to mock for tests, crashes if the manager hasn't loaded yet.

### After — Ascendere Service

**Step 1 — Define a contract (interface):**

```csharp
// IAudioService.cs
public interface IAudioService
{
    void PlaySfx(string path);
    void PlayMusic(string path);
}
```

**Step 2 — Implement with `[Service]`:**

```csharp
// AudioService.cs
[Service(typeof(IAudioService), ServiceLifetime.Singleton)]
public partial class AudioService : Node, IAudioService
{
    public void PlaySfx(string path) { /* ... */ }
    public void PlayMusic(string path) { /* ... */ }
}
```

Ascendere's `ServiceLocator` discovers and registers this automatically at startup — no `Autoload`, no static field.

**Step 3 — Consume via injection:**

```csharp
// Coin.cs
public partial class Coin : Area2D
{
    [Inject] private IAudioService _audio;

    public override void _Ready()
    {
        ServiceLocator.InjectMembers(this);
    }

    private void OnCollected()
    {
        _audio?.PlaySfx("res://sfx/coin.wav");
    }
}
```

Or resolve manually when injection isn't convenient:

```csharp
if (ServiceLocator.TryGet<IAudioService>(out var audio))
    audio.PlaySfx("res://sfx/coin.wav");
```

### Migration Checklist

- [ ] Create `IMyManager` interface mirroring the public API
- [ ] Add `[Service(typeof(IMyManager))]` to the implementation class
- [ ] Remove `static Instance` field and related `_Ready` assignment
- [ ] Replace every `MyManager.Instance.X()` call with `[Inject]` + `ServiceLocator.InjectMembers(this)`, or `ServiceLocator.TryGet<IMyManager>`
- [ ] Delete the Autoload entry in Project Settings if the manager was autoloaded

---

## 2. Direct Cross-System Calls → Event Bus

Systems that reach directly into each other create spaghetti dependencies. Replace with typed events so systems publish what happened and anyone can react — without knowing about each other.

### Before — Direct Calls

```csharp
// Player.cs
private void Die()
{
    UIManager.Instance.ShowDeathScreen();
    AudioManager.Instance.PlayMusic("res://music/game_over.ogg");
    SaveManager.Instance.DeleteCheckpoint();
    AchievementManager.Instance.Unlock("first_death");
    GetTree().ReloadCurrentScene();
}
```

Every new system that cares about player death requires another line here.

### After — Event Bus

**Step 1 — Define a typed event:**

```csharp
// GameEvents.cs
public struct PlayerDiedEvent : IEvent
{
    public Vector3 Position;
    public string CauseOfDeath;
    public int WaveNumber;
}
```

**Step 2 — Publish the event:**

```csharp
// Player.cs
private void Die()
{
    EventBus.Instance.Publish(new PlayerDiedEvent
    {
        Position = GlobalPosition,
        CauseOfDeath = _lastDamageCause,
        WaveNumber = _currentWave,
    });
}
```

**Step 3 — Each system subscribes independently:**

```csharp
// UISystem.cs — reacts to the event without touching Player
[EventHandler(typeof(PlayerDiedEvent))]
private void OnPlayerDied(PlayerDiedEvent evt)
{
    ShowDeathScreen(evt.WaveNumber);
}

// AudioSystem.cs
[EventHandler(typeof(PlayerDiedEvent))]
private void OnPlayerDied(PlayerDiedEvent evt)
{
    PlayMusic("res://music/game_over.ogg");
}

// AchievementSystem.cs
[EventHandler(typeof(PlayerDiedEvent))]
private void OnPlayerDied(PlayerDiedEvent evt)
{
    Unlock("first_death");
}
```

Adding a new reaction to player death never touches `Player.cs`.

### Migration Checklist

- [ ] Identify every "one system tells another what to do" call chain
- [ ] Create a `struct : IEvent` describing *what happened* (past tense, data-only)
- [ ] Replace the call chain with a single `EventBus.Instance.Publish(...)`
- [ ] Move each reaction into the owning system as `[EventHandler]` method
- [ ] Remove cross-system direct references

---

## 3. SceneTree.ChangeScene → Scene Manager

Raw `GetTree().ChangeSceneToFile()` calls lose history, have no transition, and scatter scene path strings around the codebase.

### Before — Raw Scene Changes

```csharp
// MainMenu.cs
private void OnPlayPressed()
{
    GetTree().ChangeSceneToFile("res://scenes/game.tscn");
}

// GameOver.cs
private void OnRetryPressed()
{
    GetTree().ChangeSceneToFile("res://scenes/game.tscn");
}

private void OnMenuPressed()
{
    GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
}
```

### After — Scene Manager (Option A: Direct API)

```csharp
// GameOver.cs
[Inject] private ISceneManager _scenes;

public override void _Ready() => ServiceLocator.InjectMembers(this);

private async void OnRetryPressed()
{
    await _scenes.ChangeSceneAsync("res://scenes/game.tscn");
}

private async void OnMenuPressed()
{
    await _scenes.GoBackAsync(); // navigates to previous scene in history
}
```

### After — Scene Manager (Option B: GameScene base class)

For a more declarative approach, inherit from `GameScene`. The scene declares its own successor; no string paths scattered across the project.

```csharp
// MainMenu.cs
public partial class MainMenu : GameScene
{
    // Declare where this scene leads
    protected override Type? GetNextSceneType() => typeof(GameplayScene);

    private void OnPlayPressed()
    {
        ProceedToNext(); // transitions automatically
    }
}

// GameplayScene.cs
public partial class GameplayScene : GameScene
{
    protected override Type? GetNextSceneType() => typeof(ScoreScene);
}
```

### Migration Checklist

- [ ] Inject `ISceneManager` wherever scenes are changed
- [ ] Replace `ChangeSceneToFile` with `await _scenes.ChangeSceneAsync(path)`
- [ ] Replace "go back" logic with `await _scenes.GoBackAsync()`
- [ ] Consider converting root scenes to `GameScene` subclasses for type-safe flow
- [ ] Preload expensive scenes with `await _scenes.PreloadAsync(path)` to eliminate load spikes

---

## 4. Static Data Dictionaries → Registry System

Hardcoded item tables, enemy stats, or ability definitions living in static dictionaries or massive switch statements become Registry entries — auto-discovered, serializable, and hot-reloadable.

### Before — Static Dictionary

```csharp
// ItemDatabase.cs
public static class ItemDatabase
{
    public static readonly Dictionary<string, ItemData> Items = new()
    {
        ["sword_iron"]  = new ItemData { Name = "Iron Sword",  Damage = 25 },
        ["sword_steel"] = new ItemData { Name = "Steel Sword", Damage = 40 },
        ["potion_hp"]   = new ItemData { Name = "Health Potion", Heal = 50 },
    };

    public static ItemData Get(string id) => Items[id];
}

// Usage
var item = ItemDatabase.Get("sword_iron");
```

Adding a new item requires editing this file. No serialization, no hot-reload.

### After — Registry System

**Step 1 — Make each entry a discoverable class:**

```csharp
// Items/IronSword.cs
[RegistryEntry("sword_iron")]
public class IronSword : ISerializableEntry
{
    public string Id => "sword_iron";
    public string Name => "Iron Sword";
    public int Damage => 25;
}

// Items/HealthPotion.cs
[RegistryEntry("potion_hp")]
public class HealthPotion : ISerializableEntry
{
    public string Id => "potion_hp";
    public string Name => "Health Potion";
    public int Heal => 50;
}
```

**Step 2 — Query the registry:**

```csharp
// Automatically registered at startup
var sword = ItemRegistry.Instance.Get<IronSword>("sword_iron");
var allItems = ItemRegistry.Instance.GetAll<ISerializableEntry>();
```

Entries can also be loaded from JSON files or Godot Resources — useful for mod support or designer-editable data.

### Migration Checklist

- [ ] Create one class per data entry annotated with `[RegistryEntry("id")]`
- [ ] Replace static dictionary lookups with `Registry.Instance.Get<T>("id")`
- [ ] Remove the static database class
- [ ] Optionally export entries to JSON for designer editing

---

## 5. Standalone Node Systems → Modules

Large node-based systems that live as Autoloads can be wrapped in a `[Module]`, giving them proper lifecycle management, load-order control, and automatic service registration.

### Before — Autoload Node

```csharp
// QuestManager.cs (registered as Autoload "QuestManager")
public partial class QuestManager : Node
{
    public static QuestManager Instance { get; private set; }
    private List<Quest> _activeQuests = new();

    public override void _Ready()
    {
        Instance = this;
        LoadQuests();
    }

    public void StartQuest(string id) { /* ... */ }
    public bool IsComplete(string id) { /* ... */ }
}
```

### After — Ascendere Module

```csharp
// IQuestService.cs
public interface IQuestService
{
    void StartQuest(string id);
    bool IsComplete(string id);
}

// QuestModule.cs
[Module("QuestModule", AutoLoad = true, LoadOrder = 20)]
public partial class QuestModule : Node, IModule, IQuestService
{
    public string Name => "QuestModule";
    public bool IsInitialized { get; private set; }

    private List<Quest> _activeQuests = new();

    public void Initialize()
    {
        ServiceLocator.Register<IQuestService>(this);
        LoadQuests();
        IsInitialized = true;
    }

    public void Cleanup()
    {
        SaveQuests();
        IsInitialized = false;
    }

    public void StartQuest(string id) { /* ... */ }
    public bool IsComplete(string id) { /* ... */ }
}
```

Remove the Autoload entry — `ModuleManager` discovers and starts the module automatically.

```csharp
// Consuming code
[Inject] private IQuestService _quests;

public override void _Ready()
{
    ServiceLocator.InjectMembers(this);
    _quests.StartQuest("find_the_key");
}
```

### Migration Checklist

- [ ] Extract a `IMyService` interface
- [ ] Add `[Module(...)]` to the class, implement `IModule`
- [ ] Move `_Ready` setup into `Initialize()` and teardown into `Cleanup()`
- [ ] Call `ServiceLocator.Register<IMyService>(this)` inside `Initialize()`
- [ ] Remove the Autoload entry
- [ ] Update all `MyManager.Instance` references to `[Inject]` or `TryGet`

---

## 6. GD.Print Debugging → Logger + Debug System

Scattered `GD.Print` calls are hard to filter, disable per-system, or route to a log file.

### Before

```csharp
public partial class EnemyAI : Node
{
    private void UpdateState()
    {
        GD.Print($"[EnemyAI] State changing to {_newState}");
        GD.Print($"[EnemyAI] Target: {_target?.Name}");
    }
}
```

### After — Logger Attribute

```csharp
[Log(true)] // enable logging for this class; set false to silence without removing calls
public partial class EnemyAI : Node
{
    private void UpdateState()
    {
        this.LogDebug($"State changing to {_newState}");
        this.LogDebug($"Target: {_target?.Name}");
    }

    private void OnDamageTaken(int amount)
    {
        this.LogWarning($"Took {amount} damage, health now {_health}");
    }
}
```

For visual runtime inspection, replace watch-variable patterns with the Debug System:

```csharp
// Instead of GD.Print("Speed: " + speed) in every frame:
public override void _Process(double delta)
{
    DebugManager.Instance.Watch("Speed", Velocity.Length());
    DebugManager.Instance.Watch("State", _currentState.ToString());

    // Visualise movement
    DebugManager.Instance.DrawArrow3D(this, GlobalPosition,
        GlobalPosition + Velocity, Colors.Cyan);
}
```

### Migration Checklist

- [ ] Add `[Log(true)]` to classes you want to log
- [ ] Replace `GD.Print` with `this.LogInfo/Debug/Warning/Error`
- [ ] Move per-frame value prints to `DebugManager.Instance.Watch`
- [ ] Move visual debugging (directions, bounds) to `DrawArrow3D/DrawBox3D/etc.`
- [ ] Set `[Log(false)]` or remove `[Log]` entirely to silence noisy classes in production

---

## 7. Custom Editor Scripts → Command Palette & Tools

One-off editor scripts run from the File System or attached to a `[Tool]` node become first-class palette commands or toolbar items discoverable with Ctrl+Shift+P.

### Before — Ad-hoc Tool Script

```csharp
// GenerateNavMesh.cs  (marked [Tool], run by right-clicking in editor)
[Tool]
public partial class GenerateNavMesh : EditorScript
{
    public override void _Run()
    {
        // bake navmesh...
    }
}
```

Requires finding the file, right-clicking, and selecting "Run". Not discoverable.

### After — Command Palette

```csharp
#if TOOLS
[EditorCommandProvider]
public static class WorldCommands
{
    [EditorCommand("Bake NavMesh",
        Description = "Rebakes all navigation meshes in the current scene",
        Category = "World",
        Priority = 100)]
    public static void BakeNavMesh()
    {
        // bake navmesh...
    }

    [EditorCommand("Clear Temp Files",
        Description = "Deletes all files in res://temp/",
        Category = "Project")]
    public static void ClearTempFiles()
    {
        // cleanup...
    }
}
#endif
```

Press **Ctrl+Shift+P**, type "bake", and hit Enter. No file hunting.

### Migration Checklist

- [ ] Create a `static class` with `[EditorCommandProvider]`
- [ ] Move each editor action into its own `static void` method with `[EditorCommand]`
- [ ] Delete the old `EditorScript` / `[Tool]` files
- [ ] Assign short, searchable display names and categories

---

## 8. Gradual Adoption Strategy

You don't need to migrate everything at once. Ascendere is designed to coexist with existing code.

### Recommended Order

| Phase | Focus | Effort |
|-------|-------|--------|
| **1 — Foundation** | Enable Ascendere plugin, keep existing code running | Low |
| **2 — Services** | Migrate the 2–3 most-shared managers (audio, UI, input) | Medium |
| **3 — Events** | Replace the most tangled call chains with events | Medium |
| **4 — Scenes** | Adopt Scene Manager for main flow transitions | Low |
| **5 — Data** | Move static item/enemy tables to Registry | Medium |
| **6 — Modules** | Wrap remaining Autoloads as proper Modules | Low per system |
| **7 — Polish** | Logger, Debug System, editor commands | Low |

### Bridging Existing Singletons

If you can't convert a manager immediately, you can register the existing instance manually so other systems can use `ServiceLocator` to retrieve it:

```csharp
// In your existing manager's _Ready, while migrating:
public override void _Ready()
{
    Instance = this; // keep for legacy callers
    ServiceLocator.Register<IMyManager>(this); // new callers use the locator
}
```

This lets you migrate call-sites file by file without a big-bang rewrite.

### Keeping Signals

Godot signals don't need to be replaced — the Event Bus complements them. Use signals for tightly-coupled parent–child node communication; use the Event Bus for cross-system notifications where the publisher shouldn't know who's listening.

---

## 9. Wrapping GDScript Addons as C# Modules

Many great Godot addons are written in GDScript. Rather than rewriting them or calling GDScript API directly from everywhere in your C# codebase, wrap them behind a typed C# interface registered in the ServiceLocator. The rest of your game never knows it's talking to a GDScript object.

### Why Wrap Instead of Call Directly?

| Direct GDScript calls | C# Wrapper Module |
|---|---|
| Stringly-typed: `GetNode("MyAddon").Call("do_thing")` | Strongly-typed: `_myAddon.DoThing()` |
| Errors are runtime, not compile-time | Errors caught by the compiler |
| Addon name scattered across codebase | Single place to update if addon changes |
| Cannot be mocked for tests | Interface can be stubbed |
| No lifecycle control | `Initialize`/`Cleanup` in module |

---

### Pattern: Godot.Object Bridge

The core technique is calling GDScript methods via `GodotObject.Call()` / `GodotObject.Get()` / `GodotObject.Set()` inside a thin C# class, then exposing a clean typed interface.

#### Example — Wrapping a GDScript Singleton Addon

Suppose you have a GDScript addon registered as an autoload that exposes:

```gdscript
# GDScript API (addon code, do not touch)
func perform_action(data: String) -> void
func cancel_action() -> void
func is_busy() -> bool
signal action_completed
```

**Step 1 — Define the C# interface:**

```csharp
// IMyAddonService.cs
public interface IMyAddonService
{
    void PerformAction(string data);
    void CancelAction();
    bool IsBusy { get; }
    event Action ActionCompleted;
}
```

**Step 2 — Write the wrapper module:**

```csharp
// MyAddonModule.cs
[Module("MyAddonModule", AutoLoad = true, LoadOrder = 15)]
public partial class MyAddonModule : Node, IModule, IMyAddonService
{
    public string Name => "MyAddonModule";
    public bool IsInitialized { get; private set; }

    public event Action ActionCompleted;

    // Reference to the GDScript singleton / autoload
    private GodotObject _addon;

    public void Initialize()
    {
        // Resolve the GDScript autoload by its registered name
        _addon = Engine.GetSingleton("MyAddon");

        if (_addon == null)
        {
            GD.PrintErr("[MyAddonModule] Autoload not found. Is the addon enabled?");
            return;
        }

        // Connect the GDScript signal to a C# handler
        _addon.Connect("action_completed",
            new Callable(this, MethodName.OnActionCompleted));

        ServiceLocator.Register<IMyAddonService>(this);
        IsInitialized = true;
    }

    public void Cleanup()
    {
        if (_addon != null && _addon.IsConnected("action_completed",
            new Callable(this, MethodName.OnActionCompleted)))
        {
            _addon.Disconnect("action_completed",
                new Callable(this, MethodName.OnActionCompleted));
        }

        IsInitialized = false;
    }

    // ── IMyAddonService ───────────────────────────────────────

    public void PerformAction(string data)
        => _addon?.Call("perform_action", data);

    public void CancelAction()
        => _addon?.Call("cancel_action");

    public bool IsBusy
        => _addon?.Call("is_busy").AsBool() ?? false;

    // ── Signal bridge ─────────────────────────────────────────

    private void OnActionCompleted()
        => ActionCompleted?.Invoke();
}
```

**Step 3 — Use it from C# with full type safety:**

```csharp
// MyController.cs
public partial class MyController : Node
{
    [Inject] private IMyAddonService _addon;

    public override void _Ready()
    {
        ServiceLocator.InjectMembers(this);
        _addon.ActionCompleted += OnAddonFinished;
    }

    public override void _ExitTree()
    {
        _addon.ActionCompleted -= OnAddonFinished;
    }

    public void TriggerAction(string data) => _addon.PerformAction(data);

    private void OnAddonFinished()
    {
        // respond to completion
    }
}
```

---

### Pattern: Node-Based GDScript Addon

Some addons must exist as nodes in the scene tree (e.g. they use `_process` or emit signals as part of the tree). Instantiate them inside the module's `Initialize` and add them as children.

```csharp
[Module("InventoryUIModule", AutoLoad = true, LoadOrder = 30)]
public partial class InventoryUIModule : Node, IModule, IInventoryUIService
{
    public string Name => "InventoryUIModule";
    public bool IsInitialized { get; private set; }

    private Node _addonRoot;

    public void Initialize()
    {
        // Load and instantiate the GDScript-based addon scene
        var scene = GD.Load<PackedScene>("res://addons/inventory_ui/InventoryUI.tscn");
        _addonRoot = scene.Instantiate();
        _addonRoot.Name = "InventoryUI";
        AddChild(_addonRoot);

        ServiceLocator.Register<IInventoryUIService>(this);
        IsInitialized = true;
    }

    public void Cleanup()
    {
        _addonRoot?.QueueFree();
        IsInitialized = false;
    }

    // Typed wrappers around the GDScript node's API
    public void Show() => _addonRoot?.Call("show_inventory");
    public void Hide() => _addonRoot?.Call("hide_inventory");
    public bool IsVisible => _addonRoot?.Call("is_inventory_visible").AsBool() ?? false;
}
```

---

### Pattern: GDScript Resource / Data Addon

Addons that expose data as GDScript `Resource` subtypes (e.g. quest databases, localisation keys) can be loaded with `GD.Load` and have their properties read via `GodotObject.Get()`.

```csharp
public partial class LocalizationModule : Node, IModule, ILocalizationService
{
    private GodotObject _translations;

    public void Initialize()
    {
        // Load GDScript resource compiled by the addon
        _translations = GD.Load<Resource>("res://addons/localization/data/en.tres");
        ServiceLocator.Register<ILocalizationService>(this);
        IsInitialized = true;
    }

    public string Get(string key)
    {
        var value = _translations?.Get(key);
        return value.VariantType != Variant.Type.Nil
            ? value.AsString()
            : $"[MISSING:{key}]";
    }

    public string Name => "LocalizationModule";
    public bool IsInitialized { get; private set; }
    public void Cleanup() => IsInitialized = false;
}
```

---

### Handling GDScript Signals

GDScript signals are connected to C# using `new Callable(this, MethodName.MyHandler)`. Always disconnect in `Cleanup` or `_ExitTree` to avoid leaks — check with `IsConnected` first as required by Godot's reload-safety rules.

```csharp
// Connect
_gdObj.Connect("my_signal", new Callable(this, MethodName.OnMySignal));

// Disconnect (always guard with IsConnected)
if (_gdObj.IsConnected("my_signal", new Callable(this, MethodName.OnMySignal)))
    _gdObj.Disconnect("my_signal", new Callable(this, MethodName.OnMySignal));
```

---

### GDScript Addon Migration Checklist

- [ ] Identify every `GetNode("Addon").Call(...)` or `Autoload.Instance.method()` call in C# code
- [ ] Define an `IMyAddonService` interface with strongly-typed methods and events
- [ ] Create a `[Module]` wrapper that resolves the GDScript object in `Initialize`
- [ ] Translate each GDScript call into a typed method body using `.Call()` / `.Get()` / `.Set()`
- [ ] Bridge GDScript signals to C# `event Action` / `event Action<T>` in the wrapper
- [ ] Register the module as `IMyAddonService` in the ServiceLocator
- [ ] Replace all raw call-sites with `[Inject] IMyAddonService`
- [ ] If the addon is ever replaced with a native C# version, only the module class changes — all consumers stay the same

---

## Quick Reference

| Old Pattern | Ascendere Equivalent |
|---|---|
| `MyManager.Instance.X()` | `[Inject] IMyService _s` + `ServiceLocator.InjectMembers(this)` |
| Static singleton Autoload | `[Service(typeof(IMyService))]` on implementation |
| Manual `GetNode<>()` between siblings | `ServiceLocator.TryGet<IMyService>()` |
| `SomeSiblingNode.SomeMethod()` | `EventBus.Instance.Publish(new SomeEvent {...})` |
| `GetTree().ChangeSceneToFile(path)` | `await _scenes.ChangeSceneAsync(path)` |
| Static item dictionary | `[RegistryEntry("id")]` classes + `Registry.Instance.Get<T>()` |
| Autoload node system | `[Module("Name")] + IModule.Initialize/Cleanup` |
| `GD.Print("[Tag] message")` | `[Log(true)]` + `this.LogDebug("message")` |
| Per-frame watch print | `DebugManager.Instance.Watch("key", value)` |
| `[Tool]` editor script | `[EditorCommandProvider]` + `[EditorCommand]` |
| `GetNode("Addon").Call("method")` | `[Inject] IAddonService` via `[Module]` wrapper |
