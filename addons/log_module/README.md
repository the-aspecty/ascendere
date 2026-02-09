# Log Module

A lightweight, attribute-based logging system for Godot C# projects.

## Quick Start

```csharp
using Ascendere.Log;

// Enable logging for this class
[Log(true)]
public partial class MyGameClass : Node
{
    public override void _Ready()
    {
        // Extension method style
        this.LogInfo("Game started!");
        this.LogDebug("Loading resources...");
        this.LogWarning("Low memory!");
        this.LogError("Failed to load asset!");
    }
}

// Disable logging for this class
[Log(false)]
public partial class QuietClass : Node
{
    public override void _Ready()
    {
        // This won't output anything
        this.LogInfo("You won't see this!");
    }
}
```

## Installation

The Logger is automatically initialized by the Ascendere plugin. No manual setup required!

## Features

### Attribute-Based Logging Control

Control logging at the class level using the `[Log]` attribute:

```csharp
[Log(true)]   // Enable logging
[Log(false)]  // Disable logging
[Log]         // Enable logging (default)
// No attribute = Enabled by default
```

### Log Levels

- **Debug**: Development/diagnostic information
- **Info**: General information
- **Warning**: Warning messages (uses GD.PushWarning)
- **Error**: Error messages (uses GD.PushError)

### Extension Methods

```csharp
this.LogDebug("Debug message");
this.LogInfo("Info message");
this.LogWarning("Warning message");
this.LogError("Error message");
```

### Direct Logger Usage

```csharp
Logger.Instance.Debug(this, "Debug message");
Logger.Instance.Info(this, "Info message");
Logger.Instance.Warning(this, "Warning message");
Logger.Instance.Error(this, "Error message");
```

### Configuration

The Logger supports runtime configuration via exported properties:

```csharp
// Set minimum log level (filters out lower priority logs)
Logger.Instance.GlobalMinimumLevel = LogLevel.Warning;

// Toggle timestamps
Logger.Instance.EnableTimestamps = false;

// Toggle class names in logs
Logger.Instance.EnableTypeNames = false;

// Toggle log level display ([Debug], [Info], etc.)
Logger.Instance.EnableLogLevel = false;
```

### Service Integration

The LogService is automatically registered with the Ascendere ServiceLocator. Use it for dependency injection:

```csharp
using Ascendere.Log;

public partial class MySystem : Node
{
    private readonly ILogService _log;

    // Constructor injection
    public MySystem(ILogService logService)
    {
        _log = logService;
    }

    // Default constructor for Godot - gets from ServiceLocator
    public MySystem() : this(ServiceLocator.Get<ILogService>())
    {
    }

    public override void _Ready()
    {
        _log.Info(this, "System initialized");
        
        // Configure through service
        _log.EnableLogLevel = false;
        _log.EnableTimestamps = true;
    }
}

// Or use directly from ServiceLocator
public partial class PlayerSystem : Node
{
    private ILogService _log;

    public override void _Ready()
    {
        _log = ServiceLocator.Get<ILogService>();
        _log.Info(this, "PlayerSystem ready");
    }
}
```

## Output Format

By default, logs are formatted as:

```
[HH:mm:ss.fff] [LogLevel] [ClassName] Message
```

You can customize the format by toggling options:

```csharp
// Default: [14:23:45.123] [Info] [PlayerController] Player spawned
Logger.Instance.EnableTimestamps = true;
Logger.Instance.EnableLogLevel = true;
Logger.Instance.EnableTypeNames = true;

// No log level: [14:23:45.123] [PlayerController] Player spawned
Logger.Instance.EnableLogLevel = false;

// No timestamps: [Info] [PlayerController] Player spawned
Logger.Instance.EnableTimestamps = false;
Logger.Instance.EnableLogLevel = true;

// Only message: Player spawned
Logger.Instance.EnableTimestamps = false;
Logger.Instance.EnableLogLevel = false;
Logger.Instance.EnableTypeNames = false;
```

