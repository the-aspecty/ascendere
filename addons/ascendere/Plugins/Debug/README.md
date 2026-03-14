# Master Debug Addon for Godot C#

A comprehensive, production-ready debugging system for Godot C# that provides runtime debugging capabilities completely decoupled from your game code.

## Features

- 🎨 **3D Debug Drawing** - Lines, spheres, boxes, arrows, labels, paths, and rays
- 👁️ **Value Watching** - Real-time monitoring of variables
- 🪟 **Custom Debug Windows** - Create draggable UI panels with controls
- 📊 **Performance Monitoring** - FPS graphs and memory tracking
- 🔍 **Node Inspector** - Runtime inspection of any node's properties
- 💬 **Debug Console** - Command execution and log viewing
- ⌨️ **Keyboard Shortcuts** - Quick access to all features
- 🎯 **Decoupled Architecture** - Zero impact on production code

---

## Installation

### 1. Add to Your Project

Place `DebugManager.cs` in your Godot project (e.g., `res://addons/debug/`).

### 2. Setup as AutoLoad

1. Go to **Project → Project Settings → Autoload**
2. Add the script with name: `DebugManager`
3. Enable it

### 3. Start Using

The debug system is now accessible from anywhere via `DebugManager.Instance`.

---

## Quick Start

```csharp
public partial class Player : CharacterBody3D
{
    public override void _Process(double delta)
    {
        // Draw velocity arrow
        DebugManager.Instance.DrawArrow3D(
            this, 
            GlobalPosition, 
            GlobalPosition + Velocity,
            Colors.Red
        );
        
        // Watch values
        DebugManager.Instance.Watch("Speed", Velocity.Length());
        DebugManager.Instance.Watch("Health", _health);
    }
}
```

---

## Keyboard Shortcuts

| Key | Function |
|-----|----------|
| `F1` | Toggle entire debug system |
| `~` (Tilde) | Toggle debug console |
| `F2` | Toggle node inspector |
| `F3` | Toggle performance monitor |

> You can customize these keybindings by editing the `[Export]` properties in `DebugManager.cs`

---

## Core Features

### 3D Debug Drawing

All drawing functions are called on `DebugManager.Instance` and require the calling `Node` as the first parameter. This keeps tracking separate and allows automatic cleanup when nodes are freed.

#### DrawLine3D
```csharp
DebugManager.Instance.DrawLine3D(
    node: this,
    from: Vector3.Zero,
    to: new Vector3(5, 0, 0),
    color: Colors.Red,
    duration: 2f,      // Optional: 0 = permanent
    thickness: 3f      // Optional: line width
);
```

#### DrawSphere3D
```csharp
DebugManager.Instance.DrawSphere3D(
    node: this,
    position: GlobalPosition,
    radius: 2.5f,
    color: Colors.Blue,
    duration: 0f
);
```

#### DrawBox3D
```csharp
DebugManager.Instance.DrawBox3D(
    node: this,
    position: targetPosition,
    size: new Vector3(2, 2, 2),
    color: Colors.Yellow,
    duration: 1f
);
```

#### DrawArrow3D
Perfect for showing directions (velocity, forces, etc.):
```csharp
DebugManager.Instance.DrawArrow3D(
    node: this,
    from: GlobalPosition,
    to: GlobalPosition + velocity,
    color: Colors.Green,
    duration: 0.1f
);
```

#### DrawLabel3D
Display text in 3D space:
```csharp
DebugManager.Instance.DrawLabel3D(
    node: this,
    position: GlobalPosition + Vector3.Up * 2,
    text: $"Health: {health}",
    color: Colors.White,
    duration: 0f
);
```

#### DrawPath3D
Visualize paths and trajectories:
```csharp
Vector3[] pathPoints = { point1, point2, point3, point4 };
DebugManager.Instance.DrawPath3D(
    node: this,
    points: pathPoints,
    color: Colors.Cyan,
    duration: 5f,
    closed: true  // Connect last point to first
);
```

#### DrawRay3D
Ray visualization with direction indicator:
```csharp
DebugManager.Instance.DrawRay3D(
    node: this,
    origin: rayOrigin,
    direction: rayDirection,
    length: 10f,
    color: Colors.Magenta,
    duration: 0.5f
);
```

