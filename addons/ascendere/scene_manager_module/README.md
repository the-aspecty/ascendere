# Scene Manager Module

A modular, extensible scene management system for Godot 4.x with C#. Provides automatic scene transitions, history management, preloading, and elegant flow control via `GameScene` base class.

## Features

- **Autoload Singleton**: Global `SceneManager` handles all scene transitions
- **Scene History**: Track visited scenes and navigate backward with `GoBackAsync()`
- **Asynchronous Transitions**: Use `async/await` for scene change operations
- **Preloading System**: Load scenes in background for faster transitions
- **GameScene Base Class**: Abstract base class for elegant scene flow definition
- **LauncherScene**: Requirement validation framework for game startup checks
- **Signal/Event System**: Built-in signals for scene change, loading, and launcher events
- **Dependency Injection**: Full integration with ServiceLocator for clean service access
- **Reflection-based Discovery**: Automatic collection of all GameScene subclasses at startup

## Quick Start

### 1. Setup SceneManager Autoload

In Godot Editor, go to **Project Settings → Autoload** and add:
- **Node Name**: `SceneManager`
- **Path**: `res://addons/ascendere/scene_manager_module/SceneManager.cs`

### 2. Create Your First GameScene

```csharp
using System;
using Godot;

public partial class MenuScene : GameScene
{
    private Button _playButton;

    public override void _Ready()
    {
        base._Ready();
        _playButton = GetNode<Button>("PlayButton");
        _playButton.Connect(Button.SignalName.Pressed, 
            new Callable(this, MethodName.OnPlayPressed));
    }

    private void OnPlayPressed()
    {
        ProceedToNext(); // Transitions to GameplayScene
    }

    protected override Type? GetNextSceneType()
    {
        return typeof(GameplayScene);
    }
}
```

### 3. Transition Between Scenes

```csharp
// Option A: Elegant flow using GameScene (recommended)
ProceedToNext(); // Uses GetNextSceneType() and validates with CanProceedToNext()

// Option B: Direct scene path
await SceneManager.ChangeSceneAsync("res://scenes/game.tscn");

// Option C: Navigate history
await SceneManager.GoBackAsync();
```

## Architecture

### Core Components

#### SceneManager (Autoload)
- Central singleton managing all scene transitions
- Maintains history stack (configurable max size)
- Tracks preloaded scenes
- Emits signals for scene lifecycle events
- Automatically collects GameScene subclasses via reflection

**Key Methods:**
- `ChangeSceneAsync(string scenePath)` - Change to scene by path
- `ChangeSceneAsync(Type gameSceneType)` - Change to scene by GameScene type
- `GoBackAsync(bool clearHistory = true)` - Navigate to previous scene
- `PreloadScene(string path)` - Async preload for faster transitions
- `UnloadScene(string path)` - Unload preloaded scene
- `ClearHistory()` - Clear the navigation stack
- `GetRegisteredGameScenes()` - Get list of all discovered GameScene types

**Signals:**
- `SceneChanged(scenePath: string)` - Emitted when scene changes
- `SceneLoading(scenePath: string, progress: float)` - Loading progress updates

#### GameScene (Abstract Base Class)
Elegant scene flow management with validation.

**Abstract Members:**
- `GetNextSceneType()` - Define what scene comes next
- `GetScenePathForType(Type)` - Override path resolution (default: `res://scenes/{TypeName}.tscn`)

**Virtual Members:**
- `CanProceedToNext()` - Validation hook (default: always true)

**Methods:**
- `ProceedToNext()` - Validate and transition to next scene
- `LoadScene(string path)` - Load a scene instance as child
- `UnloadCurrentScene()` - Clean up loaded scene
- `SwapToScene(string path)` - Load and replace current scene

**Signals:**
- `SceneLoaded(packed: PackedScene, instance: Node)`
- `SceneUnloaded(packed: PackedScene)`
- `ReadyToProceed(canProceed: bool)`

**Properties:**
- `SceneManager` - Injected via ServiceLocator
- `CurrentPackedScene` - Currently loaded PackedScene
- `CurrentInstance` - Instance node of loaded scene
- `IsSceneLoaded` - Whether a scene is active and in tree

#### LauncherScene (Extends GameScene)
Validates requirements before starting the game.

**Key Features:**
- Asynchronous requirement checking
- Extensible requirement system via `ILaunchRequirement`
- Auto-recovery for fixable issues (create missing directories, etc.)
- Progress tracking via signals

**Signals:**
- `RequirementChecked(name: string, passed: bool)`
- `AllRequirementsMet`
- `RequirementsFailed(failed: string[])`

**Exported Properties:**
- `NextSceneTypeName` - Full type name of next scene after validation
- `AutoProceed` - Auto-transition after passing validation
- `ProceedDelay` - Delay before auto-proceeding

**Default Requirements:**
- `SystemRequirement` - OS and basic system info
- `GraphicsDriverRequirement` - Graphics adapter validation

## Usage Patterns

### Pattern 1: Linear Flow

```csharp
Launcher → Menu → Gameplay → Results → Menu
```