Example:
```
[14:23:45.123] [Info] [PlayerController] Player spawned at position (0, 10, 0)
[14:23:45.456] [Warning] [HealthSystem] Health below 25%
[14:23:45.789] [Error] [SaveManager] Failed to save game data
```

## Advanced Usage

### Check if Logging is Enabled

```csharp
if (this.IsLoggingEnabled())
{
    // Perform expensive logging operation
    var detailedState = GenerateDetailedStateReport();
    this.LogDebug(detailedState);
}
```

### Runtime Override

You can override the `[Log]` attribute at runtime:

```csharp
// Disable logging for this class instance at runtime
this.SetLoggingOverride(false);
this.LogInfo("This won't log");

// Re-enable logging
this.SetLoggingOverride(true);
this.LogInfo("This will log now");

// Remove override (revert to attribute behavior)
this.RemoveLoggingOverride();

// Or use the Logger directly for type-based overrides
Logger.Instance.SetLoggingOverride(typeof(MyClass), false);
Logger.Instance.RemoveLoggingOverride(typeof(MyClass));
```

### Clear Cache

If you modify attributes at runtime (unlikely scenario), clear the cache:

```csharp
Logger.Instance.ClearCache();
```

## Performance

- Uses reflection caching to avoid repeated attribute lookups
- Checks logging state before formatting messages
- Minimal overhead for disabled loggers (early return)
- Supports null sources (logs by default)

## Best Practices

1. **Use [Log(false)] for performance-critical classes** that are called frequently
2. **Keep log messages concise** and informative
3. **Use appropriate log levels**:
   - Debug: Temporary development info
   - Info: Important state changes
   - Warning: Recoverable issues
   - Error: Critical failures
4. **Check IsLoggingEnabled()** before expensive string operations
5. **Set GlobalMinimumLevel** higher in production builds

## Integration with Other Modules

The log module is standalone but can be easily integrated with:
- Service locator pattern
- Event systems
- Debug overlays
- Custom logging backends

## Troubleshooting

**Logs not appearing?**
- Check if the class has `[Log(false)]`
- Verify Logger is added as autoload
- Check `GlobalMinimumLevel` setting

**Performance concerns?**
- Disable logging for hot-path classes using `[Log(false)]`
- Increase `GlobalMinimumLevel` to filter debug logs
- Use `IsLoggingEnabled()` checks before expensive operations

## API Reference

### LogAttribute
```csharp
[Log(true)]  // Enable
[Log(false)] // Disable
```

### ILogService Interface
```csharp
void Debug(object source, string message)
void Info(object source, string message)
void Warning(object source, string message)
void Error(object source, string message)
void Log(object source, LogLevel level, string message)
bool IsLoggingEnabled(object source)
void SetLoggingOverride(Type type, bool enabled)
void RemoveLoggingOverride(Type type)
void ClearCache()
LogLevel GlobalMinimumLevel { get; set; }
bool EnableTimestamps { get; set; }
bool EnableTypeNames { get; set; }
bool EnableLogLevel { get; set; }
```

### ILogger Interface
```csharp
void Debug(object source, string message)
void Info(object source, string message)
void Warning(object source, string message)
void Error(object source, string message)
void Log(object source, LogLevel level, string message)
bool IsLoggingEnabled(object source)
```

### Logger Properties
```csharp
LogLevel GlobalMinimumLevel { get; set; }
bool EnableTimestamps { get; set; }
bool EnableTypeNames { get; set; }
bool EnableLogLevel { get; set; }
```

### Logger Methods
```csharp
void SetLoggingOverride(Type type, bool enabled)
void SetLoggingOverride(object source, bool enabled)
void RemoveLoggingOverride(Type type)
void RemoveLoggingOverride(object source)
void ClearCache()
```

### Extension Methods
```csharp
void LogDebug(this object source, string message)
void LogInfo(this object source, string message)
void LogWarning(this object source, string message)
void LogError(this object source, string message)
bool IsLoggingEnabled(this object source)
void SetLoggingOverride(this object source, bool enabled)
void RemoveLoggingOverride(this object source)
```

---

**Version**: 1.0.0  
**Author**: Aspecty  
**License**: MIT
