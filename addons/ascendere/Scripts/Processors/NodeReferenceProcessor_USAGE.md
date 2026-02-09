# NodeReferenceProcessor - Memory Leak Prevention Guide

## Overview

The `NodeReferenceProcessor` automatically links node references using the `[NodeReference]` attribute and **now includes automatic memory cleanup** to prevent memory leaks. This guide explains how to use it safely and avoid common pitfalls.

---

## Key Memory Safety Features

### 1. **Automatic Cleanup**
The processor automatically clears node references when nodes exit the tree:
- Subscribes to `TreeExiting` signal
- Clears all tracked references
- Removes nodes from tracking dictionary

### 2. **Reference Validation**
Use `ValidateNodeReference()` to check if a node is still valid:
```csharp
if (NodeReferenceProcessor.Instance.ValidateNodeReference(_myButton))
{
    _myButton.Text = "Safe to use!";
}
```

### 3. **Manual Cleanup**
Call `CleanupNodeReferences()` in `_ExitTree()` for explicit control:
```csharp
public override void _ExitTree()
{
    NodeReferenceProcessor.Instance.CleanupNodeReferences(this);
    base._ExitTree();
}
```

---

## Basic Usage

### Pattern 1: Automatic Cleanup (Recommended)

The processor handles cleanup automatically - you don't need to do anything extra.

```csharp
public partial class Menu : Control
{
    [NodeReference("StartButton")]
    private Button _startButton;
    
    [NodeReference("StatusLabel")]
    private Label _statusLabel;
    
    public override void _Ready()
    {
        // Process node references - cleanup is automatic
        NodeReferenceProcessor.Instance.ProcessNodeReferences(this);
        
        // Use the references
        _startButton.Pressed += OnStartPressed;
    }
    
    private void OnStartPressed()
    {
        _statusLabel.Text = "Started!";
    }
    
    // No cleanup needed - automatic!
}
```

### Pattern 2: Manual Cleanup (Explicit Control)

For explicit control over cleanup timing:

```csharp
public partial class GameScene : Node
{
    [NodeReference("Player")]
    private CharacterBody2D _player;
    
    [NodeReference("Camera")]
    private Camera2D _camera;
    
    public override void _Ready()
    {
        NodeReferenceProcessor.Instance.ProcessNodeReferences(this);
    }
    
    public override void _ExitTree()
    {
        // Manually cleanup node references
        NodeReferenceProcessor.Instance.CleanupNodeReferences(this);
        
        // Other cleanup...
        base._ExitTree();
    }
}
```

### Pattern 3: Safe Reference Usage

Always validate references that might be freed:

```csharp
public partial class PlayerController : Node
{
    [NodeReference("../Enemy")]
    private Node2D _enemy;
    
    public override void _Process(double delta)
    {
        // Validate before using - enemy might be freed
        if (NodeReferenceProcessor.Instance.ValidateNodeReference(_enemy))
        {
            // Safe to use
            var distance = GlobalPosition.DistanceTo(_enemy.GlobalPosition);
            GD.Print($"Distance to enemy: {distance}");
        }
        else
        {
            GD.Print("Enemy no longer valid");
        }
    }
}
```

---

## Memory Leak Prevention Patterns

### ✅ GOOD: Proper Usage

```csharp
public partial class InventoryUI : Control
{
    [NodeReference("ItemList")]
    private ItemList _itemList;
    
    [NodeReference("DescriptionLabel")]
    private Label _descriptionLabel;
    
    public override void _Ready()
    {
        // Process references
        NodeReferenceProcessor.Instance.ProcessNodeReferences(this);
        
        // Connect signals using reload-safe pattern
        _itemList.Connect(
            ItemList.SignalName.ItemSelected,
            new Callable(this, MethodName.OnItemSelected)
        );
    }
    
    private void OnItemSelected(long index)
    {
        // Validate before use
        if (NodeReferenceProcessor.Instance.ValidateNodeReference(_descriptionLabel))
        {
            _descriptionLabel.Text = $"Item {index} selected";
        }
    }
    
    public override void _ExitTree()
    {
        // Disconnect signals to avoid keeping references
        if (_itemList != null && GodotObject.IsInstanceValid(_itemList))
        {
            var callable = new Callable(this, MethodName.OnItemSelected);
            if (_itemList.IsConnected(ItemList.SignalName.ItemSelected, callable))
            {
                _itemList.Disconnect(ItemList.SignalName.ItemSelected, callable);
            }
        }
        
        // Cleanup happens automatically, but you can do it explicitly
        NodeReferenceProcessor.Instance.CleanupNodeReferences(this);
        
        base._ExitTree();
    }
}
```

