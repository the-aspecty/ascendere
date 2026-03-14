# Debug Module Examples

## DebugExample.tscn

A comprehensive demonstration of all debug module features.

### Features Demonstrated

1. **3D Debug Drawing**
   - Animated line drawing
   - Rotating sphere
   - Static box
   - Arrow pointing to target
   - 3D labels
   - Path drawing (figure-8 pattern)
   - Ray casting visualization
   - Persistent shapes (5-second duration)

2. **Value Watching**
   - Time tracking
   - Frame count
   - Position monitoring
   - Target tracking

3. **Debug UI**
   - Debug console (F2)
   - Node inspector (F3)
   - Performance graph (F4)

### Controls

- **F1** - Toggle debug overlay
- **F2** - Toggle debug console
- **F3** - Toggle node inspector
- **F4** - Toggle performance graph

### Usage

1. Open `DebugExample.tscn` in Godot
2. Run the scene
3. Press F1-F4 to toggle various debug features
4. Observe the animated debug shapes and UI elements

### Code Integration Example

```csharp
using Ascendere.Debug;

public partial class MyGameObject : Node3D
{
    public override void _Process(double delta)
    {
        // Draw a line from this object to a target
        DebugManager.Instance?.DrawLine3D(
            this,
            GlobalPosition,
            targetPosition,
            Colors.Red
        );
        
        // Watch a value
        DebugManager.Instance?.WatchValue("Health", health);
    }
}
```

### Console Commands

Available in the debug console (F2):

- `help` - Show all commands
- `clear` - Clear console
- `timescale <value>` - Set time scale
- `pause` - Pause the game
- `resume` - Resume the game
- `quit` - Quit application
