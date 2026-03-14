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
- 🖋️ **Fluent APIs** - Every major module exposes a chainable fluent builder so you write intent, not boilerplate
<!-- - 🎥 **Camera Module** - Cinemachine-style virtual cameras, blending, impulse, noise, split-screen, and sequencing
- 🔊 **Audio Module** - Full audio pipeline: music, SFX, ambient, voice, spatial, bus snapshots, and ducking -->
- ⚙️ **Editor Extensions** - Command palette, Scene DSL, live editor-runtime bridge, and workflow tools
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

## 🖋️ Fluent APIs

Every major Ascendere module exposes a discoverable, chainable fluent interface. Instead of wiring nodes manually, you express *what* you want:

```csharp
// UI — compose whole screens without manual node plumbing
using UIModule.Core.Utils;

var panel = UIBuilder
    .CreateCenteredVBox(spacing: 12, width: 400, height: 300)
    .AddChild(UIBuilder.CreateHeader("Settings", fontSize: 24))
    .AddChild(UIBuilder.CreateControlGroup("Master Volume", new HSlider()))
    .AddChild(UIBuilder.CreateButton("Apply", OnApply));

// Scene scaffolding — generate a fully wired scene file from code
SceneBuilder
    .FromTemplate("CharacterBody3D", "Player")
    .WithScript("Player.cs")
    .AtPath("res://scenes/player.tscn")
    .Build();
```

Fluent builders are available for:
<!-- - **Camera** — `CameraFluent.VCam()`, `CameraFluent.Body()`, `CameraFluent.Aim()`, `CameraFluent.Blend()`, `CameraFluent.Impulse()` -->
- **UI** — `UIBuilder.CreateVBox/HBox()`, `CreateCenteredVBox()`, `CreateHeader()`, `CreateCard()`, `CreateControlGroup()`, `CreateNavigationBar()`, and more
- **Scene** — `SceneBuilder.FromTemplate()`, `.WithScript()`, `.AtPath()`, `.Build()`

---

<!-- @todo  audio & cam-->

<!-- ## 🎥 Camera Module

A Cinemachine-style virtual camera system for Godot 4 C#. Priority-driven VCams run through composable **Body** (position) → **Aim** (rotation) → **Extension** (post-effects) pipelines before driving a real `Camera3D` or `Camera2D`.

**Features:**
- Priority-driven VCam selection with smooth blending
- Pluggable bodies: Follow, ThirdPerson, Framing, Orbital, TrackedDolly, HardLock
- Pluggable aims: HardLookAt, Composer, GroupComposer, POV, AimAtNothing
- Extensions: Collision avoidance, Confiner, Pixel-Perfect, Recomposer, Noise, Shake, ImpulseListener
- Spatial impulse propagation (`IImpulseService`)
- Timeline/keyframe sequencer and state-driven cameras
- Split-screen support
- Runtime debug overlay (F10) showing live position, rotation, FOV, zoom, and active VCam
- Fluent API (`CameraFluent`) for zero-boilerplate setup

```csharp
// Minimal 3D follow cam via fluent API
var vcam = CameraFluent
    .VCam(priority: 10)
    .Body.Follow(target, offset: new Vector3(0, 2, 6))
    .Aim.HardLookAt(target)
    .Build();
AddChild(vcam);
AddChild(new CameraBrain3D());
```

[📖 Camera Module Documentation](addons/camera_module/README.md)

---

## 🔊 Audio Module

A complete, production-ready audio pipeline with **67 source files** across 19 subsystems, all behind a single `IAudioService` facade.

**Features:**
- Music playback with crossfade, looping, and intro/loop/outro segments
- Pooled SFX playback with variation, pitch randomisation, and cooldown
- Ambient soundscapes with zone-based blending
- Voice/dialogue queue with priority, interrupt policies, and lip-sync hooks
- Spatial audio: `AudioEmitter3D/2D`, `AudioListener`, occlusion raycasting, `AudioZone3D/2D`
- Bus management, mixer snapshots, and automatic ducking
- Spectrum analyser, waveform provider, and beat detector for visualisation
- Accessibility: subtitles, mono mix, and volume profiles
- Full event system for every audio lifecycle callback

```csharp
[Inject] private IAudioService _audio;

public override void _Ready()
{
    ServiceLocator.InjectMembers(this);
    _audio.PlayMusic(introTrack, fadeDuration: 2f);
}

private void OnPlayerHit()
{
    _audio.PlaySfx(hitSfx, GlobalPosition);
}
```

[📖 Audio Module Documentation](addons/audio_module/README.md)

--- -->

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

### Scene Builder

A **DSL-driven scene scaffolding** tool accessible from the Godot toolbar. Stamp complete, fully wired scenes directly into your project from reusable templates — no manual node setup required.

- **DSL Scenes** — Tag any static C# method with `[SceneDSL]` to expose it as a one-click toolbar action
- **Templates** — Register named templates via `SceneBuilder.RegisterTemplate()`; pick from the menu to generate a scene file with pre-configured nodes, scripts, and paths
- **Composite Templates** — Bundle multiple templates into a single action for full feature scaffolding
- **Hot-Reload Safe** — Template registry is rebuilt on every plugin reload with no stale state