Each scene defines its next scene:
```csharp
protected override Type? GetNextSceneType()
{
    if (this is LauncherScene) return typeof(MenuScene);
    if (this is MenuScene) return typeof(GameplayScene);
    if (this is GameplayScene) return typeof(ResultsScene);
    return null; // End of flow
}
```

### Pattern 2: Conditional Flow

```csharp
protected override Type? GetNextSceneType()
{
    return _playerWon ? typeof(VictoryScene) : typeof(DefeatScene);
}
```

### Pattern 3: Menu-based Navigation

```csharp
public void OnSelectCharacter(string characterName)
{
    _selectedCharacter = characterName;
    ProceedToNext(); // Proceeds to character customization
}
```

### Pattern 4: Custom Requirements

```csharp
public partial class MyLauncher : LauncherScene
{
    protected override void RegisterRequirements()
    {
        base.RegisterRequirements();
        AddRequirement(new SaveDataRequirement());
        AddRequirement(new NetworkRequirement());
    }

    protected override void OnRequirementsFailed(List<string> failed)
    {
        // Show error UI or attempt recovery
        if (failed.Contains("Save Data"))
            CreateDefaultSaveData();
    }
}
```

## Integration with ServiceLocator

All scenes automatically receive dependency injection:

```csharp
public partial class MyScene : GameScene
{
    [Inject]
    private IAudioService _audio = null!;

    [Inject]
    private IInputService _input = null!;

    public override void _Ready()
    {
        base._Ready(); // Handles injection
        
        // Services ready to use
        _audio.PlayMusic("menu_theme");
    }
}
```

## File Structure

```
addons/ascendere/scene_manager_module/
├── README.md                    # This file
├── ENHANCEMENTS.md              # Future expansion ideas
├── SceneManager.cs              # Core autoload singleton
├── GameScene.cs                 # Abstract base class
├── LauncherScene.cs             # Requirement validation
├── LauncherScene.md             # Launcher documentation
├── ISceneManager.cs             # Service interface
└── Examples/
    ├── SceneManagement/
    │   ├── README.md            # Example usage guide
    │   ├── Scripts/
    │   │   ├── ExampleLauncher.cs
    │   │   ├── Menu.cs
    │   │   └── Game.cs
    │   └── scenes/
    │       ├── launcher.tscn
    │       ├── menu.tscn
    │       └── game.tscn
```

## Advanced Topics

### Custom Scene Transitions

Override `TransitionToNextScene()` in GameScene for custom transition effects:

```csharp
protected override string? GetScenePathForType(Type sceneType)
{
    // Custom path resolution
    return $"res://scenes/{sceneType.Name}.tscn";
}
```

### Preloading for Performance

```csharp
public override void _Ready()
{
    base._Ready();
    
    // Preload next scene in background
    _sceneManager.PreloadScene("res://scenes/gameplay.tscn");
}
```

### History Management

```csharp
// Disable history tracking for certain transitions
public bool ProceedToNextWithoutHistory()
{
    _sceneManager.ClearHistory();
    return ProceedToNext();
}

// Check history depth
if (_sceneManager.GetHistoryCount() > 0)
{
    await _sceneManager.GoBackAsync();
}
```

### Custom Requirement Checking

```csharp
public class DatabaseRequirement : ILaunchRequirement
{
    public string Name => "Database Connection";

    public async Task<RequirementResult> CheckAsync()
    {
        try
        {
            var db = new Database();
            await db.ConnectAsync();
            return RequirementResult.Success("Connected to database");
        }
        catch (Exception ex)
        {
            return RequirementResult.Failure($"Database error: {ex.Message}");
        }
    }
}
```

## Performance Considerations

1. **Scene Preloading**: Preload heavy scenes during loading screens
2. **History Limit**: Adjust `MaxHistoryCount` to prevent memory growth
3. **Reflection Overhead**: GameScene collection happens once at startup
4. **Async/Await**: Scene transitions are non-blocking by design

## Troubleshooting

### "SceneManager not injected"
- Ensure GameScene calls `ServiceLocator.InjectMembers(this)` in `_Ready()`
- Verify ServiceLocator autoload is enabled

### Scene path not found
- Check `GetScenePathForType()` returns valid paths
- Ensure .tscn files exist at specified paths
- Verify file names match type names (case-sensitive)

### History not working
- Call `GoBackAsync()` without clearing if you want to use history
- Check `GetHistoryCount()` before going back

## Examples

See `Examples/SceneManagement/` for working examples:
- Basic scene transitions with Menu/Game scenes
- Launcher with requirement validation
- Integration with dependency injection
- Signal-based event handling

Run with: Open `res://Examples/SceneManagement/scenes/launcher.tscn`

## Related Modules

- **ServiceLocator**: Dependency injection framework used by GameScene
- **EventBus** (optional): Global event distribution for complex flows

## Contributing

When adding features to this module:
1. Maintain single responsibility for each class
2. Use signals for inter-component communication
3. Update examples to demonstrate new features
4. Keep GameScene abstract and lightweight
5. Document custom requirement implementations

## Version

Current: 1.1.0

- Added LauncherScene with requirement validation
- Added GameScene base class with elegant flow control
- Added reflection-based GameScene discovery
- Enhanced SceneManager with history and preloading

## License

Part of the Ascendere framework.
