# Launcher Scene System

The Launcher scene provides a validation framework for checking game requirements before proceeding to the main game.

## Overview

LauncherScene extends GameScene and adds:
- Asynchronous requirement checking
- Automatic or manual progression
- Extensible requirement system
- Signal-based progress tracking

## Quick Start

### 1. Create a Custom Launcher

```csharp
using Ascendere.SceneManagement;

public partial class MyLauncher : LauncherScene
{
    protected override void RegisterRequirements()
    {
        base.RegisterRequirements(); // System + Graphics checks
        
        // Add custom requirements
        AddRequirement(new SaveDataRequirement());
        AddRequirement(new DLCRequirement());
    }

    protected override void OnAllRequirementsMet()
    {
        base.OnAllRequirementsMet();
        GD.Print("Ready to launch!");
        // Auto-proceeds after ProceedDelay if AutoProceed = true
    }

    protected override void OnRequirementsFailed(List<string> failed)
    {
        base.OnRequirementsFailed(failed);
        // Show error UI or attempt fixes
    }
}
```

### 2. Configure in Editor

Set these exported properties:
- `NextSceneTypeName`: Full type name of next scene (e.g., "MyGame.MenuScene")
- `AutoProceed`: Whether to automatically proceed after validation (default: true)
- `ProceedDelay`: Delay in seconds before auto-proceeding (default: 0.5s)

### 3. Create Custom Requirements

```csharp
public class DLCRequirement : ILaunchRequirement
{
    public string Name => "DLC Content";

    public async Task<RequirementResult> CheckAsync()
    {
        // Async validation
        await Task.Delay(500);
        
        if (!DirAccess.DirExistsAbsolute("user://dlc"))
            return RequirementResult.Failure("DLC not installed");
        
        return RequirementResult.Success("DLC verified");
    }
}
```

## Architecture

### LauncherScene Features

- **Requirement Pipeline**: Validates all requirements sequentially
- **Signal Events**:
  - `RequirementChecked(name, passed)` - Individual check completed
  - `AllRequirementsMet` - All checks passed
  - `RequirementsFailed(failedNames[])` - One or more checks failed
- **Validation Flow**: `_Ready()` → `RegisterRequirements()` → `CheckRequirements()` → `ProceedToNext()` or `OnRequirementsFailed()`

### Default Requirements

1. **SystemRequirement**: Validates OS detection and basic system info
2. **GraphicsDriverRequirement**: Checks for valid graphics adapter

### Progression Control

The launcher uses `CanProceedToNext()` to determine if the next scene can load:

```csharp
protected override bool CanProceedToNext()
{
    return _requirementsMet; // Set by CheckRequirements()
}
```

You can override for custom logic:

```csharp
protected override bool CanProceedToNext()
{
    return _requirementsMet && _userAcceptedTerms;
}
```

## Integration with GameScene System

LauncherScene integrates with the elegant scene navigation:

```csharp
// Set next scene type in editor or code
NextSceneTypeName = "MyGame.MainMenuScene";

// Automatically calls ProceedToNext() after requirements pass
// ProceedToNext() validates with CanProceedToNext() before transitioning
```

## Manual Progression

Disable `AutoProceed` and manually trigger:

```csharp
[Export] public bool AutoProceed { get; set; } = false;

private void OnStartButtonPressed()
{
    if (ProceedToNext())
    {
        GD.Print("Starting game...");
    }
    else
    {
        GD.PrintErr("Cannot proceed - requirements not met");
    }
}
```

## UI Integration Example

```csharp
public partial class LauncherUI : LauncherScene
{
    private Label _statusLabel;
    private ProgressBar _progress;
    private int _totalRequirements;
    private int _checkedRequirements;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("StatusLabel");
        _progress = GetNode<ProgressBar>("ProgressBar");
        
        base._Ready();
        
        RequirementChecked += OnRequirementChecked;
        AllRequirementsMet += OnAllMet;
        RequirementsFailed += OnFailed;
    }

    protected override void RegisterRequirements()
    {
        base.RegisterRequirements();
        _totalRequirements = _requirements.Count;
        _progress.MaxValue = _totalRequirements;
    }

    private void OnRequirementChecked(string name, bool passed)
    {
        _checkedRequirements++;
        _progress.Value = _checkedRequirements;
        _statusLabel.Text = $"Checking: {name}...";
    }

    private void OnAllMet()
    {
        _statusLabel.Text = "All checks passed!";
    }

    private void OnFailed(string[] failed)
    {
        _statusLabel.Text = $"Failed: {string.Join(", ", failed)}";
    }
}
```

## Best Practices

1. **Fast Checks First**: Order requirements by speed (quick checks first)
2. **Recoverable Failures**: Create missing directories/config instead of failing
3. **Progress Feedback**: Connect to signals for UI updates
4. **Graceful Degradation**: Handle optional features separately from critical requirements
5. **Async Operations**: Use async/await for I/O or network checks

## Advanced: Conditional Requirements

```csharp
protected override void RegisterRequirements()
{
    base.RegisterRequirements();
    
    // Only check multiplayer if enabled
    if (ProjectSettings.GetSetting("game/multiplayer_enabled").AsBool())
    {
        AddRequirement(new NetworkRequirement());
    }
    
    // Platform-specific requirements
    if (OS.GetName() == "Windows")
    {
        AddRequirement(new DirectXRequirement());
    }
}
```

## Example: Recoverable Requirement

```csharp
public class ConfigRequirement : ILaunchRequirement
{
    public string Name => "Configuration";

    public async Task<RequirementResult> CheckAsync()
    {
        var path = "user://config.cfg";
        
        if (!FileAccess.FileExists(path))
        {
            // Auto-fix: create default config
            var config = new ConfigFile();
            config.SetValue("game", "version", "1.0.0");
            config.Save(path);
            
            return RequirementResult.Success("Created default config");
        }
        
        // Validate existing config
        var existing = new ConfigFile();
        var err = existing.Load(path);
        
        if (err != Error.Ok)
            return RequirementResult.Failure($"Config corrupt: {err}");
        
        return RequirementResult.Success("Config valid");
    }
}
```

## Testing

```csharp
// Unit test example
[Test]
public async Task Launcher_AllRequirementsPass_ShouldSetRequirementsMet()
{
    var launcher = new TestLauncher();
    launcher.AddRequirement(new AlwaysPassRequirement());
    
    await launcher.CheckRequirements();
    
    Assert.IsTrue(launcher.CanProceedToNext());
}
```

## See Also

- [GameScene Documentation](GameScene.md)
- [SceneManager Documentation](SceneManager.md)
- [Examples/SceneManagement/](../Examples/SceneManagement/)
