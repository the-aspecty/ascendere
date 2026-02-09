# Tool Menu System

A lightweight tool menu registration system that integrates with Godot's native Tools menu.

## Features

- ✅ Uses native Godot Tools menu (AddToolMenuItem/AddToolSubmenuItem)
- ✅ Attribute-based tool discovery
- ✅ Automatic initialization in plugin lifecycle
- ✅ Memory-safe cleanup with proper callable management
- ✅ Follows Single Responsibility Principle
- ✅ Submenu/category support
- ✅ Priority-based ordering

## Architecture

```
CustomTools/
├── ToolMenuManager.cs              # Main manager (discovers and registers)
├── ToolMenuItemInfo.cs             # Tool metadata
├── ToolMenuItemAttribute.cs        # Method attribute
├── ToolMenuProviderAttribute.cs    # Class attribute
└── AscendereTools.cs               # Example tools
```

## Quick Start

### 1. Create a Tool Provider

```csharp
#if TOOLS
using Godot;

namespace YourNamespace
{
    [ToolMenuProvider]
    public static class MyTools
    {
        // Top-level tool (appears directly in Tools menu)
        [ToolMenuItem("My Cool Tool")]
        public static void MyCoolTool()
        {
            GD.Print("Executing cool tool!");
        }

        // Submenu tool (appears under "My Category" submenu)
        [ToolMenuItem(
            "Generate Something",
            Category = "My Category",
            Priority = 100,
            Tooltip = "Generates something useful"
        )]
        public static void GenerateSomething()
        {
            GD.Print("Generating...");
        }
    }
}
#endif
```

### 2. Tools Are Automatically Discovered

The `ToolMenuManager` automatically:
1. Scans all assemblies for `[ToolMenuProvider]` classes
2. Finds methods with `[ToolMenuItem]` attribute
3. Registers them with the native Tools menu

### 3. Access Tools

Open the **Tools** menu in Godot editor to see your custom tools:
- Tools without categories appear at the top level
- Tools with categories appear in submenus

## Tool Attribute Properties

```csharp
[ToolMenuItem(
    "Display Name",            // Required: shown in menu
    Category = "Submenu",      // Optional: creates submenu
    Priority = 100,            // Optional: higher = listed first
    Icon = "icon.svg",         // Optional: menu icon (not currently used)
    Tooltip = "Description"    // Optional: tooltip (not currently used)
)]
```

## Integration with Plugin

The system automatically integrates with your EditorPlugin:

```csharp
private ToolMenuManager _toolMenuManager;

public override void _EnterTree()
{
    // Initialize tool menu manager
    _toolMenuManager = new ToolMenuManager();
    _toolMenuManager.Initialize(this); // Pass EditorPlugin instance
}

public override void _ExitTree()
{
    // Cleanup
    if (_toolMenuManager != null)
    {
        _toolMenuManager.Cleanup();
        _toolMenuManager = null;
    }
}
```

## Menu Organization

### Top-Level Tools
Tools without a `Category` appear directly in the Tools menu:
```csharp
[ToolMenuItem("Quick Setup")]
public static void QuickSetup() { }
```
Result: **Tools > Quick Setup**

### Submenu Tools
Tools with a `Category` appear in submenus:
```csharp
[ToolMenuItem("Create Module", Category = "Ascendere")]
public static void CreateModule() { }

[ToolMenuItem("Generate Component", Category = "Ascendere")]
public static void GenerateComponent() { }
```
Result: 
- **Tools > Ascendere > Create Module**
- **Tools > Ascendere > Generate Component**

## Example Tools

See `AscendereTools.cs` for examples:
- Create Scene Template
- Generate Component
- Create Module Package
- Validate Project Structure
- Export Module Documentation
- Clean Build Cache
- Quick Ascendere Setup

## Best Practices

### ✅ DO

- Use `[ToolMenuProvider]` on static classes
- Keep tool methods parameterless and static
- Group related tools using categories
- Use descriptive names
- Set priority for important tools
- Use `#if TOOLS` wrapper

### ❌ DON'T

- Don't create tools with parameters (not supported)
- Don't use instance methods
- Don't forget `#if TOOLS` wrapper
- Don't perform long operations without progress feedback
- Don't create too many top-level items (use categories)

## Memory Safety

The system follows proper cleanup patterns:

```csharp
public void Cleanup()
{
    // Unregister all tools from editor
    UnregisterAllTools();
    
    // Clear internal registries
    _toolItems.Clear();
    _registeredCallables.Clear();
    
    // Release plugin reference
    _editorPlugin = null;
}
```

All callables are properly tracked and cleaned up to prevent memory leaks.

## API Reference

### ToolMenuManager

```csharp
// Initialize (called by plugin)
void Initialize(EditorPlugin editorPlugin)

// Cleanup (called by plugin)
void Cleanup()

// Query tools
IReadOnlyDictionary<string, ToolMenuItemInfo> GetTools()
ToolMenuItemInfo GetTool(string key)
```

### ToolMenuItemInfo

```csharp
class ToolMenuItemInfo
{
    string Name { get; set; }
    string Category { get; set; }
    MethodInfo Method { get; set; }
    int Priority { get; set; }
    string Icon { get; set; }
    string Tooltip { get; set; }
    bool IsSubmenu { get; set; }
}
```

## Comparison with Command Palette

| Feature | Tool Menu | Command Palette |
|---------|-----------|-----------------|
| Access | Tools menu | Ctrl+Shift+P |
| Visibility | Always in menu | Search-based |
| Organization | Hierarchical submenus | Flat categories |
| Use Case | Common operations | Quick access |
| Discoverability | High (visible in menu) | Medium (need to search) |

**Recommendation**: Use Tool Menu for frequently accessed, discoverable operations. Use Command Palette for power users and less common operations.

## Troubleshooting

**Tools not appearing?**
- Ensure class has `[ToolMenuProvider]`
- Ensure method has `[ToolMenuItem]`
- Method must be `public static`
- Check console for discovery messages

**Tool not executing?**
- Check method signature (must be parameterless)
- Check for exceptions in console
- Ensure method is public and static

**Submenu not created?**
- Ensure `Category` property is set
- Category name is case-sensitive
- Check that plugin initialized properly

**Memory leaks?**
- Tools are automatically cleaned up in `_ExitTree()`
- Callables are tracked and disposed properly
- No manual cleanup needed

## Future Enhancements

Potential improvements:
- [ ] Icon support (currently defined but not used)
- [ ] Tooltip support (currently defined but not used)
- [ ] Keyboard shortcuts for tools
- [ ] Tool enablement/disablement based on context
- [ ] Async tool support with progress dialogs
- [ ] Tool history
- [ ] Dynamic tool registration at runtime