#### Clear Draw Commands
```csharp
// Clear specific node's drawings
DebugManager.Instance.ClearDrawCommands(this);

// Clear ALL drawings
DebugManager.Instance.ClearAllDrawCommands();
```

---

### Value Watching

Monitor any value in real-time with automatic updates.

```csharp
// Watch values (updates automatically)
DebugManager.Instance.Watch("Player Speed", velocity.Length());
DebugManager.Instance.Watch("Current HP", health);
DebugManager.Instance.Watch("Enemy Count", enemies.Count);
DebugManager.Instance.Watch("Score", GameManager.Score);

// Remove specific watch
DebugManager.Instance.Unwatch("Player Speed");

// Clear all watches
DebugManager.Instance.ClearWatches();

// Show the watch window
DebugManager.Instance.ShowWatchWindow();
```

The watch window updates automatically every 50ms and displays all watched values sorted alphabetically.

---

### Custom Debug Windows

Create your own debug UI panels with various controls.

#### Basic Window Creation
```csharp
var window = DebugManager.Instance.CreateWindow(
    title: "Player Debug",
    position: new Vector2(10, 10),
    size: new Vector2(300, 250)
);
```

#### Adding Controls

**Labels:**
```csharp
window.AddLabel("=== Player Stats ===", Colors.Cyan);
window.AddLabel($"Position: {GlobalPosition}");
window.AddLabel($"Health: {health}/{maxHealth}", Colors.Green);
```

**Buttons:**
```csharp
window.AddButton("Heal to Full", () => {
    health = maxHealth;
    DebugManager.Instance.Log("Player healed!");
});

window.AddButton("Reset Position", () => {
    GlobalPosition = Vector3.Zero;
});
```

**Sliders:**
```csharp
window.AddSlider(
    label: "Speed Multiplier",
    min: 0.1f,
    max: 5f,
    value: 1f,
    callback: (value) => speedMultiplier = value
);

window.AddSlider("Gravity", -20f, 0f, -9.8f, (v) => {
    ProjectSettings.SetSetting("physics/3d/default_gravity", v);
});
```

**Checkboxes:**
```csharp
window.AddCheckbox("God Mode", false, (enabled) => {
    godMode = enabled;
    DebugManager.Instance.Log($"God mode: {enabled}");
});

window.AddCheckbox("Show Hitboxes", false, (enabled) => {
    showHitboxes = enabled;
});
```

**Separators:**
```csharp
window.AddSeparator();  // Visual divider
```

#### Window Management

```csharp
// Get existing window
var window = DebugManager.Instance.GetWindow("Player Debug");

// Clear all controls from window
window.Clear();

// Remove window completely
DebugManager.Instance.RemoveWindow("Player Debug");
```

#### Example: Complete Debug Panel
```csharp
public override void _Ready()
{
    var window = DebugManager.Instance.CreateWindow(
        "Game Controls", 
        new Vector2(10, 10), 
        new Vector2(320, 400)
    );
    
    window.AddLabel("=== Time Controls ===", Colors.Yellow);
    window.AddSlider("Time Scale", 0f, 3f, 1f, (v) => Engine.TimeScale = v);
    window.AddButton("Pause", () => GetTree().Paused = true);
    window.AddButton("Resume", () => GetTree().Paused = false);
    
    window.AddSeparator();
    
    window.AddLabel("=== Player Cheats ===", Colors.Yellow);
    window.AddCheckbox("Invincible", false, (v) => invincible = v);
    window.AddCheckbox("Infinite Ammo", false, (v) => infiniteAmmo = v);
    window.AddButton("Add 1000 Gold", () => gold += 1000);
    
    window.AddSeparator();
    
    window.AddLabel("=== Debug Options ===", Colors.Yellow);
    window.AddCheckbox("Show FPS", true, (v) => showFps = v);
    window.AddCheckbox("Wireframe", false, (v) => ToggleWireframe(v));
}
```

---

### Debug Console

A command-line interface for runtime debugging.

#### Built-in Commands

| Command | Description |
|---------|-------------|
| `help` | Show available commands |
| `clear` | Clear console output |
| `timescale <value>` | Set game time scale (e.g., `timescale 0.5`) |
| `pause` | Pause the game |
| `resume` | Resume the game |
| `quit` | Exit the game |

#### Logging to Console

```csharp
// Standard log
DebugManager.Instance.Log("Enemy spawned", Colors.Green);

// Warning
DebugManager.Instance.LogWarning("Low health!");

// Error
DebugManager.Instance.LogError("Failed to load save file");
```

