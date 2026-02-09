# SceneManagement Example

This example demonstrates the SceneManager system including:
- Basic scene transitions and history
- Dependency injection with `[Inject]` attribute
- Elegant scene flow with GameScene base class
- Launcher system with requirement validation

## Setup

1. Ensure the `SceneManager` autoload is configured in Project Settings → Autoload:
   - Name: `SceneManager`
   - Path: `res://addons/ascendere/scene_manager_module/SceneManager.cs`
   - Enable: ✓

2. Ensure the ServiceLocator is configured for dependency injection to work.

## Examples Included

### 1. Basic Scene Transitions (Menu ↔ Game)

**Run**: Open `res://Examples/SceneManagement/scenes/menu.tscn`

- Menu scene uses `[Inject] ISceneManager` to get scene management service
- Press `Start Game` to transition to game scene
- Game scene uses history to go back to menu
- Demonstrates signal subscriptions and async/await patterns

**Files**:
- `scenes/menu.tscn` and `Scripts/Menu.cs`
- `scenes/game.tscn` and `Scripts/Game.cs`

### 2. Launcher with Requirement Validation

**Run**: Open `res://Examples/SceneManagement/scenes/launcher.tscn`

- Validates system requirements before proceeding
- Checks graphics driver, save data, configuration
- Auto-proceeds to next scene after validation
- Shows progress feedback during checks

**Features**:
- Asynchronous requirement checking
- Default requirements (system, graphics)
- Custom requirements (save data, config)
- Auto-creation of missing resources
- Signal-based progress tracking

**Files**:
- `scenes/launcher.tscn` and `Scripts/ExampleLauncher.cs`

## Key Concepts

### Dependency Injection

Both Menu and Game scenes use the `[Inject]` attribute:

```csharp
[Inject] private ISceneManager _sceneManager;
```

This is resolved automatically by the ServiceLocator when scenes are instantiated.

### Elegant Scene Flow

GameScene provides a clean navigation API:

```csharp
protected override Type? GetNextSceneType()
{
    return typeof(MenuScene); // Define scene flow
}

public bool ProceedToNext()
{
    // Validates with CanProceedToNext() then transitions
}
```

### LauncherScene Validation

Extend LauncherScene to add custom requirements:

```csharp
public partial class MyLauncher : LauncherScene
{
    protected override void RegisterRequirements()
    {
        base.RegisterRequirements();
        AddRequirement(new MyCustomRequirement());
    }
}
```

## Notes

- Scene changes use `await ChangeSceneAsync(...)` for proper async handling
- The menu subscribes to `SceneChanged` signal for status updates
- History is managed automatically by SceneManager
- LauncherScene validates requirements before allowing progression
- All scenes benefit from reflection-based GameScene collection in SceneManager

## Documentation

See also:
- [LauncherScene.md](../../addons/ascendere/scene_manager_module/LauncherScene.md) - Detailed launcher documentation
- [GameScene source](../../addons/ascendere/scene_manager_module/GameScene.cs) - Base class implementation

