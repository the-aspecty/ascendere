#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Ascendere.EditorRuntime.Editor;

namespace Ascendere.EditorRuntime
{
    [Tool]
    [Plugin(
        "Editor Runtime",
        Description = "Enables communication between the Godot Editor and running game instances for debugging and data inspection."
    )]
    public partial class EditorRuntimePlugin : EditorPlugin
    {
        private RuntimeDebuggerPlugin _debuggerPlugin;
        private Control _dock;
        private Label _statusLabel;
        private TabContainer _tabContainer;
        private Dictionary<string, RichTextLabel> _commandLogs = new();
        private Button _pingButton;
        private LineEdit _editorMessageInput;
        private Button _sendToGameButton;
        private FlowContainer _commandButtonsPanel;
        private HashSet<string> _addedCommandButtons = new();
        private RichTextLabel _availableMessagesLabel;
        private Dictionary<string, Button> _commandButtons = new();
        private Dictionary<string, Button> _clearButtons = new();

        public override void _EnterTree()
        {
            // Register Debugger Plugin
            _debuggerPlugin = new RuntimeDebuggerPlugin();
            AddDebuggerPlugin(_debuggerPlugin);
            _debuggerPlugin.Connect(
                RuntimeDebuggerPlugin.SignalName.MessageReceived,
                new Callable(this, MethodName.OnMessageReceived)
            );

            // Add Autoload
            AddAutoloadSingleton(
                "RuntimeBridge",
                "res://addons/editorruntime/Scripts/Runtime/RuntimeBridge.cs"
            );

            // Create Dock UI
            _dock = new VBoxContainer();
            _dock.Name = "Runtime Data";

            _statusLabel = new Label();
            _statusLabel.Text = "Status: Ready";
            _dock.AddChild(_statusLabel);

            _pingButton = new Button();
            _pingButton.Text = "Ping Game";
            _pingButton.Connect(
                Button.SignalName.Pressed,
                new Callable(this, MethodName.OnPingPressed)
            );
            _dock.AddChild(_pingButton);

            // Editor to Game Input
            var hBox = new HBoxContainer();
            _dock.AddChild(hBox);

            _editorMessageInput = new LineEdit();
            _editorMessageInput.PlaceholderText = "Command to Game...";
            _editorMessageInput.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            hBox.AddChild(_editorMessageInput);

            _sendToGameButton = new Button();
            _sendToGameButton.Text = "Send";
            _sendToGameButton.Connect(
                Button.SignalName.Pressed,
                new Callable(this, MethodName.OnSendToGamePressed)
            );
            hBox.AddChild(_sendToGameButton);

            // Quick Command Buttons Panel
            var commandsLabel = new Label();
            commandsLabel.Text = "Quick Commands:";
            _dock.AddChild(commandsLabel);

            _commandButtonsPanel = new FlowContainer();
            _commandButtonsPanel.CustomMinimumSize = new Vector2(0, 40);
            _dock.AddChild(_commandButtonsPanel);

            // Scan All button
            var scanButton = new Button();
            scanButton.Text = "Scan All Messages";
            scanButton.Connect(
                Button.SignalName.Pressed,
                new Callable(this, MethodName.OnScanAllPressed)
            );
            _dock.AddChild(scanButton);

            // Available messages display
            var messagesLabel = new Label();
            messagesLabel.Text = "Available Messages:";
            _dock.AddChild(messagesLabel);

            _availableMessagesLabel = new RichTextLabel();
            _availableMessagesLabel.CustomMinimumSize = new Vector2(0, 100);
            _availableMessagesLabel.ScrollFollowing = true;
            _dock.AddChild(_availableMessagesLabel);

            // Tab Container for command logs
            _tabContainer = new TabContainer();
            _tabContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _dock.AddChild(_tabContainer);

            AddControlToDock(DockSlot.RightUl, _dock);
        }