All logs are timestamped and color-coded in the console.

---

### Node Inspector

Runtime inspection of any node's properties and transforms.

```csharp
// Inspect any node
DebugManager.Instance.InspectNode(player);
DebugManager.Instance.InspectNode(enemy);

// Or toggle with F2 and click nodes
```

The inspector shows:
- Node type and scene path
- Transform data (for Node3D)
- All public properties with current values
- Auto-refreshes 5 times per second

---

### Performance Monitor

Real-time performance metrics with visual graphs.

**Displays:**
- Current and average FPS
- Memory usage in MB
- Active object count
- 120-sample FPS history graph

**Color Coding:**
- 🟢 Green: 55+ FPS (good)
- 🟡 Yellow: 30-54 FPS (okay)
- 🔴 Red: <30 FPS (poor)

Toggle with `F3` or:
```csharp
DebugManager.Instance.TogglePerformance();
```

---

## Advanced Usage

### Example: AI Debug Visualization

```csharp
public partial class Enemy : CharacterBody3D
{
    private Vector3 _targetPosition;
    private Vector3[] _pathPoints;
    
    public override void _Process(double delta)
    {
        // Draw vision cone
        DebugManager.Instance.DrawRay3D(
            this, 
            GlobalPosition, 
            -GlobalTransform.Basis.Z, 
            visionRange, 
            Colors.Yellow
        );
        
        // Draw target
        if (_targetPosition != Vector3.Zero)
        {
            DebugManager.Instance.DrawSphere3D(
                this, 
                _targetPosition, 
                0.5f, 
                Colors.Red
            );
            
            DebugManager.Instance.DrawArrow3D(
                this,
                GlobalPosition,
                _targetPosition,
                Colors.Orange
            );
        }
        
        // Draw pathfinding
        if (_pathPoints != null && _pathPoints.Length > 0)
        {
            DebugManager.Instance.DrawPath3D(
                this,
                _pathPoints,
                Colors.Cyan,
                0f,
                false
            );
        }
        
        // Watch state
        DebugManager.Instance.Watch($"Enemy_{Name}_State", currentState);
        DebugManager.Instance.Watch($"Enemy_{Name}_Target", 
            _targetPosition != Vector3.Zero ? "Active" : "None");
    }
}
```

### Example: Physics Debug

```csharp
public partial class PhysicsBody : RigidBody3D
{
    public override void _PhysicsProcess(double delta)
    {
        // Visualize velocity
        DebugManager.Instance.DrawArrow3D(
            this,
            GlobalPosition,
            GlobalPosition + LinearVelocity,
            Colors.Green,
            0.1f
        );
        
        // Visualize angular velocity
        var angVelViz = GlobalPosition + AngularVelocity.Normalized() * 2;
        DebugManager.Instance.DrawArrow3D(
            this,
            GlobalPosition,
            angVelViz,
            Colors.Magenta,
            0.1f
        );
        
        // Draw collision shape bounds
        if (GetChild(0) is CollisionShape3D collShape)
        {
            if (collShape.Shape is BoxShape3D box)
            {
                DebugManager.Instance.DrawBox3D(
                    this,
                    GlobalPosition,
                    box.Size,
                    Colors.Red,
                    0f
                );
            }
        }
        
        DebugManager.Instance.Watch("Linear Velocity", LinearVelocity.Length());
        DebugManager.Instance.Watch("Angular Velocity", AngularVelocity.Length());
    }
}
```

### Example: Custom Debug Panel with Live Updates