### ❌ BAD: Common Mistakes

#### 1. Storing References Without Validation
```csharp
// BAD: Not checking if node is still valid
public partial class BadExample : Node
{
    [NodeReference("Enemy")]
    private Node2D _enemy;
    
    private List<Node2D> _enemies = new List<Node2D>();
    
    public override void _Ready()
    {
        NodeReferenceProcessor.Instance.ProcessNodeReferences(this);
        
        // BAD: Storing reference elsewhere without tracking
        _enemies.Add(_enemy);
    }
    
    public void AttackEnemy()
    {
        // CRASH: _enemy might be freed!
        _enemy.QueueFree();
        
        // Later...
        foreach (var enemy in _enemies) // BAD: Might have freed nodes
        {
            enemy.GlobalPosition = Vector2.Zero; // CRASH!
        }
    }
}

// GOOD: Validate before using
public void AttackEnemy()
{
    if (NodeReferenceProcessor.Instance.ValidateNodeReference(_enemy))
    {
        _enemy.QueueFree();
    }
    
    // Clean up list
    _enemies.RemoveAll(e => !NodeReferenceProcessor.Instance.ValidateNodeReference(e));
}
```

#### 2. Not Disconnecting Signals
```csharp
// BAD: Signal keeps reference alive
public partial class BadSignals : Control
{
    [NodeReference("Button")]
    private Button _button;
    
    public override void _Ready()
    {
        NodeReferenceProcessor.Instance.ProcessNodeReferences(this);
        
        // BAD: Using lambda captures this and _button
        _button.Pressed += () => GD.Print(_button.Text);
    }
    
    // No cleanup - signal keeps references!
}

// GOOD: Use Callable pattern and disconnect
public partial class GoodSignals : Control
{
    [NodeReference("Button")]
    private Button _button;
    
    public override void _Ready()
    {
        NodeReferenceProcessor.Instance.ProcessNodeReferences(this);
        
        // GOOD: Reload-safe callable
        _button.Connect(
            Button.SignalName.Pressed,
            new Callable(this, MethodName.OnButtonPressed)
        );
    }
    
    private void OnButtonPressed()
    {
        GD.Print(_button.Text);
    }
    
    public override void _ExitTree()
    {
        // Disconnect signal
        if (_button != null && GodotObject.IsInstanceValid(_button))
        {
            var callable = new Callable(this, MethodName.OnButtonPressed);
            if (_button.IsConnected(Button.SignalName.Pressed, callable))
            {
                _button.Disconnect(Button.SignalName.Pressed, callable);
            }
        }
        base._ExitTree();
    }
}
```

#### 3. Circular References
```csharp
// BAD: Creates circular reference
public partial class Parent : Node
{
    [NodeReference("Child")]
    private Child _child;
    
    public override void _Ready()
    {
        NodeReferenceProcessor.Instance.ProcessNodeReferences(this);
        
        // BAD: Child holds reference to parent
        _child.SetParent(this);
    }
}

public partial class Child : Node
{
    private Parent _parent; // Circular reference!
    
    public void SetParent(Parent parent)
    {
        _parent = parent;
    }
}

// GOOD: Use GetParent() instead of storing reference
public partial class GoodChild : Node
{
    public void DoSomething()
    {
        // Get parent dynamically
        var parent = GetParent() as Parent;
        if (parent != null && GodotObject.IsInstanceValid(parent))
        {
            // Use parent
        }
    }
}
```

---

