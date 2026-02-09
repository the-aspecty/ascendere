# Command Palette System

A lightweight command registration system that integrates with Godot's native editor command palette.

## Features

- ✅ Uses native Godot editor command palette (no custom UI)
- ✅ Attribute-based command discovery
- ✅ Automatic initialization in plugin lifecycle
- ✅ Memory-safe cleanup
- ✅ Follows Single Responsibility Principle (each class in its own file)
- ✅ Category organization
- ✅ Priority-based ordering

## Architecture

```
CustomCommands/
├── CommandPaletteManager.cs         # Main manager (discovers and registers)
├── CommandInfo.cs                   # Command metadata
├── EditorCommandAttribute.cs        # Method attribute
├── EditorCommandProviderAttribute.cs # Class attribute
└── AscendereCommands.cs             # Example commands
```

## Quick Start

### 1. Create a Command Provider

```csharp
#if TOOLS
using Godot;

namespace YourNamespace
{
    [EditorCommandProvider]
    public static class MyCommands
    {
        [EditorCommand(
            "Do Something Cool",
            Description = "Performs a cool action",
            Category = "Tools",
            Priority = 100
        )]
        public static void DoSomethingCool()
        {
            GD.Print("Doing something cool!");
            // Your implementation
        }
    }
}
#endif
```

### 2. Commands Are Automatically Discovered

No manual registration needed! The `CommandPaletteManager` automatically:
1. Scans all assemblies for `[EditorCommandProvider]` classes
2. Finds methods with `[EditorCommand]` attribute
3. Registers them with the native editor palette

### 3. Access Commands

Press **Ctrl+Shift+P** (or **Cmd+Shift+P** on Mac) to open the command palette, then:
- Type "Do Something Cool" to find your command
- Commands are organized by category: `ascendere/<category>/<command_id>`

## Command Attribute Properties

```csharp
[EditorCommand(
    "Display Name",              // Required: shown in palette
    Description = "Details...",  // Optional: command description
    Category = "Tools",          // Optional: for organization
    Priority = 100               // Optional: higher = listed first
)]
```

## Integration with Plugin

The system automatically integrates with `AscenderePlugin`:

```csharp
// In _EnterTree()
_commandPaletteManager = new CommandPaletteManager();
_commandPaletteManager.Initialize(_commandPalette);

// In _ExitTree()
_commandPaletteManager.Cleanup();
```

## Command Organization

Commands are registered with keys like:
- `ascendere/<command_id>` (no category)
- `ascendere/<category>/<command_id>` (with category)

Example:
- `ascendere/module/create_new_module`
- `ascendere/tools/generate_registry`
- `ascendere/settings/open_ascendere_settings`

## Example Commands

See `AscendereCommands.cs` for examples:
- Create New Module
- Refresh Module List
- Open Ascendere Settings
- Generate Registry
- Clear Cache

## Best Practices

### ✅ DO

- Use `[EditorCommandProvider]` on static classes
- Keep command methods parameterless and static
- Organize commands by category
- Use descriptive names
- Add helpful descriptions
- Set priority for important commands

### ❌ DON'T

- Don't create commands with parameters (not supported)
- Don't use instance methods
- Don't forget `#if TOOLS` wrapper
- Don't perform long-running operations without feedback

## Memory Safety

The system follows proper cleanup patterns:

```csharp
public void Cleanup()
{
    // Unregister all commands from editor palette
    UnregisterAllCommands();
    
    // Clear internal registry
    _commands.Clear();
    
    // Release editor palette reference
    _editorPalette = null;
}
```

## API Reference

### CommandPaletteManager

```csharp
// Initialize (called by plugin)
void Initialize(EditorCommandPalette editorPalette)

// Cleanup (called by plugin)
void Cleanup()

// Query commands
IReadOnlyDictionary<string, CommandInfo> GetCommands()
CommandInfo GetCommand(string id)

// Manual registration (usually not needed)
bool RegisterCommand(CommandInfo command)
```

### CommandInfo

```csharp
class CommandInfo
{
    string Id { get; set; }
    string Name { get; set; }
    string Description { get; set; }
    string Category { get; set; }
    string Shortcut { get; set; }
    int Priority { get; set; }
    MethodInfo Method { get; set; }
}
```

## Troubleshooting

**Commands not appearing?**
- Ensure class has `[EditorCommandProvider]`
- Ensure method has `[EditorCommand]`
- Method must be `public static`
- Check console for discovery messages

**Command not executing?**
- Check method signature (must be parameterless)
- Check for exceptions in console
- Ensure method is public and static

**Memory leaks?**
- Commands are automatically cleaned up in plugin `_ExitTree()`
- No manual cleanup needed

## Future Enhancements

Potential improvements:
- [ ] Command shortcuts
- [ ] Command history
- [ ] Command aliases
- [ ] Async command support
- [ ] Command context (selection, current scene, etc.)
- [ ] Command validation/enablement
