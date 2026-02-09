# Use Cases: Editor Runtime Communication Addon

This addon enables developers to communicate between the Godot Editor and a running game instance in real-time. Below are comprehensive use cases and examples.

## Development & Debugging

### 1. Real-Time Game State Inspection
**Use**: Monitor and inspect game state while the game is running without stopping or restarting.

**Example**:
```csharp
// Game sends current player state to Editor
public class PlayerStateMessage : RuntimeMessage
{
    public override string Command => "player_state";
    public int Health { get; set; }
    public int Experience { get; set; }
    public string CurrentLevel { get; set; }

    public override Array Serialize() => new Array { Health, Experience, CurrentLevel };
    public override void Deserialize(Array data) 
    { 
        Health = data[0].AsInt32();
        Experience = data[1].AsInt32();
        CurrentLevel = data[2].AsString();
    }
}

// Send periodically or on demand
RuntimeBridge.Instance.Send(new PlayerStateMessage 
{ 
    Health = player.Health,
    Experience = player.Experience,
    CurrentLevel = GetTree().CurrentScene.Name
});
```

**Benefits**:
- Debug complex game states without console logs
- Verify game logic is working as expected
- Catch state inconsistencies early

---

### 2. Live Configuration Testing
**Use**: Modify game parameters from the Editor without recompiling or restarting.

**Example**:
```csharp
public class ConfigUpdateMessage : RuntimeMessage
{
    public override string Command => "config_update";
    public string Key { get; set; }
    public string Value { get; set; }

    public override Array Serialize() => new Array { Key, Value };
    public override void Deserialize(Array data) 
    { 
        Key = data[0].AsString();
        Value = data[1].AsString();
    }
}

// In game script
if (RuntimeBridge.Instance.TryDeserialize(cmd, data, out ConfigUpdateMessage cfgMsg))
{
    switch(cfgMsg.Key)
    {
        case "player_speed":
            player.MoveSpeed = float.Parse(cfgMsg.Value);
            break;
        case "jump_force":
            player.JumpForce = float.Parse(cfgMsg.Value);
            break;
    }
}
```

**Benefits**:
- Test game balance instantly
- Tweak gameplay parameters without rebuilding
- A/B test different settings quickly

---

### 3. Performance Profiling
**Use**: Collect performance metrics from the running game and display them in the Editor.

**Example**:
```csharp
public class PerformanceMetricsMessage : RuntimeMessage
{
    public override string Command => "perf_metrics";
    public float FPS { get; set; }
    public float MemoryUsageMB { get; set; }
    public int ActiveNodes { get; set; }

    public override Array Serialize() => new Array { FPS, MemoryUsageMB, ActiveNodes };
    public override void Deserialize(Array data) 
    { 
        FPS = (float)data[0].AsDouble();
        MemoryUsageMB = (float)data[1].AsDouble();
        ActiveNodes = data[2].AsInt32();
    }
}

// Send metrics every frame
RuntimeBridge.Instance.Send(new PerformanceMetricsMessage
{
    FPS = Engine.GetFramesPerSecond(),
    MemoryUsageMB = OS.GetStaticMemoryUsage() / 1024f / 1024f,
    ActiveNodes = GetTree().GetNodeCount()
});
```

**Benefits**:
- Identify performance bottlenecks
- Monitor memory leaks in real-time
- Track frame rate during gameplay

---

### 4. Event Logging & Analysis
**Use**: Send game events to the Editor for real-time logging and analysis.

**Example**:
```csharp
public class GameEventMessage : RuntimeMessage
{
    public override string Command => "game_event";
    public string EventType { get; set; }
    public string Details { get; set; }
    public float Timestamp { get; set; }

    public override Array Serialize() => new Array { EventType, Details, Timestamp };
    public override void Deserialize(Array data) 
    { 
        EventType = data[0].AsString();
        Details = data[1].AsString();
        Timestamp = (float)data[2].AsDouble();
    }
}

// Log player actions
RuntimeBridge.Instance.Send(new GameEventMessage
{
    EventType = "player_damage",
    Details = $"Took {damage} damage from {source}",
    Timestamp = Time.GetTicksMsec() / 1000f
});
```

**Benefits**:
- Build comprehensive gameplay logs
- Analyze player interactions
- Identify gameplay patterns

---

## QA & Testing

### 5. Remote Test Execution
**Use**: Trigger tests from the Editor while the game is running.

**Example**:
```csharp
public class RunTestMessage : RuntimeMessage
{
    public override string Command => "run_test";
    public string TestName { get; set; }

    public override Array Serialize() => new Array { TestName };
    public override void Deserialize(Array data) => TestName = data[0].AsString();
}

// Game side
if (RuntimeBridge.Instance.TryDeserialize(cmd, data, out RunTestMessage testMsg))
{
    switch(testMsg.TestName)
    {
        case "collision":
            RunCollisionTests();
            break;
        case "ai":
            RunAITests();
            break;
    }
}
```

**Benefits**:
- Execute automated tests without stopping the game
- Validate features during gameplay
- Rapid QA iteration

---

### 6. Unit Test Results Reporting
**Use**: Send test results from game to Editor for analysis.