        public override void _ExitTree()
        {
            // Cleanup Dock
            if (_dock != null)
            {
                RemoveControlFromDocks(_dock);
                _dock.QueueFree();
                _dock = null;
            }

            // Cleanup Debugger Plugin
            if (_debuggerPlugin != null)
            {
                if (
                    _debuggerPlugin.IsConnected(
                        RuntimeDebuggerPlugin.SignalName.MessageReceived,
                        new Callable(this, MethodName.OnMessageReceived)
                    )
                )
                {
                    _debuggerPlugin.Disconnect(
                        RuntimeDebuggerPlugin.SignalName.MessageReceived,
                        new Callable(this, MethodName.OnMessageReceived)
                    );
                }
                RemoveDebuggerPlugin(_debuggerPlugin);
                _debuggerPlugin = null;
            }

            // Cleanup Autoload
            RemoveAutoloadSingleton("RuntimeBridge");
        }

        private void OnPingPressed()
        {
            if (_debuggerPlugin != null)
            {
                _debuggerPlugin.Broadcast(new PingMessage { Text = "Ping from Editor" });
                Log("Ping", "Sent: Ping from Editor");
            }
        }

        private void OnSendToGamePressed()
        {
            if (_debuggerPlugin != null && !string.IsNullOrEmpty(_editorMessageInput.Text))
            {
                string cmd = _editorMessageInput.Text;
                _debuggerPlugin.Broadcast(new TextMessage(cmd));
                Log("Text", $"Sent: {cmd}");
                _editorMessageInput.Text = "";
            }
        }

        private void OnMessageReceived(string message, Godot.Collections.Array data, int sessionId)
        {
            string displayName = message;

            // If an example announces its supported commands, create tabs for each command now
            if (_debuggerPlugin.TryDeserialize(message, data, out AvailableCommandsMessage avail))
            {
                foreach (var cmd in avail.Commands)
                {
                    EnsureTabExists(cmd);
                    EnsureCommandButton(cmd);
                    Log(cmd, $"Announced by session {sessionId}");
                }
                return;
            }

            if (_debuggerPlugin.TryDeserialize(message, data, out PingMessage pingMsg))
            {
                displayName = "Ping";
                Log(displayName, $"Received [{sessionId}]: {pingMsg.Text}");
            }
            else if (_debuggerPlugin.TryDeserialize(message, data, out TextMessage textMsg))
            {
                displayName = "Text";
                Log(displayName, $"Received [{sessionId}]: {textMsg.Text}");
            }
            else
            {
                string dataStr = data != null ? data.ToString() : "null";
                Log(displayName, $"Received [{sessionId}]: {dataStr}");
            }
        }

        private void Log(string commandName, string msg)
        {
            EnsureTabExists(commandName);

            if (
                _commandLogs.TryGetValue(commandName, out var logLabel)
                && logLabel != null
                && IsInstanceValid(logLabel)
            )
            {
                logLabel.AddText($"{DateTime.Now:HH:mm:ss}: {msg}\n");
            }
            GD.Print($"[EditorRuntime] {msg}");
        }

        private void EnsureTabExists(string commandName)
        {
            if (_commandLogs.ContainsKey(commandName))
                return;

            // Create container for this command's tab
            var tabVBox = new VBoxContainer();
            tabVBox.Name = commandName;

            // Clear button
            var clearButton = new Button();
            clearButton.Text = "Clear Log";
            clearButton.CustomMinimumSize = new Vector2(0, 30);
            _clearButtons[commandName] = clearButton;
            clearButton.Connect(
                Button.SignalName.Pressed,
                new Callable(this, MethodName.OnClearLogPressed)
            );
            tabVBox.AddChild(clearButton);

            // Log display
            var logLabel = new RichTextLabel();
            logLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            logLabel.ScrollFollowing = true;
            tabVBox.AddChild(logLabel);

            _tabContainer.AddChild(tabVBox);
            _commandLogs[commandName] = logLabel;
        }

