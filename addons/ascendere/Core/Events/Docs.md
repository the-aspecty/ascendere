# Godot 4.5 C# EventBus Documentation

A type-safe, attribute-based event system for Godot 4.5 C# with native signal integration, priority handling, and advanced debugging features.

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Installation](#installation)
3. [Core Concepts](#core-concepts)
4. [Event Definition](#event-definition)
5. [Event Handlers](#event-handlers)
6. [Signal Handlers](#signal-handlers)
7. [Publishing Events](#publishing-events)
8. [Advanced Features](#advanced-features)
9. [Best Practices](#best-practices)
10. [API Reference](#api-reference)
11. [Examples](#examples)

---

## Quick Start

### 1. Setup EventBus
Add `EventBus.cs` to your project and add it as an **Autoload Singleton**:
- Project → Project Settings → Autoload
- Add EventBus script with name `EventBus`

### 2. Define an Event
```csharp
public struct PlayerDiedEvent : IEvent
{
    public string PlayerName;
    public Vector2 DeathPosition;
    
    public Godot.Collections.Dictionary ToGodotDict()
    {
        return new Godot.Collections.Dictionary
        {
            { "PlayerName", PlayerName },
            { "DeathPosition", DeathPosition }
        };
    }
    
    public void FromGodotDict(Godot.Collections.Dictionary dict)
    {
        PlayerName = dict["PlayerName"].AsString();
        DeathPosition = dict["DeathPosition"].AsVector2();
    }
}
```

### 3. Subscribe and Handle
```csharp
public partial class GameManager : Node
{
    public override void _Ready()
    {
        EventBus.Instance.Subscribe(this);
    }
    
    public override void _ExitTree()
    {
        EventBus.Instance.Unsubscribe(this);
    }
    
    [EventHandler(typeof(PlayerDiedEvent))]
    private void OnPlayerDied(PlayerDiedEvent evt)
    {
        GD.Print($"{evt.PlayerName} died at {evt.DeathPosition}");
        // Show game over screen
    }
}
```

### 4. Publish Events
```csharp
EventBus.Instance.Publish(new PlayerDiedEvent
{
    PlayerName = "Hero",
    DeathPosition = GlobalPosition
});
```

---

## Installation

### Step 1: Add EventBus to Project
1. Copy the `EventBus.cs` file to your project
2. Ensure it compiles without errors

### Step 2: Configure Autoload
1. Go to **Project → Project Settings → Autoload**
2. Click the folder icon and select `EventBus.cs`
3. Set Node Name to `EventBus`
4. Enable the autoload

### Step 3: Configure Settings (Optional)
Select the EventBus node in the scene tree and configure:
- `Enable History` - Track event history for debugging
- `Max History Size` - Maximum events to store (default: 100)
- `Log Events` - Print events to console
- `Enable Profiling` - Track performance statistics

---

## Core Concepts

### Type Safety
Events are **compile-time checked structs**. No string-based event names means:
- IDE autocomplete and IntelliSense
- Refactoring safety
- Zero runtime lookup overhead
- Catches typos at compile time

### Decoupling
Publishers don't need references to subscribers:
```csharp
// Player doesn't need to know about UI, GameManager, etc.
EventBus.Instance.Publish(new PlayerHealthChangedEvent {...});

// UI, GameManager, and any other systems can listen independently
```

### Automatic Management
- Attribute scanning finds all handlers automatically
- No manual `Connect()`/`Disconnect()` calls needed
- Cleanup handled by `Unsubscribe()` in `_ExitTree()`

---

## Event Definition

### Basic Event
```csharp
public struct MyEvent : IEvent
{
    public string Message;
    public int Value;
    
    public Godot.Collections.Dictionary ToGodotDict()
    {
        return new Godot.Collections.Dictionary
        {
            { "Message", Message },
            { "Value", Value }
        };
    }
    
    public void FromGodotDict(Godot.Collections.Dictionary dict)
    {
        Message = dict["Message"].AsString();
        Value = dict["Value"].AsInt32();
    }
}
```

### Cancellable Event
```csharp
public struct DamageEvent : ICancellableEvent
{
    public int Damage;
    public string Source;
    public bool IsCancelled { get; set; }
    
    public Godot.Collections.Dictionary ToGodotDict()
    {
        return new Godot.Collections.Dictionary
        {
            { "Damage", Damage },
            { "Source", Source },
            { "IsCancelled", IsCancelled }
        };
    }
    
    public void FromGodotDict(Godot.Collections.Dictionary dict)
    {
        Damage = dict["Damage"].AsInt32();
        Source = dict["Source"].AsString();
        IsCancelled = dict["IsCancelled"].AsBool();
    }
}
```

### Event with Complex Types
```csharp
public struct InventoryChangedEvent : IEvent
{
    public string[] ItemNames;
    public int[] ItemCounts;
    public Godot.Collections.Array<Item> Items;
    
    public Godot.Collections.Dictionary ToGodotDict()
    {
        return new Godot.Collections.Dictionary
        {
            { "ItemNames", new Godot.Collections.Array(ItemNames) },
            { "ItemCounts", new Godot.Collections.Array(ItemCounts) },
            { "Items", Items }
        };
    }
    
    public void FromGodotDict(Godot.Collections.Dictionary dict)
    {
        var names = dict["ItemNames"].AsGodotArray();
        ItemNames = names.Select(v => v.AsString()).ToArray();
        
        var counts = dict["ItemCounts"].AsGodotArray();
        ItemCounts = counts.Select(v => v.AsInt32()).ToArray();
        
        Items = dict["Items"].AsGodotArray<Item>();
    }
}
```

---

## Event Handlers

### Basic Handler
```csharp
[EventHandler(typeof(PlayerHealthChangedEvent))]
private void OnHealthChanged(PlayerHealthChangedEvent evt)
{
    GD.Print($"Health: {evt.NewHealth}");
}
```

### Handler with Priority
```csharp
// Higher priority = executes first (default is 0)
[EventHandler(typeof(DamageEvent), Priority = 100)]
private void OnDamageFirst(DamageEvent evt)
{
    // This runs before priority 0 handlers
}

[EventHandler(typeof(DamageEvent), Priority = -10)]
private void OnDamageLast(DamageEvent evt)
{
    // This runs last
}
```

### Modifying Events
```csharp
[EventHandler(typeof(DamageEvent))]
private DamageEvent OnDamage(DamageEvent evt)
{
    // Modify damage based on armor
    evt.Damage = Mathf.Max(0, evt.Damage - _armor);
    return evt; // Return modified event
}
```

### Cancelling Events
```csharp
[EventHandler(typeof(DamageEvent), Priority = 100)]
private DamageEvent OnDamageCheck(DamageEvent evt)
{
    if (_isInvulnerable)
    {
        evt.IsCancelled = true; // Cancel the event
    }
    return evt;
}

// Later handler won't run if cancelled
[EventHandler(typeof(DamageEvent))]
private void ApplyDamage(DamageEvent evt)
{
    // This won't execute if cancelled
    _health -= evt.Damage;
}
```

### Multiple Handlers
```csharp
public partial class GameManager : Node
{
    [EventHandler(typeof(PlayerDiedEvent))]
    private void OnPlayerDied(PlayerDiedEvent evt)
    {
        // Show game over
    }
    
    [EventHandler(typeof(EnemyDiedEvent))]
    private void OnEnemyDied(EnemyDiedEvent evt)
    {
        // Update score
    }
    
    [EventHandler(typeof(LevelCompletedEvent))]
    private void OnLevelCompleted(LevelCompletedEvent evt)
    {
        // Load next level
    }
}
```

---

## Signal Handlers

### Signal on Same Node
```csharp
public partial class Item : Area2D
{
    public override void _Ready()
    {
        EventBus.Instance.Subscribe(this);
    }
    
    [SignalHandler("body_entered")]
    private void OnBodyEntered(Node2D body)
    {
        GD.Print($"Body entered: {body.Name}");
    }
    
    [SignalHandler("area_entered")]
    private void OnAreaEntered(Area2D area)
    {
        GD.Print($"Area entered: {area.Name}");
    }
}
```

### Signal on Child Node
```csharp
public partial class MainMenu : Control
{
    public override void _Ready()
    {
        EventBus.Instance.Subscribe(this);
    }
    
    [SignalHandler("pressed", "StartButton")]
    private void OnStartPressed()
    {
        GetTree().ChangeSceneToFile("res://game.tscn");
    }
    
    [SignalHandler("pressed", "OptionsButton")]
    private void OnOptionsPressed()
    {
        GetNode<Panel>("OptionsPanel").Visible = true;
    }
    
    [SignalHandler("pressed", "QuitButton")]
    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
```

### Timer and Animation Signals
```csharp
public partial class Enemy : CharacterBody2D
{
    public override void _Ready()
    {
        EventBus.Instance.Subscribe(this);
    }
    
    [SignalHandler("timeout", "AttackTimer")]
    private void OnAttackTimeout()
    {
        PerformAttack();
    }
    
    [SignalHandler("animation_finished", "AnimationPlayer")]
    private void OnAnimationFinished(StringName animName)
    {
        if (animName == "death")
        {
            QueueFree();
        }
    }
}
```

### Multiple Signals to Same Handler
```csharp
public partial class AudioManager : Node
{
    public override void _Ready()
    {
        EventBus.Instance.Subscribe(this);
    }
    
    [SignalHandler("pressed", "Button1")]
    [SignalHandler("pressed", "Button2")]
    [SignalHandler("pressed", "Button3")]
    private void OnAnyButtonPressed()
    {
        PlayClickSound();
    }
}
```

---

## Publishing Events

### Immediate Publishing
```csharp
// Event is processed immediately
EventBus.Instance.Publish(new ItemCollectedEvent
{
    ItemName = "Gold Coin",
    Quantity = 10,
    Position = GlobalPosition
});
```

### Queued Publishing
```csharp
// Event is queued and processed next frame
EventBus.Instance.QueueEvent(new ItemCollectedEvent
{
    ItemName = "Gold Coin",
    Quantity = 10,
    Position = GlobalPosition
});

// Useful for:
// - Publishing from _PhysicsProcess to _Process consumers
// - Thread-safe event publishing
// - Avoiding mid-frame state issues
```

### Cancellable Publishing
```csharp
var damageEvt = EventBus.Instance.PublishCancellable(new DamageEvent
{
    Damage = 25,
    Source = "Enemy",
    IsCancelled = false
});

if (!damageEvt.IsCancelled)
{
    // Damage was not cancelled by any handler
    _health -= damageEvt.Damage;
}
```

### Conditional Publishing
```csharp
// Only publish if someone is listening
if (EventBus.Instance.HasSubscribers<ExpensiveEvent>())
{
    var evt = CalculateExpensiveEventData();
    EventBus.Instance.Publish(evt);
}
```

---

## Advanced Features

### Runtime Subscriptions
```csharp
public override void _Ready()
{
    // Subscribe with attribute scanning
    EventBus.Instance.Subscribe(this);
    
    // Also subscribe at runtime
    EventBus.Instance.SubscribeToEvent<PowerUpCollectedEvent>(
        OnPowerUpCollected, 
        priority: 10
    );
}

private void OnPowerUpCollected(PowerUpCollectedEvent evt)
{
    GD.Print($"Runtime handler: {evt.PowerUpType}");
}

public override void _ExitTree()
{
    EventBus.Instance.UnsubscribeFromEvent<PowerUpCollectedEvent>(OnPowerUpCollected);
    EventBus.Instance.Unsubscribe(this);
}
```

### Event History
```csharp
// Get all event history
var allHistory = EventBus.Instance.GetEventHistory();

// Get filtered history
var healthEvents = EventBus.Instance.GetEventHistory("PlayerHealthChangedEvent");

foreach (var entry in healthEvents)
{
    GD.Print($"Event: {entry.EventType}");
    GD.Print($"Time: {entry.Timestamp}");
    GD.Print($"Data: {entry.Data}");
    GD.Print($"Subscribers: {entry.SubscriberCount}");
}

// Clear history
EventBus.Instance.ClearHistory();
```

### Performance Profiling
```csharp
var stats = EventBus.Instance.GetEventStats();

foreach (var kvp in stats)
{
    var eventName = kvp.Key;
    var stat = kvp.Value;
    
    GD.Print($"Event: {eventName}");
    GD.Print($"  Total Calls: {stat.TotalCalls}");
    GD.Print($"  Avg Time: {stat.AverageExecutionTimeMicros}μs");
    GD.Print($"  Subscribers: {stat.LastSubscriberCount}");
}

EventBus.Instance.ClearStats();
```

### Subscriber Information
```csharp
// Check if anyone is listening
if (EventBus.Instance.HasSubscribers<MyEvent>())
{
    GD.Print("Someone is listening!");
}

// Get exact count
int count = EventBus.Instance.GetSubscriberCount<MyEvent>();
GD.Print($"Subscriber count: {count}");
```

---

## Best Practices

### 1. Always Unsubscribe
```csharp
public override void _ExitTree()
{
    EventBus.Instance.Unsubscribe(this);
}
```

### 2. Use Descriptive Event Names
```csharp
// Good
PlayerHealthChangedEvent
ItemCollectedEvent
EnemyDiedEvent

// Bad
Event1
DataChangedEvent
UpdateEvent
```

### 3. Keep Events Immutable When Possible
```csharp
// Good - read-only data
public struct ScoreChangedEvent : IEvent
{
    public readonly int OldScore;
    public readonly int NewScore;
    
    public ScoreChangedEvent(int oldScore, int newScore)
    {
        OldScore = oldScore;
        NewScore = newScore;
    }
}
```

### 4. Use Priority Wisely
```csharp
// Shield system checks damage first (high priority)
[EventHandler(typeof(DamageEvent), Priority = 100)]

// Player takes damage after checks (normal priority)
[EventHandler(typeof(DamageEvent), Priority = 0)]

// UI updates last (low priority)
[EventHandler(typeof(DamageEvent), Priority = -10)]
```

### 5. Event Naming Convention
- Use past tense for completed actions: `ItemCollectedEvent`, `PlayerDiedEvent`
- Use present tense for ongoing/cancellable: `DamageEvent`, `InputEvent`
- Suffix with `Event`: `MyActionEvent`

### 6. Avoid Heavy Computation in Handlers
```csharp
// Bad
[EventHandler(typeof(GameTickEvent))]
private void OnGameTick(GameTickEvent evt)
{
    RecalculateEntireWorld(); // Too expensive!
}

// Good
[EventHandler(typeof(GameTickEvent))]
private void OnGameTick(GameTickEvent evt)
{
    _needsRecalculation = true; // Set flag, process later
}
```

### 7. Use QueueEvent for Cross-Process Communication
```csharp
public override void _PhysicsProcess(double delta)
{
    if (DetectCollision())
    {
        // Queue event to be processed in _Process
        EventBus.Instance.QueueEvent(new CollisionEvent {...});
    }
}
```

---

## API Reference

### EventBus Methods

#### Subscribe/Unsubscribe
```csharp
void Subscribe(Node subscriber)
void Unsubscribe(Node subscriber)
void SubscribeToEvent<T>(Action<T> handler, int priority = 0)
void UnsubscribeFromEvent<T>(Action<T> handler)
```

#### Publishing
```csharp
void Publish<T>(T evt) where T : struct, IEvent
void QueueEvent<T>(T evt) where T : struct, IEvent
T PublishCancellable<T>(T evt) where T : struct, ICancellableEvent
```

#### Information
```csharp
bool HasSubscribers<T>() where T : struct, IEvent
int GetSubscriberCount<T>() where T : struct, IEvent
```

#### History & Stats
```csharp
List<EventHistoryEntry> GetEventHistory(string eventTypeFilter = null)
Dictionary<string, EventStats> GetEventStats()
void ClearHistory()
void ClearStats()
```

### Attributes

```csharp
[EventHandler(typeof(MyEvent), Priority = 0)]

[SignalHandler("signal_name")]
[SignalHandler("signal_name", "NodePath")]
```

### Interfaces

```csharp
public interface IEvent
{
    Godot.Collections.Dictionary ToGodotDict();
    void FromGodotDict(Godot.Collections.Dictionary dict);
}

public interface ICancellableEvent : IEvent
{
    bool IsCancelled { get; set; }
}
```

---

## Examples

### Complete Combat System
```csharp
// Events
public struct AttackEvent : ICancellableEvent
{
    public string AttackerName;
    public string TargetName;
    public int Damage;
    public bool IsCritical;
    public bool IsCancelled { get; set; }
    
    public Godot.Collections.Dictionary ToGodotDict() { /* ... */ }
    public void FromGodotDict(Godot.Collections.Dictionary dict) { /* ... */ }
}

// Player
public partial class Player : CharacterBody2D
{
    [EventHandler(typeof(AttackEvent), Priority = 50)]
    private AttackEvent OnAttackReceived(AttackEvent evt)
    {
        if (evt.TargetName == Name)
        {
            if (_isDodging)
            {
                evt.IsCancelled = true;
                GD.Print("Dodged!");
            }
            else if (!evt.IsCancelled)
            {
                _health -= evt.Damage;
                PlayHitAnimation();
            }
        }
        return evt;
    }
}

// Armor System
public partial class ArmorSystem : Node
{
    [EventHandler(typeof(AttackEvent), Priority = 100)]
    private AttackEvent OnAttackFilter(AttackEvent evt)
    {
        evt.Damage = Mathf.Max(0, evt.Damage - _armorValue);
        return evt;
    }
}

// Damage Numbers UI
public partial class DamageNumbers : Node2D
{
    [EventHandler(typeof(AttackEvent), Priority = -100)]
    private void ShowDamageNumber(AttackEvent evt)
    {
        if (!evt.IsCancelled)
        {
            SpawnFloatingText(evt.Damage.ToString(), evt.IsCritical);
        }
    }
}
```

### Save/Load System
```csharp
public struct GameSavedEvent : IEvent
{
    public string SaveSlot;
    public double Timestamp;
    
    public Godot.Collections.Dictionary ToGodotDict() { /* ... */ }
    public void FromGodotDict(Godot.Collections.Dictionary dict) { /* ... */ }
}

public partial class SaveManager : Node
{
    public void SaveGame(string slot)
    {
        // Save player data
        // Save inventory
        // Save world state
        
        EventBus.Instance.Publish(new GameSavedEvent
        {
            SaveSlot = slot,
            Timestamp = Time.GetUnixTimeFromSystem()
        });
    }
}

public partial class SaveNotification : Control
{
    [EventHandler(typeof(GameSavedEvent))]
    private void OnGameSaved(GameSavedEvent evt)
    {
        ShowNotification($"Game saved to slot {evt.SaveSlot}");
    }
}
```

### Achievement System
```csharp
public struct AchievementUnlockedEvent : IEvent
{
    public string AchievementId;
    public string Title;
    public string Description;
    
    public Godot.Collections.Dictionary ToGodotDict() { /* ... */ }
    public void FromGodotDict(Godot.Collections.Dictionary dict) { /* ... */ }
}

public partial class AchievementManager : Node
{
    [EventHandler(typeof(EnemyDiedEvent))]
    private void OnEnemyDied(EnemyDiedEvent evt)
    {
        _enemiesKilled++;
        if (_enemiesKilled >= 100)
        {
            UnlockAchievement("ENEMY_SLAYER");
        }
    }
    
    private void UnlockAchievement(string id)
    {
        EventBus.Instance.Publish(new AchievementUnlockedEvent
        {
            AchievementId = id,
            Title = "Enemy Slayer",
            Description = "Defeat 100 enemies"
        });
    }
}

public partial class AchievementPopup : Control
{
    [EventHandler(typeof(AchievementUnlockedEvent))]
    private void OnAchievementUnlocked(AchievementUnlockedEvent evt)
    {
        ShowPopup(evt.Title, evt.Description);
        PlaySound("achievement_unlocked");
    }
}
```

---

## GDScript Integration

### Listening from GDScript
```gdscript
func _ready():
    EventBus.EventFired.connect(_on_event_fired)

func _on_event_fired(event_type: String, event_data: Dictionary):
    match event_type:
        "PlayerHealthChangedEvent":
            print("Health: ", event_data["NewHealth"])
        "ItemCollectedEvent":
            print("Collected: ", event_data["ItemName"])
```

### Publishing from GDScript
```gdscript
# Create event data
var event_data = {
    "ItemName": "Gold Coin",
    "Quantity": 10,
    "Position": global_position
}

# This will trigger C# handlers
EventBus.emit_signal("EventFired", "ItemCollectedEvent", event_data)
```

---

## Troubleshooting

### Events Not Firing
- ✅ Check EventBus is in Autoload
- ✅ Call `Subscribe(this)` in `_Ready()`
- ✅ Verify event type matches exactly
- ✅ Enable `LogEvents` to see what's published

### Signals Not Connecting
- ✅ Verify node path is correct
- ✅ Check signal name matches Godot's signal
- ✅ Ensure child nodes exist before `Subscribe()`
- ✅ Check console for connection errors

### Memory Leaks
- ✅ Always call `Unsubscribe()` in `_ExitTree()`
- ✅ Don't hold references to dead nodes
- ✅ Use weak references for long-lived subscribers

### Performance Issues
- ✅ Enable profiling to identify slow handlers
- ✅ Avoid heavy computation in handlers
- ✅ Use `QueueEvent()` for non-critical events
- ✅ Check subscriber counts with `GetSubscriberCount()`

---

## License

This EventBus system is provided as-is for use in Godot projects. Feel free to modify and extend it for your needs!