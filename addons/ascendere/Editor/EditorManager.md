# EditorManager Documentation

## Overview

The `EditorManager` class is a comprehensive utility for managing Godot editor dock panels within the Ascendere ecosystem. It provides a high-level, thread-safe interface for creating, adding, removing, and monitoring editor dock panels with automatic resource management and cleanup.

## Table of Contents

- [Features](#features)
- [Quick Start](#quick-start)
- [API Reference](#api-reference)
- [Events](#events)
- [Examples](#examples)
- [Thread Safety](#thread-safety)
- [Performance](#performance)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)

## Features

### Core Functionality
- ✅ **Dock Panel Management**: Create, add, remove, and query dock panels
- ✅ **Thread Safety**: All operations are thread-safe with proper locking
- ✅ **Resource Management**: Automatic cleanup and validation
- ✅ **Event System**: Real-time notifications for dock operations
- ✅ **Diagnostics**: Health checks and monitoring capabilities
- ✅ **Bulk Operations**: Efficient batch operations on multiple docks

### Safety & Reliability
- ✅ **Input Validation**: Comprehensive parameter validation
- ✅ **Error Handling**: Graceful error handling with detailed logging
- ✅ **Memory Management**: Proper resource cleanup and leak prevention
- ✅ **State Validation**: Continuous validation of dock panel states

## Quick Start

### Basic Usage

```csharp
using Ascendere.Editor;

// Create a simple dock panel
var dockPanel = EditorManager.CreateSimpleDockPanel("MyDock", "Hello World!");

// Add it to the editor
bool success = EditorManager.AddDockToEditor(dockPanel, EditorManager.DockSlot.LeftBr);

if (success)
{
    GD.Print("Dock panel added successfully!");
}

// Remove when done
EditorManager.RemoveDockFromEditor("MyDock");
```

### Advanced Usage

```csharp
// Create a custom dock with complex content
var container = new VBoxContainer();
var button = new Button { Text = "Click Me!" };
var label = new Label { Text = "Status: Ready" };

container.AddChild(button);
container.AddChild(label);

var customDock = EditorManager.CreateDockPanel("CustomDock", container);

// Subscribe to events
EditorManager.DockAdded += (name, slot) => 
    GD.Print($"Dock '{name}' added to {slot}");

EditorManager.DockOperationFailed += (name, error) => 
    GD.PrintErr($"Failed to operate on '{name}': {error}");

// Add to editor
EditorManager.AddDockToEditor(customDock, EditorManager.DockSlot.RightUl);
```

## API Reference

### Properties

#### `IsEditorAvailable`
```csharp
public static bool IsEditorAvailable { get; }
```
Gets whether the editor plugin is currently available. Returns `false` when not in editor mode.

#### `RegisteredDocksCount`
```csharp
public static int RegisteredDocksCount { get; }
```
Gets the number of currently registered dock panels.

#### `RegisteredDockNames`
```csharp
public static IEnumerable<string> RegisteredDockNames { get; }
```
Gets the names of all registered dock panels.

### Enums

#### `DockSlot`
Defines the available dock slot positions in the Godot editor:

```csharp
public enum DockSlot
{
    LeftUl = 0,    // Left Upper slot
    LeftBl = 1,    // Left Lower slot  
    LeftBr = 2,    // Left Bottom Right slot
    RightUl = 3,   // Right Upper slot
    RightBl = 4,   // Right Lower slot
    RightBr = 5,   // Right Bottom Right slot
    Bottom = 6     // Bottom slot
}
```

### Core Methods

#### `CreateDockPanel`
```csharp
public static Control CreateDockPanel(string name, Control content)
```
Creates a new dock panel with the specified name and content.

**Parameters:**
- `name`: Unique name for the dock panel (required)
- `content`: Control node to display in the dock (required)

**Returns:** The created `PanelContainer` wrapping the content

**Exceptions:**
- `ArgumentException`: When name is null or empty
- `ArgumentNullException`: When content is null
- `InvalidOperationException`: When a dock with the same name already exists

#### `CreateSimpleDockPanel`
```csharp
public static Control CreateSimpleDockPanel(string name, string text)
```
Creates a simple dock panel with a centered label for quick prototyping.

**Parameters:**
- `name`: Unique name for the dock panel
- `text`: Text to display (defaults to name if null)

**Returns:** The created dock panel with a `Label` as content

#### `AddDockToEditor`
```csharp
public static bool AddDockToEditor(Control dockPanel, DockSlot slot)
```
Adds a dock panel to the Godot editor at the specified slot.

**Parameters:**
- `dockPanel`: The dock panel to add (required)
- `slot`: The dock slot position

**Returns:** `true` if successful, `false` otherwise

**Exceptions:**
- `ArgumentNullException`: When dockPanel is null

#### `RemoveDockFromEditor`
```csharp
public static bool RemoveDockFromEditor(Control dockPanel)
public static bool RemoveDockFromEditor(string dockName)
```
Removes a dock panel from the editor by Control reference or name.

**Returns:** `true` if successful, `false` otherwise

#### `RemoveAllDocks`
```csharp
public static void RemoveAllDocks()
```
Removes all registered dock panels from the editor. Typically called during plugin cleanup.

### Query Methods

#### `GetDockPanel`
```csharp
public static Control GetDockPanel(string dockName)
```
Gets a registered dock panel by name.

**Returns:** The dock panel if found, `null` otherwise

#### `GetDockSlot`
```csharp
public static DockSlot? GetDockSlot(Control dockPanel)
```
Gets the dock slot for a registered dock panel.

**Returns:** The dock slot if registered, `null` otherwise

#### `IsDockRegistered`
```csharp
public static bool IsDockRegistered(string dockName)
```
Checks if a dock panel with the specified name is registered.

#### `GetDocksBySlot`
```csharp
public static IReadOnlyDictionary<DockSlot, List<string>> GetDocksBySlot()
```
Gets all registered dock panels grouped by their dock slots.

### Validation Methods

#### `ValidateDockPanel`
```csharp
public static bool ValidateDockPanel(Control dockPanel)
```
Validates that a dock panel is properly configured and accessible.

**Validation Checks:**
- Not null
- Valid Godot object instance
- Has a non-empty name

### Bulk Operations

#### `RemoveDocksWhere`
```csharp
public static int RemoveDocksWhere(Func<string, bool> predicate)
```
Removes all docks that match the specified predicate.

**Parameters:**
- `predicate`: Function to test dock names

**Returns:** Number of docks successfully removed

**Example:**
```csharp
// Remove all docks with names starting with "temp"
int removed = EditorManager.RemoveDocksWhere(name => name.StartsWith("temp"));
```

### Diagnostics

#### `GetDiagnosticInfo`
```csharp
public static string GetDiagnosticInfo()
```
Gets comprehensive diagnostic information about the current state.

**Returns:** Formatted string with:
- Editor availability status
- Total registered docks
- Dock slots used
- Individual dock states

#### `PerformHealthCheck`
```csharp
public static List<string> PerformHealthCheck()
```
Performs a health check on all registered dock panels.

**Returns:** List of issues found (empty if all healthy)

**Checks for:**
- Invalid Control references
- Empty dock names
- Missing slot information
- Orphaned slot references

## Events

### `DockAdded`
```csharp
public static event Action<string, DockSlot> DockAdded;
```
Fired when a dock panel is successfully added to the editor.

**Parameters:**
- `string`: Dock panel name
- `DockSlot`: The slot where it was added

### `DockRemoved`
```csharp
public static event Action<string> DockRemoved;
```
Fired when a dock panel is successfully removed from the editor.

**Parameters:**
- `string`: Dock panel name

### `DockOperationFailed`
```csharp
public static event Action<string, string> DockOperationFailed;
```
Fired when dock management operations fail.

**Parameters:**
- `string`: Dock panel name
- `string`: Error message

### Event Usage Example

```csharp
// Subscribe to all events
EditorManager.DockAdded += (name, slot) => 
    GD.Print($"✅ Dock '{name}' added to {slot}");

EditorManager.DockRemoved += (name) => 
    GD.Print($"🗑️ Dock '{name}' removed");

EditorManager.DockOperationFailed += (name, error) => 
    GD.PrintErr($"❌ Operation failed on '{name}': {error}");
```

## Examples

### Example 1: Basic Dock with Button

```csharp
using Ascendere.Editor;

public void CreateToolDock()
{
    var container = new VBoxContainer();
    
    var titleLabel = new Label 
    { 
        Text = "Meta Framework Tools",
        HorizontalAlignment = HorizontalAlignment.Center
    };
    
    var generateButton = new Button { Text = "Generate Code" };
    generateButton.Pressed += () => GD.Print("Generate clicked!");
    
    var refreshButton = new Button { Text = "Refresh" };
    refreshButton.Pressed += () => GD.Print("Refresh clicked!");
    
    container.AddChild(titleLabel);
    container.AddChild(generateButton);
    container.AddChild(refreshButton);
    
    var dock = EditorManager.CreateDockPanel("MetaTools", container);
    EditorManager.AddDockToEditor(dock, EditorManager.DockSlot.LeftUl);
}
```

### Example 2: Settings Dock with Configuration

```csharp
public void CreateSettingsDock()
{
    var form = new VBoxContainer();
    
    // Add setting controls
    var enabledCheck = new CheckBox { Text = "Enable Feature", ButtonPressed = true };
    var speedSlider = new HSlider { Min = 0, Max = 100, Value = 50 };
    var pathEdit = new LineEdit { PlaceholderText = "Enter path..." };
    
    form.AddChild(new Label { Text = "Settings" });
    form.AddChild(enabledCheck);
    form.AddChild(new Label { Text = "Speed:" });
    form.AddChild(speedSlider);
    form.AddChild(new Label { Text = "Path:" });
    form.AddChild(pathEdit);
    
    var settingsDock = EditorManager.CreateDockPanel("Settings", form);
    
    if (EditorManager.AddDockToEditor(settingsDock, EditorManager.DockSlot.RightBr))
    {
        GD.Print("Settings dock created successfully!");
    }
}
```

### Example 3: Monitoring and Cleanup

```csharp
public void MonitorDocks()
{
    // Print diagnostic information
    GD.Print(EditorManager.GetDiagnosticInfo());
    
    // Perform health check
    var issues = EditorManager.PerformHealthCheck();
    if (issues.Count > 0)
    {
        GD.PrintErr("Health check found issues:");
        foreach (var issue in issues)
        {
            GD.PrintErr($"  - {issue}");
        }
    }
    
    // Get docks by slot
    var docksBySlot = EditorManager.GetDocksBySlot();
    foreach (var kvp in docksBySlot)
    {
        GD.Print($"Slot {kvp.Key}: {string.Join(", ", kvp.Value)}");
    }
}

public void CleanupTempDocks()
{
    // Remove all temporary docks
    int removed = EditorManager.RemoveDocksWhere(name => name.StartsWith("temp_"));
    GD.Print($"Removed {removed} temporary docks");
}
```

## Thread Safety

The `EditorManager` class is fully thread-safe:

- **Locking Strategy**: Uses a private `_lock` object for synchronization
- **Read Operations**: All read operations are protected
- **Write Operations**: All write operations are atomic
- **Collections**: Internal collections are safely accessed

### Thread Safety Example

```csharp
// Safe to call from any thread
Task.Run(() => 
{
    var count = EditorManager.RegisteredDocksCount;
    var names = EditorManager.RegisteredDockNames.ToList();
    var isRegistered = EditorManager.IsDockRegistered("MyDock");
});
```

**Note:** While the class is thread-safe, Godot UI operations must still be performed on the main thread.

## Performance

### Optimization Features

- **Cached Operations**: Dock operations are cached to prevent unnecessary editor calls
- **Efficient Lookups**: Uses dictionaries for O(1) lookups
- **Lazy Evaluation**: Diagnostic information is computed on-demand
- **Bulk Operations**: Optimized batch operations for multiple docks

### Performance Tips

1. **Batch Operations**: Use `RemoveDocksWhere` for multiple removals
2. **Cache References**: Store dock panel references instead of repeated lookups
3. **Event Handling**: Avoid heavy operations in event handlers
4. **Validation**: Use `ValidateDockPanel` before operations

## Troubleshooting

### Common Issues

#### "Editor plugin not available"
**Cause:** Trying to use dock operations when not in editor mode or plugin not loaded.

**Solution:**
```csharp
if (!EditorManager.IsEditorAvailable)
{
    GD.PrintErr("Editor not available - operations will be skipped");
    return;
}
```

#### "Dock already registered"
**Cause:** Attempting to add a dock with a name that already exists.

**Solution:**
```csharp
if (EditorManager.IsDockRegistered("MyDock"))
{
    EditorManager.RemoveDockFromEditor("MyDock");
}
// Now safe to add new dock
```

#### "Invalid Control reference"
**Cause:** Dock panel Control became invalid (freed or disposed).

**Solution:**
```csharp
if (!EditorManager.ValidateDockPanel(myDock))
{
    // Recreate the dock
    myDock = EditorManager.CreateDockPanel("MyDock", content);
}
```

### Debugging

#### Enable Diagnostic Logging
```csharp
// Print diagnostic information
GD.Print(EditorManager.GetDiagnosticInfo());

// Perform health check
var issues = EditorManager.PerformHealthCheck();
foreach (var issue in issues)
{
    GD.PrintErr($"Health Check Issue: {issue}");
}
```

#### Monitor Events
```csharp
EditorManager.DockOperationFailed += (name, error) => 
{
    GD.PrintErr($"Dock operation failed: {name} - {error}");
    // Add breakpoint here for debugging
};
```

## Best Practices

### 1. Always Validate Input
```csharp
// ✅ Good
if (string.IsNullOrEmpty(dockName))
{
    GD.PrintErr("Invalid dock name");
    return;
}

// ❌ Bad
var dock = EditorManager.GetDockPanel(dockName); // Could be null
```

### 2. Handle Editor Availability
```csharp
// ✅ Good
if (EditorManager.IsEditorAvailable)
{
    EditorManager.AddDockToEditor(dock, slot);
}

// ❌ Bad
EditorManager.AddDockToEditor(dock, slot); // May fail if editor not available
```

### 3. Use Events for Feedback
```csharp
// ✅ Good
EditorManager.DockAdded += (name, slot) => 
    UpdateUI($"Dock {name} ready");

EditorManager.DockOperationFailed += (name, error) => 
    ShowErrorDialog($"Failed: {error}");
```

### 4. Clean Up Resources
```csharp
// ✅ Good - In plugin _ExitTree
public override void _ExitTree()
{
    EditorManager.RemoveAllDocks();
    base._ExitTree();
}
```

### 5. Use Descriptive Names
```csharp
// ✅ Good
EditorManager.CreateDockPanel("SceneGenerator", content);
EditorManager.CreateDockPanel("AssetBrowser", content);

// ❌ Bad
EditorManager.CreateDockPanel("dock1", content);
EditorManager.CreateDockPanel("panel", content);
```

### 6. Validate Before Operations
```csharp
// ✅ Good
if (EditorManager.ValidateDockPanel(dock))
{
    EditorManager.AddDockToEditor(dock, slot);
}

// ❌ Bad
EditorManager.AddDockToEditor(dock, slot); // May fail with invalid dock
```

### 7. Use Health Checks Periodically
```csharp
// ✅ Good - Periodic health monitoring
private Timer _healthCheckTimer;

public override void _Ready()
{
    _healthCheckTimer = new Timer();
    _healthCheckTimer.WaitTime = 30.0; // Check every 30 seconds
    _healthCheckTimer.Timeout += PerformPeriodicHealthCheck;
    AddChild(_healthCheckTimer);
    _healthCheckTimer.Start();
}

private void PerformPeriodicHealthCheck()
{
    var issues = EditorManager.PerformHealthCheck();
    if (issues.Count > 0)
    {
        GD.PrintErr($"Found {issues.Count} dock health issues");
    }
}
```

---

## Version Information

- **Version**: 1.0.0
- **Godot Version**: 4.x
- **Framework**: Ascendere
- **Language**: C#

For more information about the Ascendere, see the main documentation in the project root.
