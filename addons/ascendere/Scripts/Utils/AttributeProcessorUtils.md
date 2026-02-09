# AttributeProcessorUtils Documentation

## Overview

`AttributeProcessorUtils` is a comprehensive reflection-based utility class for discovering and processing methods, fields, and properties decorated with custom attributes. It enables automatic discovery and registration of attributed members across assemblies, supporting both static and instance members with optional asynchronous processing.

**Namespace:** `Ascendere.Utils`  
**Type:** Static Utility Class  
**Primary Use:** Plugin discovery, event registration, dependency injection, and attribute-driven workflows

---

## Table of Contents

1. [Core Concepts](#core-concepts)
2. [Method Reference](#method-reference)
3. [Delegate Types](#delegate-types)
4. [Usage Examples](#usage-examples)
5. [Common Patterns](#common-patterns)
6. [Best Practices](#best-practices)
7. [Integration Guide](#integration-guide)
8. [Troubleshooting](#troubleshooting)

---

## Core Concepts

### Attribute-Driven Discovery

The utility scans assemblies to find types, methods, fields, and properties decorated with a specified custom attribute. This enables a declarative, attribute-based workflow where developers mark code for automatic processing without explicit registration.

### Assembly Scanning

- **Default Assembly:** If no assembly is specified, the executing assembly is scanned
- **Type Discovery:** Searches for types containing members with the target attribute
- **Instantiation:** Automatically instantiates types with parameterless constructors
- **Static Support:** Handles static members without requiring instantiation

### Handler Invocation

Discovered attributes are processed via handler delegates, providing:
- The reflected member (MethodInfo, FieldInfo, PropertyInfo)
- The instance (null for static members)
- The attribute instance with all metadata

---

## Method Reference

### Method Scanning Methods

#### `ScanAndRegisterAttributes<TAttribute>`

**Signature:**
```csharp
public static void ScanAndRegisterAttributes<TAttribute>(
    Assembly assembly,
    AttributeHandler<TAttribute> handler
)
where TAttribute : Attribute
```

**Description:**
Scans an assembly for methods decorated with `TAttribute` and invokes the handler for each discovered method.

**Parameters:**
- `assembly` - Assembly to scan; uses executing assembly if null
- `handler` - Delegate invoked for each attributed method

**Behavior:**
1. Discovers all types containing methods with `TAttribute`
2. Processes static methods with null instance
3. Instantiates types with parameterless constructors
4. Processes instance methods with their instances

**Example:**
```csharp
[AttributeUsage(AttributeTargets.Method)]
public class InitializerAttribute : Attribute { }

public class ServiceInitializer
{
    [Initializer]
    public void OnApplicationStart()
    {
        GD.Print("Service initialized");
    }
}

// Discover and register
AttributeProcessorUtils.ScanAndRegisterAttributes<InitializerAttribute>(
    null,
    (method, instance, attr) =>
    {
        method.Invoke(instance, null);
    }
);
```

---

#### `RegisterAttributes<TAttribute>`

**Signature:**
```csharp
public static void RegisterAttributes<TAttribute>(
    object instance,
    Type type,
    AttributeHandler<TAttribute> handler
)
where TAttribute : Attribute
```

**Description:**
Registers methods with `TAttribute` for a specific type or instance (targeted scanning).

**Parameters:**
- `instance` - Object instance; null for static-only scanning
- `type` - Type to scan; uses instance type if null
- `handler` - Delegate for each attributed method

**Use Case:** When you want to process attributes on a specific type without scanning entire assembly.

**Example:**
```csharp
var myService = new ServiceInitializer();
AttributeProcessorUtils.RegisterAttributes<InitializerAttribute>(
    myService,
    typeof(ServiceInitializer),
    (method, instance, attr) =>
    {
        method.Invoke(instance, null);
    }
);
```

---

### Field Scanning Methods

#### `ScanAndRegisterFieldAttributes<TAttribute>`

**Signature:**
```csharp
public static void ScanAndRegisterFieldAttributes<TAttribute>(
    Assembly assembly,
    FieldAttributeHandler<TAttribute> handler
)
where TAttribute : Attribute
```

**Description:**
Scans an assembly for fields decorated with `TAttribute` and invokes the handler for each.

**Parameters:**
- `assembly` - Assembly to scan; uses executing assembly if null
- `handler` - Delegate invoked for each attributed field

**Use Case:** Dependency injection, configuration initialization, or automatic wiring.

**Example:**
```csharp
[AttributeUsage(AttributeTargets.Field)]
public class InjectAttribute : Attribute { }

public class ServiceConsumer
{
    [Inject]
    public IDataService DataService;
}

// Auto-wire dependencies
AttributeProcessorUtils.ScanAndRegisterFieldAttributes<InjectAttribute>(
    null,
    (field, instance, attr) =>
    {
        var service = ServiceLocator.GetService(field.FieldType);
        field.SetValue(instance, service);
    }
);
```

---

#### `RegisterFieldAttributes<TAttribute>`

**Signature:**
```csharp
public static void RegisterFieldAttributes<TAttribute>(
    object instance,
    Type type,
    FieldAttributeHandler<TAttribute> handler
)
where TAttribute : Attribute
```

**Description:**
Registers fields with `TAttribute` for a specific type or instance (targeted scanning).

---

#### `ScanAndRegisterFieldAttributesAsync<TAttribute>`

**Signature:**
```csharp
public static async Task ScanAndRegisterFieldAttributesAsync<TAttribute>(
    Assembly assembly,
    FieldAttributeHandlerAsync<TAttribute> handler
)
where TAttribute : Attribute
```

**Description:**
Asynchronously scans an assembly for attributed fields and invokes an async handler for each.

**Use Case:** Asynchronous initialization (loading files, network requests, database operations).

**Example:**
```csharp
[AttributeUsage(AttributeTargets.Field)]
public class LoadAssetAttribute : Attribute
{
    public string AssetPath { get; set; }
    
    public LoadAssetAttribute(string path) => AssetPath = path;
}

public class AssetManager
{
    [LoadAsset("res://assets/textures/player.png")]
    public Texture2D PlayerTexture;
}

// Async load
await AttributeProcessorUtils.ScanAndRegisterFieldAttributesAsync<LoadAssetAttribute>(
    null,
    async (field, instance, attr) =>
    {
        var asset = await LoadAssetAsync(attr.AssetPath);
        field.SetValue(instance, asset);
    }
);
```

---

#### `RegisterFieldAttributesAsync<TAttribute>`

**Signature:**
```csharp
public static async Task RegisterFieldAttributesAsync<TAttribute>(
    object instance,
    Type type,
    FieldAttributeHandlerAsync<TAttribute> handler
)
where TAttribute : Attribute
```

**Description:**
Asynchronously registers fields with `TAttribute` for a specific type or instance.

---

### Property Scanning Methods

#### `ScanAndRegisterPropertyAttributes<TAttribute>`

**Signature:**
```csharp
public static void ScanAndRegisterPropertyAttributes<TAttribute>(
    Assembly assembly,
    PropertyAttributeHandler<TAttribute> handler
)
where TAttribute : Attribute
```

**Description:**
Scans an assembly for properties decorated with `TAttribute` and invokes the handler for each.

**Use Case:** Configuration binding, validation, or automatic property initialization.

**Example:**
```csharp
[AttributeUsage(AttributeTargets.Property)]
public class ConfigAttribute : Attribute
{
    public string Key { get; set; }
    public object DefaultValue { get; set; }
}

public class AppSettings
{
    [Config("app.version", DefaultValue = "1.0.0")]
    public string Version { get; set; }
    
    [Config("app.debug", DefaultValue = false)]
    public bool DebugMode { get; set; }
}

// Auto-bind config
AttributeProcessorUtils.ScanAndRegisterPropertyAttributes<ConfigAttribute>(
    null,
    (prop, instance, attr) =>
    {
        var value = ConfigManager.Get(attr.Key, attr.DefaultValue);
        prop.SetValue(instance, value);
    }
);
```

---

#### `RegisterPropertyAttributes<TAttribute>`

**Signature:**
```csharp
public static void RegisterPropertyAttributes<TAttribute>(
    object instance,
    Type type,
    PropertyAttributeHandler<TAttribute> handler
)
where TAttribute : Attribute
```

**Description:**
Registers properties with `TAttribute` for a specific type or instance (targeted scanning).

---

## Delegate Types

### AttributeHandler<TAttribute>

```csharp
public delegate void AttributeHandler<TAttribute>(
    MethodInfo method,
    object instance,
    TAttribute attribute
)
where TAttribute : Attribute
```

**Purpose:** Handle discovered methods decorated with `TAttribute`

**Parameters:**
- `method` - Reflected method with the attribute
- `instance` - Object instance (null for static methods)
- `attribute` - The attribute instance

---

### FieldAttributeHandler<TAttribute>

```csharp
public delegate void FieldAttributeHandler<TAttribute>(
    FieldInfo field,
    object instance,
    TAttribute attribute
)
where TAttribute : Attribute
```

**Purpose:** Handle discovered fields decorated with `TAttribute`

---

### PropertyAttributeHandler<TAttribute>

```csharp
public delegate void PropertyAttributeHandler<TAttribute>(
    PropertyInfo property,
    object instance,
    TAttribute attribute
)
where TAttribute : Attribute
```

**Purpose:** Handle discovered properties decorated with `TAttribute`

---

### FieldAttributeHandlerAsync<TAttribute>

```csharp
public delegate Task FieldAttributeHandlerAsync<TAttribute>(
    FieldInfo field,
    object instance,
    TAttribute attribute
)
where TAttribute : Attribute
```

**Purpose:** Asynchronously handle discovered fields decorated with `TAttribute`

---

## Usage Examples

### Example 1: Event Handler Registration

Automatically discover and register event handlers marked with a custom attribute:

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class EventHandlerAttribute : Attribute
{
    public string EventName { get; set; }
    
    public EventHandlerAttribute(string eventName) => EventName = eventName;
}

public class GameEventManager
{
    private Dictionary<string, List<Action>> _handlers = new();
    
    public void RegisterEventHandlers()
    {
        AttributeProcessorUtils.ScanAndRegisterAttributes<EventHandlerAttribute>(
            null,
            (method, instance, attr) =>
            {
                var handler = (Action)Delegate.CreateDelegate(
                    typeof(Action),
                    instance,
                    method
                );
                
                if (!_handlers.ContainsKey(attr.EventName))
                    _handlers[attr.EventName] = new List<Action>();
                
                _handlers[attr.EventName].Add(handler);
            }
        );
    }
    
    public void RaiseEvent(string eventName)
    {
        if (_handlers.ContainsKey(eventName))
            foreach (var handler in _handlers[eventName])
                handler();
    }
}

// Usage in client code:
public class PlayerController
{
    [EventHandler("OnGameStart")]
    public void Initialize()
    {
        GD.Print("Player initialized");
    }
    
    [EventHandler("OnGameEnd")]
    public void Cleanup()
    {
        GD.Print("Player cleanup");
    }
}
```

---

### Example 2: Dependency Injection

Implement simple automatic dependency injection:

```csharp
[AttributeUsage(AttributeTargets.Field)]
public class ServiceAttribute : Attribute
{
    public Type ServiceType { get; set; }
    
    public ServiceAttribute(Type serviceType) => ServiceType = serviceType;
}

public static class ServiceInjector
{
    private static Dictionary<Type, object> _services = new();
    
    public static void RegisterService(Type type, object instance)
    {
        _services[type] = instance;
    }
    
    public static void InjectDependencies(object target)
    {
        AttributeProcessorUtils.RegisterFieldAttributes<ServiceAttribute>(
            target,
            target.GetType(),
            (field, instance, attr) =>
            {
                if (_services.ContainsKey(attr.ServiceType))
                {
                    field.SetValue(instance, _services[attr.ServiceType]);
                }
            }
        );
    }
}

// Usage:
public class PlayerService { }

public class GameController
{
    [Service(typeof(PlayerService))]
    public PlayerService PlayerService;
}

// In initialization:
ServiceInjector.RegisterService(typeof(PlayerService), new PlayerService());
var controller = new GameController();
ServiceInjector.InjectDependencies(controller);
```

---

### Example 3: Async Configuration Loading

Load configuration asynchronously from external sources:

```csharp
[AttributeUsage(AttributeTargets.Field)]
public class ConfigFieldAttribute : Attribute
{
    public string ConfigKey { get; set; }
    
    public ConfigFieldAttribute(string key) => ConfigKey = key;
}

public class ConfigLoader
{
    public static async Task LoadConfigAsync(object target)
    {
        await AttributeProcessorUtils.RegisterFieldAttributesAsync<ConfigFieldAttribute>(
            target,
            target.GetType(),
            async (field, instance, attr) =>
            {
                // Simulate async loading (e.g., from database, file system, etc.)
                var value = await FetchConfigValueAsync(attr.ConfigKey);
                field.SetValue(instance, Convert.ChangeType(value, field.FieldType));
            }
        );
    }
    
    private static async Task<object> FetchConfigValueAsync(string key)
    {
        await Task.Delay(100); // Simulate I/O
        return key switch
        {
            "game.difficulty" => "hard",
            "game.language" => "en",
            _ => null
        };
    }
}

// Usage:
public class GameConfig
{
    [ConfigField("game.difficulty")]
    public string Difficulty;
    
    [ConfigField("game.language")]
    public string Language;
}

var config = new GameConfig();
await ConfigLoader.LoadConfigAsync(config);
// config.Difficulty == "hard"
// config.Language == "en"
```

---

## Common Patterns

### Pattern 1: Plugin Discovery System

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public string Name { get; set; }
    public string Version { get; set; }
}

[Plugin(Name = "AIModule", Version = "1.0")]
public class AIPlugin : IGamePlugin
{
    public void Initialize() { }
}

public class PluginManager
{
    public List<IGamePlugin> LoadPlugins()
    {
        var plugins = new List<IGamePlugin>();
        
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<PluginAttribute>() != null);
        
        foreach (var type in types)
        {
            if (Activator.CreateInstance(type) is IGamePlugin plugin)
            {
                plugin.Initialize();
                plugins.Add(plugin);
            }
        }
        
        return plugins;
    }
}
```

---

### Pattern 2: Validation System

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class ValidateAttribute : Attribute
{
    public string Pattern { get; set; }
    public string ErrorMessage { get; set; }
}

public class ValidationEngine
{
    public List<string> ValidateObject(object obj)
    {
        var errors = new List<string>();
        
        AttributeProcessorUtils.RegisterPropertyAttributes<ValidateAttribute>(
            obj,
            obj.GetType(),
            (prop, instance, attr) =>
            {
                var value = prop.GetValue(instance)?.ToString() ?? "";
                if (!System.Text.RegularExpressions.Regex.IsMatch(value, attr.Pattern))
                {
                    errors.Add($"{prop.Name}: {attr.ErrorMessage}");
                }
            }
        );
        
        return errors;
    }
}

// Usage:
public class UserData
{
    [Validate(Pattern = @"^\w+@\w+\.\w+$", ErrorMessage = "Invalid email")]
    public string Email { get; set; }
}
```

---

## Best Practices

### 1. **Null Checks on Retrieved Values**

Always validate reflected members before invoking:

```csharp
AttributeProcessorUtils.RegisterAttributes<MyAttribute>(
    instance,
    type,
    (method, inst, attr) =>
    {
        // Validate before invocation
        if (method == null || attr == null)
            return;
        
        try
        {
            method.Invoke(inst, null);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error invoking {method.Name}: {ex.Message}");
        }
    }
);
```

---

### 2. **Performance: Cache Discovered Types**

Scanning assemblies repeatedly is expensive. Cache results:

```csharp
private static List<Type> _cachedTypes;

public static List<Type> GetTypesWithAttribute<TAttribute>()
where TAttribute : Attribute
{
    _cachedTypes ??= AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .Where(t => t.GetCustomAttributes(typeof(TAttribute), true).Length > 0)
        .ToList();
    
    return _cachedTypes;
}
```

---

### 3. **Error Handling in Handlers**

Wrap handler logic in try-catch to prevent cascading failures:

```csharp
AttributeProcessorUtils.ScanAndRegisterAttributes<EventHandlerAttribute>(
    null,
    (method, instance, attr) =>
    {
        try
        {
            method.Invoke(instance, null);
        }
        catch (TargetInvocationException ex)
        {
            GD.PrintErr($"Handler execution failed: {ex.InnerException?.Message}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Unexpected error: {ex.Message}");
        }
    }
);
```

---

### 4. **Parameter Validation**

Always define method signatures that work with your attributes:

```csharp
// ✅ Good: Method has no parameters
[EventHandler("OnStart")]
public void Initialize() { }

// ❌ Bad: Reflection can't easily call with parameters
[EventHandler("OnStart")]
public void Initialize(string data) { }
```

---

### 5. **Thread Safety**

Use locks when scanning from multiple threads:

```csharp
private static readonly object _lock = new object();

public static void ThreadSafeRegister<TAttribute>(
    Assembly assembly,
    AttributeHandler<TAttribute> handler
)
where TAttribute : Attribute
{
    lock (_lock)
    {
        AttributeProcessorUtils.ScanAndRegisterAttributes(assembly, handler);
    }
}
```

---

## Integration Guide

### Step 1: Define Your Attribute

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class MyCustomAttribute : Attribute
{
    public string Metadata { get; set; }
}
```

### Step 2: Mark Code with Your Attribute

```csharp
public class MyService
{
    [MyCustom("initialization")]
    public void OnInit()
    {
        // Initialization logic
    }
}
```

### Step 3: Discover and Process

```csharp
public class MyProcessor
{
    public void ProcessAttributes()
    {
        AttributeProcessorUtils.ScanAndRegisterAttributes<MyCustomAttribute>(
            null,
            (method, instance, attr) =>
            {
                GD.Print($"Found method: {method.Name} with metadata: {attr.Metadata}");
                method.Invoke(instance, null);
            }
        );
    }
}
```

---

## Troubleshooting

### Issue: "Type not found" or "No types discovered"

**Causes:**
- Assembly not loaded in memory
- Attribute not decorated on target members
- BindingFlags don't match member visibility

**Solution:**
```csharp
// Ensure assembly is loaded
var assembly = Assembly.Load("MyAssembly");

// Or use Type.GetType() to load:
var type = Type.GetType("Namespace.ClassName, AssemblyName");
```

---

### Issue: Handler Receives Null Instance for Instance Methods

**Cause:** Type has no parameterless constructor

**Solution:**
```csharp
// Use targeted registration with existing instance
var instance = new MyClass(parameter);
AttributeProcessorUtils.RegisterAttributes<MyAttribute>(
    instance,
    typeof(MyClass),
    handler
);
```

---

### Issue: Async Handler Never Completes

**Cause:** Not awaiting the async scan method

**Solution:**
```csharp
// ❌ Wrong: Don't fire-and-forget
AttributeProcessorUtils.ScanAndRegisterFieldAttributesAsync<MyAttribute>(null, handler);

// ✅ Correct: Await the task
await AttributeProcessorUtils.ScanAndRegisterFieldAttributesAsync<MyAttribute>(null, handler);
```

---

### Issue: Performance Degradation with Large Assemblies

**Cause:** Repeated full-assembly scans

**Solution:**
```csharp
// Cache results on first scan
private static Dictionary<Type, List<MethodInfo>> _methodCache = new();

public static List<MethodInfo> GetCachedMethods<TAttribute>()
where TAttribute : Attribute
{
    var key = typeof(TAttribute);
    if (!_methodCache.ContainsKey(key))
    {
        _methodCache[key] = new List<MethodInfo>();
        AttributeProcessorUtils.ScanAndRegisterAttributes<TAttribute>(
            null,
            (method, instance, attr) =>
            {
                _methodCache[key].Add(method);
            }
        );
    }
    return _methodCache[key];
}
```

---

## Extension Points

### Custom Attribute Types

Extend with specialized attributes for your domain:

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string CommandName { get; set; }
    public string Description { get; set; }
}

[Command("save", "Save game state")]
public void SaveGame() { }
```

### Handler Composition

Chain multiple handlers:

```csharp
public static void ChainHandlers<TAttribute>(
    object instance,
    Type type,
    params AttributeHandler<TAttribute>[] handlers
)
where TAttribute : Attribute
{
    AttributeProcessorUtils.RegisterAttributes<TAttribute>(
        instance,
        type,
        (method, inst, attr) =>
        {
            foreach (var handler in handlers)
                handler(method, inst, attr);
        }
    );
}
```

---

## Related Classes

- `MetaComponentInfo` - Component metadata discovery
- `AttributeDiscovery` - High-level attribute scanning
- `Ascendere.Editor.EditorSettings` - Project settings management

---

## Version History

| Version | Changes |
|---------|---------|
| 1.0     | Initial implementation with method, field, property scanning |
| 1.1     | Added async field scanning support |
| 1.2     | Enhanced error handling and static member support |

---

## Contributing

To extend `AttributeProcessorUtils`:

1. Add new handler delegate type for your member type (if needed)
2. Implement `ScanAndRegister[MemberType]Attributes` for assembly scanning
3. Implement `Register[MemberType]Attributes` for targeted scanning
4. Document with usage examples
5. Add unit tests for new functionality

