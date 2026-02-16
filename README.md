# 🚀 Ascendere

> **The Godot MetaFramework** - A modular, production-ready framework for building scalable games in Godot 4.x with C#

Ascendere is a comprehensive metaframework that provides everything you need to build professional games with Godot and C#. From service locators and event systems to scene management and debugging tools, Ascendere offers a cohesive ecosystem of battle-tested modules that work seamlessly together.

---

## ✨ Features at a Glance

- 🔌 **Plug-and-Play Modules** - Modular architecture with auto-discovery
- 🎯 **Service Locator** - Dependency injection with automatic service discovery
- 📡 **Event Bus** - Type-safe event system with native Godot signal integration
- 🎬 **Scene Manager** - Elegant scene transitions with history and preloading
- 📦 **Registry System** - Type-safe data management for items, entities, and more
- 🐛 **Debug System** - Comprehensive runtime debugging tools
- 📝 **Logger** - Attribute-based logging with configurable levels
- ⚙️ **Editor Extensions** - Command palette, custom tools, and workflow enhancements
- 🎮 **Game Modules** - AI, Inventory, Networking, Quests, SaveLoad, and more

> More features and modules are in development and refinement! Check the repository for updates.

---

## 🏗️ Architecture

Ascendere follows these principles:

- **Modular First** - Everything is a module that can be extended, tweaked, or replaced
- **Godot-Native** - It's an Addon that integrates directly with Godot with no external dependencies
- **Auto-Discovery** - Use attributes, minimal boilerplate
- **Convention Over Configuration** - Sensible defaults, configure only when needed
- **Dependency Injection** - Services are injected, not manually instantiated
- **Declarative Style** - Self-describing code with clear patterns
- **Type Safety** - Generics and interfaces prevent bugs

---

## 🎯 Use Cases

Ascendere is perfect for:

- 🎮 **Mid to large-scale games** requiring proper architecture
- 👥 **Team projects** where consistency and clarity matter
- 🔄 **Long-term projects** that need maintainability
- 🎓 **Learning** modern C# game development patterns
- 🚀 **Rapid prototyping** with production-ready systems


---

## Meta framework?
A meta-framework is a higher-level framework that integrates or builds upon existing framework to provide a more cohesive, opinionated, and feature-rich development environment.

Ascendere is a metaframework that combines core architecture, powerful features and tools, reusable systems, and clear conventions for building games on top of Godot to help you structure, scale, and maintain projects.

Designed for team workflows and long-term development, Ascendere enables rapid prototyping while remaining production-ready.

---

## 🎯 Quick Start

### Installation

1. Clone or download this repository
2. Copy the `addons/ascendere` folder into your Godot project's `addons/` directory
3. Open your project in Godot
4. Go to **Project → Project Settings → Plugins**
5. Enable the **Ascendere** plugin

That's it! Ascendere will automatically initialize its core systems.

### Your First Service

```csharp
using Ascendere;

// 1. Define an interface
public interface IScoreService
{
    int Score { get; }
    void AddPoints(int points);
}

// 2. Implement with [Service] attribute
[Service(typeof(IScoreService))]
public class ScoreService : IScoreService
{
    public int Score { get; private set; }
    
    public void AddPoints(int points)
    {
        Score += points;
        EventBus.Instance.Publish(new ScoreChangedEvent { NewScore = Score });
    }
}

// 3. Use in your nodes
public partial class Player : CharacterBody2D
{
    [Inject] private IScoreService _scoreService;

    public override void _Ready()
    {
        ServiceLocator.InjectMembers(this);
    }

    private void CollectCoin()
    {
        _scoreService?.AddPoints(10);
    }
}
```

No manual registration needed - it's all automatic! ✨

---

## 📚 Core Modules

### 🔌 Service Locator

A powerful dependency injection system with automatic service discovery.

**Features:**
- Automatic service discovery via `[Service]` attribute
- Multiple lifetimes (Singleton, Transient, Scoped)
- Constructor and member injection
- Lazy loading
- Async initialization support
- Event system for service lifecycle tracking

[📖 Full Service Locator Documentation](addons/ascendere/service_locator/README.md)

---

### 📡 Event Bus

Type-safe, attribute-based event system with native Godot signal integration.

**Features:**
- Type-safe event handling
- Priority-based execution order
- Native Godot signal conversion
- Event filtering and debugging
- Subscribe/unsubscribe tracking
- Global and local event scopes

```csharp
// Define an event
public struct PlayerDiedEvent : IEvent
{
    public string PlayerName;
    public Vector2 Position;
}

// Subscribe and handle
[EventHandler(typeof(PlayerDiedEvent))]
private void OnPlayerDied(PlayerDiedEvent evt)
{
    GD.Print($"{evt.PlayerName} died!");
}

// Publish
EventBus.Instance.Publish(new PlayerDiedEvent 
{ 
    PlayerName = "Hero", 
    Position = GlobalPosition 
});
```

[📖 Full Event Bus Documentation](addons/ascendere/events_module/Docs.md)

---

### 🎬 Scene Manager

Sophisticated scene management with transitions, history, and preloading.