## Advanced Patterns

### Pattern: Pooled Objects

When using object pooling, clear references before returning to pool:

```csharp
public partial class PooledProjectile : Area2D
{
    [NodeReference("Sprite")]
    private Sprite2D _sprite;
    
    [NodeReference("CollisionShape")]
    private CollisionShape2D _collision;
    
    public override void _Ready()
    {
        NodeReferenceProcessor.Instance.ProcessNodeReferences(this);
    }
    
    public void ReturnToPool()
    {
        // Cleanup before pooling
        NodeReferenceProcessor.Instance.CleanupNodeReferences(this);
        
        // Re-process when retrieved from pool
        NodeReferenceProcessor.Instance.ProcessNodeReferences(this);
    }
}
```

### Pattern: Dynamic Node Creation

For dynamically created nodes:

```csharp
public partial class DynamicSpawner : Node
{
    private List<Node> _spawnedNodes = new List<Node>();
    
    public void SpawnNode()
    {
        var scene = GD.Load<PackedScene>("res://scenes/enemy.tscn");
        var instance = scene.Instantiate();
        AddChild(instance);
        
        // Process its references
        NodeReferenceProcessor.Instance.ProcessNodeReferences(instance);
        
        _spawnedNodes.Add(instance);
    }
    
    public override void _ExitTree()
    {
        // Cleanup all spawned nodes
        foreach (var node in _spawnedNodes)
        {
            if (GodotObject.IsInstanceValid(node))
            {
                NodeReferenceProcessor.Instance.CleanupNodeReferences(node);
            }
        }
        _spawnedNodes.Clear();
        
        base._ExitTree();
    }
}
```

### Pattern: Optional References

For optional node references that might not exist:

```csharp
public partial class OptionalReferences : Node
{
    [NodeReference("OptionalUI", required: false)]
    private Control _optionalUI;
    
    public override void _Ready()
    {
        NodeReferenceProcessor.Instance.ProcessNodeReferences(this);
    }
    
    public override void _Process(double delta)
    {
        // Always validate optional references
        if (_optionalUI != null && 
            NodeReferenceProcessor.Instance.ValidateNodeReference(_optionalUI))
        {
            _optionalUI.Visible = true;
        }
    }
}
```

---

## Testing for Memory Leaks

### Manual Testing

```csharp
public partial class MemoryLeakTest : Node
{
    public override void _Ready()
    {
        // Create and destroy many nodes
        for (int i = 0; i < 1000; i++)
        {
            var scene = GD.Load<PackedScene>("res://scenes/test.tscn");
            var instance = scene.Instantiate();
            AddChild(instance);
            
            NodeReferenceProcessor.Instance.ProcessNodeReferences(instance);
            
            // Remove after short delay
            GetTree().CreateTimer(0.1).Timeout += () =>
            {
                if (GodotObject.IsInstanceValid(instance))
                {
                    instance.QueueFree();
                }
            };
        }
    }
}
```

### Validation

Check tracked references count:

```csharp
// Add this method to NodeReferenceProcessor for debugging
public int GetTrackedNodeCount()
{
    return _trackedReferences.Count;
}

// Use in tests
GD.Print($"Tracked nodes: {NodeReferenceProcessor.Instance.GetTrackedNodeCount()}");
```

---

## Summary

### ✅ DO:
- Let automatic cleanup handle memory management
- Validate references before using them
- Disconnect signals in `_ExitTree()`
- Use `Callable` pattern instead of lambdas
- Clear references manually if you need explicit control

### ❌ DON'T:
- Store node references in collections without validation
- Use lambdas that capture node references in signals
- Create circular references between nodes
- Skip cleanup when using object pools
- Assume references are always valid

### Key APIs:
- `ProcessNodeReferences(node)` - Setup references (automatic cleanup)
- `CleanupNodeReferences(node)` - Manual cleanup
- `ValidateNodeReference(node)` - Check if node is valid
- `ClearAllTrackedReferences()` - Clear all tracking (shutdown)

Following these patterns ensures your game remains memory-efficient and leak-free!