```csharp
[SceneDSL("My Scenes/Player Scene")]
public static void CreatePlayerScene()
{
    SceneBuilder
        .FromTemplate("CharacterBody3D", "Player")
        .WithScript("Player.cs")
        .AtPath("res://scenes/player.tscn")
        .Build();
}
```

### Editor Runtime Bridge

Enables **live bidirectional communication** between the Godot editor and the running game. Useful for hot-sending config, tweaking values without restarting, or pushing telemetry back to the editor during play.

- **Runtime Data dock** — Live panel in the editor showing messages from the game
- **Ping / Send** — Send arbitrary messages to the game and receive responses in real time
- **Custom message types** — Extend `RuntimeMessage` and implement `IMessageHandler<T>` for domain-specific commands
- **Zero game-code coupling** — The bridge is fully decoupled; game code only knows about `RuntimeBridge`, not the editor

```csharp
// In your game (runtime side)
public class HealPlayerHandler : IMessageHandler<HealPlayerMessage>
{
    public void Handle(HealPlayerMessage msg, RuntimeBridge bridge)
    {
        Player.Instance.Heal(msg.Amount);
        bridge.Send(new AckMessage { Ok = true });
    }
}
```

### Command Palette & Tools Menu

Ascendere provides **code-level building blocks** so you can extend the editor's own command palette and tools menu with your own commands — all discovered automatically, no wiring required.

**Command Palette** — Tag any static method with `[EditorCommand]` and it appears in Godot's native **Ctrl+Shift+P** palette, organised by category:

```csharp
#if TOOLS
[EditorCommandProvider]
public static class MyProjectCommands
{
    [EditorCommand("Reset Player Position",
        Description = "Teleports player to origin",
        Category = "Debug",
        Priority = 50)]
    public static void ResetPlayerPosition()
    {
        // runs in the editor context
    }
}
#endif
```

**Tools Menu** — Register custom menu items that appear under the Godot toolbar's **Project → Tools** dropdown:

```csharp
#if TOOLS
[EditorToolsProvider]
public static class MyTools
{
    [EditorTool("Generate Navmesh", Category = "World")]
    public static void BakeNavmesh() { /* ... */ }
}
#endif
```

Both systems use the same auto-discovery pattern: the `CommandPaletteManager` scans assemblies at plugin load and registers everything it finds — no `AddChild`, no manual subscriptions, no cleanup code needed.

---

<!-- @todo  -->
<!-- ## Ascendere CLI

A .NET CLI tool for scaffolding projects, managing modules, and building mods — all from the terminal.

```bash
# Install globally
dotnet tool install -g Ascendere.Cli

# Create a new project from a template
ascendere new MyGame --template moddable

# Add modules to an existing project
ascendere modules add camera_module audio_module

# Check project health
ascendere doctor

# Create, build, and package a mod
ascendere mod new mymod.crafting --name "Crafting System"
ascendere mod build --release --validate
ascendere mod pack
```

**Commands:** `new` · `modules` · `mod` · `sdk` · `info` · `doctor` · `clean` · `template`

[📖 Full CLI Documentation](tools/cli/CLI-DOCS.md)

--- -->

## �📖 Documentation

Each module has comprehensive documentation:

- [Service Locator](addons/ascendere/service_locator/README.md) - Full API reference and examples
- [Event Bus](addons/ascendere/events_module/Docs.md) - Event system guide
- [Scene Manager](addons/ascendere/scene_manager_module/README.md) - Scene flow patterns
- [Registry System](addons/ascendere/registry_module/INTEGRATION_GUIDE.md) - Integration guide
- [Debug System](addons/debug_module/README.md) - Debugging tools reference
- [Logger](addons/log_module/README.md) - Logging configuration
- [Camera Module](addons/camera_module/README.md) - Virtual cameras, blending, fluent API
- [Audio Module](addons/audio_module/README.md) - Full audio pipeline reference
- [Editor Runtime](addons/editorruntime/README.md) - Editor-game bridge

---


## Migration Guide

To help you migrate an existing Godot/C# project into Ascendere, the migration guide walks through the most common patterns (singleton managers, scene switching, static data, etc.) and shows how to replace them with Ascendere equivalents. Read it at [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md).

## Adapters

Ascendere uses *Adapters* to safely connect modules without creating direct dependencies between them. They live in `addons/ascendere/Adapters/` and are documented in [ADAPTERS.md](ADAPTERS.md).

## License

Ascendere is licensed under the **Ascendere Community License (ACL-1.0)**. That means you are free to use, modify, and ship Ascendere inside your games (including commercial titles), but you may not redistribute the framework itself as a standalone addon/library or rebrand it without permission. See the full terms in [LICENSE](LICENSE).

> Note: Premium/paid modules (e.g. **UI Kit Pro**) are governed by a separate commercial license.