**Example**:
```csharp
public class TestResultMessage : RuntimeMessage
{
    public override string Command => "test_result";
    public string TestName { get; set; }
    public bool Passed { get; set; }
    public string ErrorMessage { get; set; }

    public override Array Serialize() => new Array { TestName, Passed, ErrorMessage };
    public override void Deserialize(Array data) 
    { 
        TestName = data[0].AsString();
        Passed = data[1].AsBool();
        ErrorMessage = data[2].AsString();
    }
}
```

**Benefits**:
- Automated test reporting
- Track test coverage
- Identify regressions

---

## Gameplay Assistance

### 7. Remote Cheats & Debug Commands
**Use**: Execute cheat codes from the Editor to test gameplay scenarios.

**Example**:
```csharp
public class CheatCommandMessage : RuntimeMessage
{
    public override string Command => "cheat";
    public string CheatCode { get; set; }
    public string Param { get; set; }

    public override Array Serialize() => new Array { CheatCode, Param };
    public override void Deserialize(Array data) 
    { 
        CheatCode = data[0].AsString();
        Param = data[1].AsString();
    }
}

// Game side
if (RuntimeBridge.Instance.TryDeserialize(cmd, data, out CheatCommandMessage cheat))
{
    switch(cheat.CheatCode)
    {
        case "godmode":
            player.IsInvulnerable = true;
            break;
        case "give_item":
            inventory.AddItem(cheat.Param);
            break;
        case "level_up":
            player.GainExperience(int.Parse(cheat.Param));
            break;
    }
}
```

**Benefits**:
- Test endgame content without grinding
- Skip to specific scenarios
- Debug gameplay mechanics

---

### 8. Scene/Level Testing
**Use**: Load and test different scenes directly from the Editor.

**Example**:
```csharp
public class LoadSceneMessage : RuntimeMessage
{
    public override string Command => "load_scene";
    public string ScenePath { get; set; }

    public override Array Serialize() => new Array { ScenePath };
    public override void Deserialize(Array data) => ScenePath = data[0].AsString();
}

// Game side
if (RuntimeBridge.Instance.TryDeserialize(cmd, data, out LoadSceneMessage sceneMsg))
{
    GetTree().ChangeSceneToFile(sceneMsg.ScenePath);
}
```

**Benefits**:
- Jump directly to levels for testing
- Validate scene loading
- Test scene transitions

---

## Data Analysis & Monitoring

### 9. Gameplay Analytics Collection
**Use**: Gather player behavior data during gameplay for analysis.

**Example**:
```csharp
public class AnalyticsMessage : RuntimeMessage
{
    public override string Command => "analytics";
    public string EventName { get; set; }
    public Dictionary<string, Variant> Data { get; set; }

    public override Array Serialize() 
    { 
        var arr = new Array { EventName };
        arr.Add(GD.VarToStr(Data));
        return arr;
    }
    
    public override void Deserialize(Array data) 
    { 
        EventName = data[0].AsString();
        Data = GD.StrToVar(data[1].AsString()) as Dictionary<string, Variant>;
    }
}
```

**Benefits**:
- Track player progression
- Understand player preferences
- Make data-driven balance decisions

---

### 10. Network Debugging
**Use**: Monitor network traffic and latency during gameplay.

**Example**:
```csharp
public class NetworkStatsMessage : RuntimeMessage
{
    public override string Command => "network_stats";
    public float Ping { get; set; }
    public int PacketLoss { get; set; }
    public float Bandwidth { get; set; }

    public override Array Serialize() => new Array { Ping, PacketLoss, Bandwidth };
    public override void Deserialize(Array data) 
    { 
        Ping = (float)data[0].AsDouble();
        PacketLoss = data[1].AsInt32();
        Bandwidth = (float)data[2].AsDouble();
    }
}
```

**Benefits**:
- Debug network issues
- Optimize network usage
- Monitor connection quality

---

### 11. Asset Hot-Reloading
**Use**: Reload assets from Editor while game is running.

**Example**:
```csharp
public class ReloadAssetMessage : RuntimeMessage
{
    public override string Command => "reload_asset";
    public string AssetPath { get; set; }

    public override Array Serialize() => new Array { AssetPath };
    public override void Deserialize(Array data) => AssetPath = data[0].AsString();
}

// Game side - watch for reload messages
if (RuntimeBridge.Instance.TryDeserialize(cmd, data, out ReloadAssetMessage reloadMsg))
{
    var asset = GD.Load(reloadMsg.AssetPath);
    // Update references to the reloaded asset
}
```

**Benefits**:
- Iterate on assets without restarting
- See changes in real-time
- Faster art pipeline

---

## Educational & Learning

### 12. Algorithm Visualization
**Use**: Visualize algorithms running in the game by sending step-by-step data.

**Example**:
```csharp
public class AlgoStepMessage : RuntimeMessage
{
    public override string Command => "algo_step";
    public int Step { get; set; }
    public string Description { get; set; }
    public Array Data { get; set; }

    public override Array Serialize() => new Array { Step, Description, GD.VarToStr(Data) };
    public override void Deserialize(Array data) 
    { 
        Step = data[0].AsInt32();
        Description = data[1].AsString();
        Data = GD.StrToVar(data[2].AsString()) as Array;
    }
}
```

