# Editor Runtime Data

A Godot addon that facilitates bidirectional communication between the Godot Editor and the running game instance (Runtime).

## Quick Start

1.  Enable the plugin in **Project Settings > Plugins**.
2.  Run your game (F5).
3.  Open the **Runtime Data** dock in the Editor.
4.  Click **Ping Game** or type a message and click **Send**.

## Installation

1.  Copy the `addons/editorruntime` folder to your project's `addons/` directory.
2.  Build the solution (if using C#).
3.  Enable the plugin in Project Settings.

## API Reference

### Creating Custom Messages

Inherit from `RuntimeMessage` in `Aspecty.EditorRuntime.Common.Messages`:

```csharp
public class MyCustomMessage : RuntimeMessage
{
    public override string Command => "my_custom_command";
    public int Value { get; set; }

    public override Godot.Collections.Array Serialize()
    {
        return new Godot.Collections.Array { Value };
    }

    public override void Deserialize(Godot.Collections.Array data)
    {
        Value = data[0].AsInt32();
    }
}
```

### Creating Custom Message Handlers

Implement `IMessageHandler<T>` to automatically handle specific message types:

```csharp
using Godot;
using Aspecty.EditorRuntime.Runtime;
using YourNamespace.Messages;

public class MyCustomHandler : IMessageHandler<MyCustomMessage>
{
    public void Handle(MyCustomMessage message, RuntimeBridge bridge)
    {
        GD.Print($"Received value: {message.Value}");
        // Optionally send a response
        bridge.Send(new ResponseMessage { Result = "OK" });
    }
}
```

**Auto-Discovery**: Handlers are automatically discovered and registered via reflection. Just implement the interface and the system will find it!

Or manually register custom handlers:

```csharp
public override void _Ready()
{
    RuntimeBridge.Instance?.RegisterHandler(new MyCustomHandler());
}
```

### Sending from Game

```csharp
RuntimeBridge.Instance.Send(new MyCustomMessage { Value = 42 });
```

### Receiving in Game (Legacy Signal Approach)

```csharp
private void OnMessageReceived(string command, Godot.Collections.Array data)
{
    if (RuntimeBridge.Instance.TryDeserialize(command, data, out MyCustomMessage msg))
    {
        GD.Print($"Received value: {msg.Value}");
    }
}
```

### Sending from Editor

```csharp
// In your EditorPlugin or Tool script
_debuggerPlugin.Broadcast(new MyCustomMessage { Value = 100 });
```

## Architecture

```
Editor                          Game
┌──────────────────┐           ┌──────────────────┐
│ EditorRuntimePlugin          │ RuntimeBridge    │
│ (Tool Plugin)                │ (Autoload)       │
├──────────────────┤           ├──────────────────┤
│ RuntimeDebugger  │◄─────────►│ EngineDebugger   │
│ Plugin           │  Messages │ MessageCapture   │
│                  │           │                  │
│ Command Buttons  │           │ MessageHandlers  │
│ Tabs & Logs      │           │ (Auto-Discovery) │
└──────────────────┘           └──────────────────┘
```

## Built-In Message Handlers

- **PingMessageHandler**: Automatically responds to ping messages
- **TextMessageHandler**: Forwards text commands as RuntimeBridge signals

## Troubleshooting

*   **Messages not received?** Ensure you are running a **Debug** build. The bridge is disabled in Release builds.
*   **Plugin not working?** Try disabling and re-enabling the plugin in Project Settings after building the solution.
*   **Custom handler not called?** Ensure it's in the same assembly and implements `IMessageHandler<T>`.