```csharp
public partial class GameManager : Node
{
    private DebugWindow _statsWindow;
    
    public override void _Ready()
    {
        _statsWindow = DebugManager.Instance.CreateWindow(
            "Live Stats",
            new Vector2(GetViewport().GetVisibleRect().Size.X - 310, 220),
            new Vector2(300, 350)
        );
        
        UpdateStatsWindow();
    }
    
    private void UpdateStatsWindow()
    {
        _statsWindow.Clear();
        
        _statsWindow.AddLabel("=== Game State ===", Colors.Cyan);
        _statsWindow.AddLabel($"Enemies: {GetTree().GetNodesInGroup("enemies").Count}");
        _statsWindow.AddLabel($"Players: {GetTree().GetNodesInGroup("players").Count}");
        _statsWindow.AddLabel($"Score: {score}");
        _statsWindow.AddLabel($"Wave: {currentWave}");
        
        _statsWindow.AddSeparator();
        
        _statsWindow.AddLabel("=== Quick Actions ===", Colors.Yellow);
        _statsWindow.AddButton("Spawn Wave", () => SpawnWave());
        _statsWindow.AddButton("Clear Enemies", () => ClearEnemies());
        _statsWindow.AddButton("Next Level", () => LoadNextLevel());
        
        _statsWindow.AddSeparator();
        
        _statsWindow.AddLabel("=== Difficulty ===", Colors.Yellow);
        _statsWindow.AddSlider("Enemy Damage", 0.5f, 3f, 1f, (v) => enemyDamageMult = v);
        _statsWindow.AddSlider("Spawn Rate", 0.5f, 2f, 1f, (v) => spawnRateMult = v);
        
        GetTree().CreateTimer(0.2).Timeout += UpdateStatsWindow;
    }
}
```

---

## Utility Functions

### Breakpoint
Pause execution and log:
```csharp
DebugManager.Instance.Breakpoint("Check player state here");
```

### Time Scale Control
```csharp
DebugManager.Instance.TimeScale(0.5f);  // Slow motion
DebugManager.Instance.TimeScale(2f);    // Fast forward
DebugManager.Instance.TimeScale(1f);    // Normal speed
```

### Toggle System
```csharp
// Disable entire debug system
DebugManager.Instance.ToggleDebug();
```

---

## Best Practices

### 1. Use Duration Parameter
For temporary visualizations, use the duration parameter to avoid clutter:
```csharp
// Auto-removes after 0.5 seconds
DebugManager.Instance.DrawArrow3D(this, from, to, Colors.Red, 0.5f);
```

### 2. Conditional Compilation
For production builds:
```csharp
#if DEBUG
DebugManager.Instance.DrawBox3D(this, pos, size, Colors.Blue);
#endif
```

### 3. Organized Windows
Create separate windows for different systems:
```csharp
CreateWindow("Player Debug", ...);
CreateWindow("Enemy AI", ...);
CreateWindow("Physics", ...);
```

### 4. Meaningful Keys
Use descriptive watch keys:
```csharp
// Good
DebugManager.Instance.Watch("Player/Health", health);
DebugManager.Instance.Watch("Player/Stamina", stamina);

// Avoid
DebugManager.Instance.Watch("h", health);
```

### 5. Clean Up
Clear draws when changing states:
```csharp
public void OnStateChange()
{
    DebugManager.Instance.ClearDrawCommands(this);
}
```

---

## Performance Considerations

- **Drawing**: All 3D draws are 2D screen-space projections (minimal overhead)
- **Watches**: Update at 20 FPS (50ms intervals)
- **Auto-cleanup**: Invalid nodes are automatically removed
- **Conditional**: Use `#if DEBUG` for zero production impact

---

## Customization

### Change Keybindings
Edit the exports at the top of `DebugManager`:
```csharp
[Export] public Key ToggleKey = Key.F1;
[Export] public Key ConsoleKey = Key.Quoteleft;
[Export] public Key InspectorKey = Key.F2;
[Export] public Key PerformanceKey = Key.F3;
```

### Extend Console Commands
Add custom commands in `ExecuteCommand()`:
```csharp
case "spawn":
    if (parts.Length > 1)
    {
        SpawnEnemy(parts[1]);
        AddLog(new LogEntry($"Spawned {parts[1]}", Colors.Green));
    }
    break;
```

### Custom Window Styles
Modify styles in constructors:
```csharp
var style = new StyleBoxFlat();
style.BgColor = new Color(0.2f, 0.1f, 0.1f, 0.95f); // Red tint
style.BorderColor = Colors.Red;
window.AddThemeStyleboxOverride("panel", style);
```

---

## Troubleshooting

**Debug system not appearing?**
- Ensure AutoLoad is properly configured
- Check that `DebugManager.Instance` is not null
- Press F1 to toggle visibility

**3D draws not showing?**
- Verify a Camera3D exists in the scene
- Check that the node reference is valid
- Ensure draws aren't behind camera

**Performance issues?**
- Use duration parameter for temporary draws
- Clear old draws with `ClearDrawCommands()`
- Reduce watch update frequency if needed