        private void EnsureCommandButton(string commandName)
        {
            if (_addedCommandButtons.Contains(commandName))
                return;

            var btn = new Button();
            btn.Text = commandName;
            btn.TooltipText = $"Send '{commandName}' command to game";
            _commandButtons[commandName] = btn;
            btn.Connect(
                Button.SignalName.Pressed,
                new Callable(this, MethodName.OnQuickCommandPressed)
            );
            _commandButtonsPanel.AddChild(btn);
            _addedCommandButtons.Add(commandName);
        }

        private void OnQuickCommandPressed()
        {
            // Find which button was pressed by checking which one is currently pressed
            foreach (var kvp in _commandButtons)
            {
                var command = kvp.Key;
                var btn = kvp.Value;
                if (btn != null && IsInstanceValid(btn) && btn.IsPressed())
                {
                    if (_debuggerPlugin != null)
                    {
                        _debuggerPlugin.Broadcast(new TextMessage(command));
                        Log("Text", $"Sent: {command}");
                    }
                    return;
                }
            }
        }

        private void OnClearLogPressed()
        {
            // Find which button was pressed by checking which one is currently pressed
            foreach (var kvp in _clearButtons)
            {
                var commandName = kvp.Key;
                var btn = kvp.Value;
                if (btn != null && IsInstanceValid(btn) && btn.IsPressed())
                {
                    ClearLog(commandName);
                    return;
                }
            }
        }

        private void OnScanAllPressed()
        {
            _availableMessagesLabel.Clear();
            _availableMessagesLabel.AddText("Scanning for RuntimeMessage implementations...\n\n");

            try
            {
                var runtimeMessageType = typeof(RuntimeMessage);
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var messageTypes = new List<Type>();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var types = assembly
                            .GetTypes()
                            .Where(t =>
                                !t.IsAbstract
                                && !t.IsInterface
                                && t.IsAssignableTo(runtimeMessageType)
                                && t != runtimeMessageType
                            )
                            .OrderBy(t => t.Name);

                        messageTypes.AddRange(types);
                    }
                    catch
                    {
                        // Skip assemblies that can't be scanned
                    }
                }

                if (messageTypes.Count == 0)
                {
                    _availableMessagesLabel.AddText(
                        "[color=yellow]No RuntimeMessage implementations found[/color]\n"
                    );
                    return;
                }

                _availableMessagesLabel.AddText(
                    $"[color=lime]Found {messageTypes.Count} message type(s):[/color]\n\n"
                );

                foreach (var type in messageTypes)
                {
                    _availableMessagesLabel.AddText(
                        $"[color=cyan]{type.Name}[/color] ({type.Namespace})\n"
                    );

                    // Try to get the Command property
                    try
                    {
                        var instance = Activator.CreateInstance(type) as RuntimeMessage;
                        if (instance != null)
                        {
                            _availableMessagesLabel.AddText(
                                $"  → Command: [color=yellow]{instance.Command}[/color]\n"
                            );
                        }
                    }
                    catch
                    {
                        _availableMessagesLabel.AddText(
                            $"  → Command: [color=orange](unable to instantiate)[/color]\n"
                        );
                    }
                }

                GD.Print(
                    $"[EditorRuntime] Scanned {messageTypes.Count} RuntimeMessage implementations"
                );
            }
            catch (Exception ex)
            {
                _availableMessagesLabel.AddText(
                    $"[color=red]Error scanning messages: {ex.Message}[/color]\n"
                );
                GD.PrintErr($"[EditorRuntime] Scan error: {ex}");
            }
        }

        private void ClearLog(string commandName)
        {
            if (
                _commandLogs.TryGetValue(commandName, out var logLabel)
                && logLabel != null
                && IsInstanceValid(logLabel)
            )
            {
                logLabel.Clear();
            }
        }
    }
}
#endif