**Benefits**:
- Learn how algorithms work
- Debug pathfinding, sorting, etc.
- Educational tool for students

---

### 13. Game State Tutorials
**Use**: Record and replay game state for tutorials.

**Example**:
```csharp
public class TutorialStateMessage : RuntimeMessage
{
    public override string Command => "tutorial_state";
    public string StepName { get; set; }
    public string Instruction { get; set; }

    public override Array Serialize() => new Array { StepName, Instruction };
    public override void Deserialize(Array data) 
    { 
        StepName = data[0].AsString();
        Instruction = data[1].AsString();
    }
}
```

**Benefits**:
- Create interactive tutorials
- Guide players through gameplay
- Track tutorial progress

---

## Specialized Use Cases

### 14. AI/NPC Behavior Debugging
**Use**: Monitor AI state and decision-making in real-time.

**Example**:
```csharp
public class AIDebugMessage : RuntimeMessage
{
    public override string Command => "ai_debug";
    public string NPCName { get; set; }
    public string CurrentState { get; set; }
    public Vector2 Target { get; set; }

    public override Array Serialize() => new Array { NPCName, CurrentState, Target.X, Target.Y };
    public override void Deserialize(Array data) 
    { 
        NPCName = data[0].AsString();
        CurrentState = data[1].AsString();
        Target = new Vector2((float)data[2].AsDouble(), (float)data[3].AsDouble());
    }
}
```

**Benefits**:
- Debug AI pathfinding
- Monitor state machines
- Verify behavior trees

---

### 15. Physics Debugging
**Use**: Monitor physics interactions and collisions.

**Example**:
```csharp
public class PhysicsDebugMessage : RuntimeMessage
{
    public override string Command => "physics_debug";
    public string ObjectName { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }

    public override Array Serialize() => new Array { ObjectName, Position.X, Position.Y, Velocity.X, Velocity.Y };
    public override void Deserialize(Array data) 
    { 
        ObjectName = data[0].AsString();
        Position = new Vector2((float)data[1].AsDouble(), (float)data[2].AsDouble());
        Velocity = new Vector2((float)data[3].AsDouble(), (float)data[4].AsDouble());
    }
}
```

**Benefits**:
- Debug collision issues
- Verify physics calculations
- Monitor object velocities

---

### 16. Live Debugging Tools
**Use**: Create in-game debugging overlays controlled from the Editor.

**Example**:
```csharp
public class DebugOverlayMessage : RuntimeMessage
{
    public override string Command => "debug_overlay";
    public string OverlayType { get; set; }
    public bool Enabled { get; set; }

    public override Array Serialize() => new Array { OverlayType, Enabled };
    public override void Deserialize(Array data) 
    { 
        OverlayType = data[0].AsString();
        Enabled = data[1].AsBool();
    }
}

// Game side - toggle debug overlays
if (RuntimeBridge.Instance.TryDeserialize(cmd, data, out DebugOverlayMessage overlayMsg))
{
    switch(overlayMsg.OverlayType)
    {
        case "collision":
            debugOverlay.ShowCollisions = overlayMsg.Enabled;
            break;
        case "pathfinding":
            debugOverlay.ShowPathfinding = overlayMsg.Enabled;
            break;
    }
}
```

**Benefits**:
- Visual debugging tools
- Toggle debug info without code changes
- Real-time visualization

---

## Workflow Optimization

### 17. Automated Build Testing
**Use**: Run automated checks on builds before deployment.

**Example**:
```csharp
public class BuildCheckMessage : RuntimeMessage
{
    public override string Command => "build_check";
    public string CheckType { get; set; }

    public override Array Serialize() => new Array { CheckType };
    public override void Deserialize(Array data) => CheckType = data[0].AsString();
}
```

**Benefits**:
- Validate build integrity
- Run pre-deployment checks
- Ensure quality standards

---

### 18. Cross-Platform Testing
**Use**: Test game on multiple platforms while maintaining Editor connection.

**Example**:
```csharp
public class PlatformInfoMessage : RuntimeMessage
{
    public override string Command => "platform_info";
    public string OSName { get; set; }
    public Vector2 ScreenResolution { get; set; }

    public override Array Serialize() => new Array { OSName, ScreenResolution.X, ScreenResolution.Y };
    public override void Deserialize(Array data) 
    { 
        OSName = data[0].AsString();
        ScreenResolution = new Vector2((float)data[1].AsDouble(), (float)data[2].AsDouble());
    }
}
```

**Benefits**:
- Verify cross-platform compatibility
- Detect platform-specific issues
- Monitor platform metrics

---

## Summary

This addon is ideal for developers who need:
- **Real-time debugging** without stopping the game
- **Live parameter tuning** for gameplay balance
- **Automated testing** and QA workflows
- **Performance monitoring** and optimization
- **Data collection** for analytics
- **Educational tools** for learning game development

The type-safe message system ensures reliability and maintainability across complex communication scenarios.