**Features:**
- Async scene transitions with `async/await`
- Scene history and navigation
- Background preloading for instant transitions
- `GameScene` base class for elegant flow control
- `LauncherScene` for requirement validation
- Reflection-based scene discovery

```csharp
// Option 1: Elegant flow using GameScene
public partial class MenuScene : GameScene
{
    protected override Type? GetNextSceneType() => typeof(GameplayScene);
    
    private void OnPlayPressed()
    {
        ProceedToNext(); // Automatically transitions to GameplayScene
    }
}

// Option 2: Direct scene management
await SceneManager.ChangeSceneAsync("res://scenes/game.tscn");
await SceneManager.GoBackAsync(); // Navigate backwards in history
```

[📖 Full Scene Manager Documentation](addons/ascendere/scene_manager_module/README.md)

---

### 📦 Registry System

Type-safe, centralized data management for game content.

**Features:**
- Auto-discovery via `[RegistryEntry]` attribute
- Type-safe generic base prevents mixing incompatible types
- Built-in serialization (JSON and Godot Resources)
- Event system for tracking changes
- Load from files or Resources
- Hot-reloading support

```csharp
// Define an item
[RegistryEntry("sword_iron")]
public class IronSword : ISerializableEntry
{
    public string Id => "sword_iron";
    public string Name => "Iron Sword";
    public int Damage => 25;
}

// Auto-registered on startup!
var sword = ItemRegistry.Instance.Get("sword_iron");
```

[📖 Full Registry Documentation](addons/ascendere/registry_module/INTEGRATION_GUIDE.md)

---

### 🐛 Debug System

Comprehensive runtime debugging completely decoupled from your game code.

**Features:**
- 3D debug drawing (lines, spheres, boxes, arrows, labels, paths, rays)
- Real-time value watching
- Custom debug windows
- Performance monitoring (FPS, memory)
- Node inspector
- Debug console with command execution
- Keyboard shortcuts (F1, F2, F3, ~)

```csharp
public override void _Process(double delta)
{
    // Draw velocity arrow
    DebugManager.Instance.DrawArrow3D(
        this, 
        GlobalPosition, 
        GlobalPosition + Velocity,
        Colors.Red
    );
    
    // Watch values in real-time
    DebugManager.Instance.Watch("Speed", Velocity.Length());
    DebugManager.Instance.Watch("Health", _health);
}
```

[📖 Full Debug System Documentation](addons/debug_module/README.md)

---

### 📝 Logger

Lightweight, attribute-based logging with configurable output.

**Features:**
- Attribute-based logging control per class
- Multiple log levels (Debug, Info, Warning, Error)
- Runtime configuration
- Timestamps and type names
- Service locator integration
- Extension methods for easy usage

```csharp
[Log(true)] // Enable logging for this class
public partial class MyGame : Node
{
    public override void _Ready()
    {
        this.LogInfo("Game started!");
        this.LogDebug("Loading resources...");
        this.LogWarning("Low memory!");
        this.LogError("Failed to load asset!");
    }
}
```

[📖 Full Logger Documentation](addons/log_module/README.md)

---

### ⚙️ Modular System

Build and integrate your own modules with the Ascendere architecture.

**Features:**
- Auto-discovery via `[Module]` attribute
- Load order control
- Lifecycle management (Initialize/Cleanup)
- Integration with Service Locator
- Hot-reload support

```csharp
[Module("MyModule", AutoLoad = true, LoadOrder = 10)]
public partial class MyModule : Node, IModule
{
    public string Name => "MyModule";
    public bool IsInitialized { get; private set; }
    
    public void Initialize()
    {
        // Setup your module
        IsInitialized = true;
    }
    
    public void Cleanup()
    {
        // Clean up resources
        IsInitialized = false;
    }
}
```

---

## 🎮 Game Modules

Ascendere includes ready-to-use game modules:

| Module | Description |
|--------|-------------|
| **AI** | Behavior trees, state machines, and AI utilities |
| **Inventory** | Complete inventory system with stacking and categories |
| **Networking** | Multiplayer functionality and network synchronization |
| **Quests** | Quest management with objectives and rewards |
| **SaveLoad** | Game state serialization and save file management |

*More modules coming soon!*

---

## 🛠️ Editor Tools

Ascendere extends the Godot editor with productivity tools:

- **Command Palette** - Quick access to common actions
- **Custom Tools Menu** - Extended editor functionality
- **Editor Settings** - Centralized configuration in Project Settings
- **Custom Docks** - Additional editor panels (coming soon)

---

## 📖 Documentation

Each module has comprehensive documentation:

- [Service Locator](addons/ascendere/service_locator/README.md) - Full API reference and examples
- [Event Bus](addons/ascendere/events_module/Docs.md) - Event system guide
- [Scene Manager](addons/ascendere/scene_manager_module/README.md) - Scene flow patterns
- [Registry System](addons/ascendere/registry_module/INTEGRATION_GUIDE.md) - Integration guide
- [Debug System](addons/debug_module/README.md) - Debugging tools reference
- [Logger](addons/log_module/README.md) - Logging configuration

---

<div align="center">

**Made with Godot 4.x and C#**

</div>
