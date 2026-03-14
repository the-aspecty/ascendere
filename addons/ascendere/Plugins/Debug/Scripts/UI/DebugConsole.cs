using System;
using System.Collections.Generic;
using System.Linq;
using Ascendere.Log;
using Godot;

namespace Ascendere.Debug.UI
{
    /// <summary>
    /// Enhanced debug console with command history, autocomplete, and extensive commands
    /// </summary>
    public partial class DebugConsole : PanelContainer
    {
        private RichTextLabel _logDisplay;
        private LineEdit _commandInput;
        private readonly List<string> _commandHistory = new();
        private int _historyIndex = -1;
        private readonly ILogService _log;
        private readonly Dictionary<string, ConsoleCommand> _commands = new();
        private Label _suggestionsLabel;

        public DebugConsole(ILogService logService)
        {
            _log = logService;
            
            // Set up as overlay at bottom of screen
            SetAnchorsPreset(LayoutPreset.BottomWide);
            OffsetTop = -400;
            OffsetBottom = 0;
            ZIndex = 100; // Ensure console is on top
            MouseFilter = MouseFilterEnum.Stop; // Block input to things behind

            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 5);
            AddChild(vbox);

            var scroll = new ScrollContainer();
            scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
            scroll.FollowFocus = true;
            vbox.AddChild(scroll);

            _logDisplay = new RichTextLabel();
            _logDisplay.BbcodeEnabled = true;
            _logDisplay.ScrollFollowing = true;
            _logDisplay.SizeFlagsVertical = SizeFlags.ExpandFill;
            _logDisplay.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _logDisplay.FitContent = true;
            scroll.AddChild(_logDisplay);

            _suggestionsLabel = new Label();
            _suggestionsLabel.AddThemeColorOverride("font_color", Colors.Yellow);
            _suggestionsLabel.AddThemeConstantOverride("outline_size", 1);
            _suggestionsLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
            _suggestionsLabel.Visible = false;
            vbox.AddChild(_suggestionsLabel);

            _commandInput = new LineEdit();
            _commandInput.PlaceholderText = "Enter command... (Tab for autocomplete, Up/Down for history)";
            _commandInput.TextSubmitted += OnCommandSubmitted;
            _commandInput.TextChanged += OnTextChanged;
            vbox.AddChild(_commandInput);

            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.08f, 0.08f, 0.1f, 0.98f);
            style.SetBorderWidthAll(3);
            style.BorderColor = new Color(0.3f, 0.6f, 0.8f, 1.0f);
            style.SetCornerRadiusAll(0);
            style.SetExpandMarginAll(2);
            AddThemeStyleboxOverride("panel", style);

            RegisterCommands();
            AddLogEntry("=== Debug Console Ready ===", Colors.Cyan, LogType.Info);
            AddLogEntry("Type 'help' for available commands", Colors.LightGray, LogType.Info);
            AddLogEntry("Use Tab for autocomplete, Up/Down for history", Colors.Gray, LogType.Info);
        }
        
        public override void _Input(InputEvent @event)
        {
            if (!Visible || !_commandInput.HasFocus())
                return;
                
            if (@event is InputEventKey key && key.Pressed && !key.Echo)
            {
                switch (key.Keycode)
                {
                    case Key.Up:
                        NavigateHistory(-1);
                        GetViewport().SetInputAsHandled();
                        break;
                    case Key.Down:
                        NavigateHistory(1);
                        GetViewport().SetInputAsHandled();
                        break;
                    case Key.Tab:
                        AutoComplete();
                        GetViewport().SetInputAsHandled();
                        break;
                }
            }
        }
        
        private void RegisterCommands()
        {
            _commands["help"] = new ConsoleCommand("help", "Show all available commands", ExecuteHelp);
            _commands["clear"] = new ConsoleCommand("clear", "Clear console output", ExecuteClear);
            _commands["timescale"] = new ConsoleCommand("timescale <value>", "Set game time scale (0.0-10.0)", ExecuteTimescale);
            _commands["pause"] = new ConsoleCommand("pause", "Pause the game", ExecutePause);
            _commands["resume"] = new ConsoleCommand("resume", "Resume the game", ExecuteResume);
            _commands["quit"] = new ConsoleCommand("quit", "Exit the application", ExecuteQuit);
            _commands["fps"] = new ConsoleCommand("fps", "Show current FPS", ExecuteFps);
            _commands["mem"] = new ConsoleCommand("mem", "Show memory usage", ExecuteMemory);
            _commands["scene"] = new ConsoleCommand("scene", "Show current scene info", ExecuteScene);
            _commands["list"] = new ConsoleCommand("list [filter]", "List all nodes in scene tree", ExecuteList);
            _commands["reload"] = new ConsoleCommand("reload", "Reload current scene", ExecuteReload);
            _commands["gc"] = new ConsoleCommand("gc", "Force garbage collection", ExecuteGC);
            _commands["vsync"] = new ConsoleCommand("vsync <on|off>", "Toggle VSync", ExecuteVSync);
            _commands["fullscreen"] = new ConsoleCommand("fullscreen", "Toggle fullscreen mode", ExecuteFullscreen);
            _commands["history"] = new ConsoleCommand("history", "Show command history", ExecuteHistory);
        }
        
        private void NavigateHistory(int direction)
        {
            if (_commandHistory.Count == 0)
                return;
                
            if (_historyIndex == -1)
            {
                _historyIndex = _commandHistory.Count - 1;
            }
            else
            {
                _historyIndex += direction;
                _historyIndex = Mathf.Clamp(_historyIndex, 0, _commandHistory.Count - 1);
            }
            
            if (_historyIndex >= 0 && _historyIndex < _commandHistory.Count)
            {
                _commandInput.Text = _commandHistory[_historyIndex];
                _commandInput.CaretColumn = _commandInput.Text.Length;
            }
        }
        
        private void AutoComplete()
        {
            string input = _commandInput.Text.ToLower();
            if (string.IsNullOrWhiteSpace(input))
                return;
                
            var matches = _commands.Keys.Where(cmd => cmd.StartsWith(input)).ToList();
            
            if (matches.Count == 1)
            {
                _commandInput.Text = matches[0];
                _commandInput.CaretColumn = _commandInput.Text.Length;
                _suggestionsLabel.Visible = false;
            }
            else if (matches.Count > 1)
            {
                _suggestionsLabel.Text = $"Suggestions: {string.Join(", ", matches)}";
                _suggestionsLabel.Visible = true;
            }
        }
        
        private void OnTextChanged(string newText)
        {
            if (string.IsNullOrWhiteSpace(newText))
            {
                _suggestionsLabel.Visible = false;
                return;
            }
                
            var input = newText.ToLower().Split(' ')[0];
            var matches = _commands.Keys.Where(cmd => cmd.StartsWith(input) && cmd != input).Take(5).ToList();
            
            if (matches.Count > 0)
            {
                _suggestionsLabel.Text = $"💡 {string.Join(", ", matches)}";
                _suggestionsLabel.Visible = true;
            }
            else
            {
                _suggestionsLabel.Visible = false;
            }
        }

        public void AddLog(string message, Color color, LogType type)
        {
            AddLogEntry(message, color, type);
        }

        private void AddLogEntry(string message, Color color, LogType type)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string colorHex = color.ToHtml();
            string prefix =
                type == LogType.Error ? "[ERROR]"
                : type == LogType.Warning ? "[WARN]"
                : "[INFO]";

            _logDisplay.AppendText(
                $"[color=gray]{timestamp}[/color] [color={colorHex}]{prefix}[/color] {message}\n"
            );
        }

        private void OnCommandSubmitted(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            _commandHistory.Add(command);
            _historyIndex = -1;

            AddLogEntry($"> {command}", Colors.Cyan, LogType.Info);
            ExecuteCommand(command);

            _commandInput.Clear();
            _suggestionsLabel.Visible = false;
        }

        private void ExecuteCommand(string command)
        {
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;

            var cmd = parts[0].ToLower();

            if (_commands.TryGetValue(cmd, out var consoleCommand))
            {
                consoleCommand.Execute(parts);
            }
            else
            {
                AddLogEntry($"Unknown command: '{cmd}'. Type 'help' for available commands.", Colors.Red, LogType.Error);
            }
        }
        
        private void ExecuteHelp(string[] args)
        {
            AddLogEntry("=== AVAILABLE COMMANDS ===", Colors.Yellow, LogType.Info);
            foreach (var cmd in _commands.Values.OrderBy(c => c.Name))
            {
                AddLogEntry($"  {cmd.Name,-20} - {cmd.Description}", Colors.White, LogType.Info);
            }
        }
        
        private void ExecuteClear(string[] args)
        {
            _logDisplay.Clear();
            AddLogEntry("Console cleared", Colors.Green, LogType.Info);
        }
        
        private void ExecuteTimescale(string[] args)
        {
            if (args.Length > 1 && float.TryParse(args[1], out float scale))
            {
                scale = Mathf.Clamp(scale, 0f, 10f);
                Engine.TimeScale = scale;
                AddLogEntry($"Time scale set to {scale:F2}", Colors.Green, LogType.Info);
            }
            else
            {
                AddLogEntry($"Current time scale: {Engine.TimeScale:F2}", Colors.White, LogType.Info);
                AddLogEntry("Usage: timescale <0.0-10.0>", Colors.Gray, LogType.Info);
            }
        }
        
        private void ExecutePause(string[] args)
        {
            GetTree().Paused = true;
            AddLogEntry("⏸ Game paused", Colors.Yellow, LogType.Info);
        }
        
        private void ExecuteResume(string[] args)
        {
            GetTree().Paused = false;
            AddLogEntry("▶ Game resumed", Colors.Green, LogType.Info);
        }
        
        private void ExecuteQuit(string[] args)
        {
            AddLogEntry("Exiting application...", Colors.Red, LogType.Info);
            GetTree().Quit();
        }
        
        private void ExecuteFps(string[] args)
        {
            double fps = Engine.GetFramesPerSecond();
            Color color = fps >= 55 ? Colors.Green : fps >= 30 ? Colors.Yellow : Colors.Red;
            AddLogEntry($"FPS: {fps:F1}", color, LogType.Info);
        }
        
        private void ExecuteMemory(string[] args)
        {
            var staticMem = OS.GetStaticMemoryUsage();
            var memMB = staticMem / (1024.0 * 1024.0);
            AddLogEntry($"Memory Usage: {memMB:F2} MB ({staticMem:N0} bytes)", Colors.Cyan, LogType.Info);
        }
        
        private void ExecuteScene(string[] args)
        {
            var scene = GetTree().CurrentScene;
            if (scene != null)
            {
                AddLogEntry($"Current Scene: {scene.Name}", Colors.White, LogType.Info);
                AddLogEntry($"Path: {scene.SceneFilePath}", Colors.Gray, LogType.Info);
                AddLogEntry($"Children: {scene.GetChildCount()}", Colors.Gray, LogType.Info);
            }
            else
            {
                AddLogEntry("No current scene", Colors.Red, LogType.Error);
            }
        }
        
        private void ExecuteList(string[] args)
        {
            var scene = GetTree().CurrentScene;
            if (scene == null)
            {
                AddLogEntry("No scene loaded", Colors.Red, LogType.Error);
                return;
            }
            
            string filter = args.Length > 1 ? args[1].ToLower() : "";
            AddLogEntry($"=== SCENE TREE {(string.IsNullOrEmpty(filter) ? "" : $"(filter: {filter})")} ===", Colors.Cyan, LogType.Info);
            ListNodes(scene, 0, filter);
        }
        
        private void ListNodes(Node node, int depth, string filter)
        {
            string indent = new string(' ', depth * 2);
            string nodeName = node.Name;
            string nodeType = node.GetType().Name;
            
            if (string.IsNullOrEmpty(filter) || nodeName.ToLower().Contains(filter) || nodeType.ToLower().Contains(filter))
            {
                AddLogEntry($"{indent}└─ {nodeName} ({nodeType})", Colors.White, LogType.Info);
            }
            
            foreach (Node child in node.GetChildren())
            {
                ListNodes(child, depth + 1, filter);
            }
        }
        
        private void ExecuteReload(string[] args)
        {
            var scene = GetTree().CurrentScene;
            if (scene != null)
            {
                AddLogEntry("Reloading scene...", Colors.Yellow, LogType.Info);
                GetTree().ReloadCurrentScene();
            }
            else
            {
                AddLogEntry("No scene to reload", Colors.Red, LogType.Error);
            }
        }
        
        private void ExecuteGC(string[] args)
        {
            AddLogEntry("Forcing garbage collection...", Colors.Yellow, LogType.Info);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            AddLogEntry("Garbage collection complete", Colors.Green, LogType.Info);
        }
        
        private void ExecuteVSync(string[] args)
        {
            if (args.Length > 1)
            {
                bool enable = args[1].ToLower() == "on";
                DisplayServer.WindowSetVsyncMode(enable ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
                AddLogEntry($"VSync {(enable ? "enabled" : "disabled")}", Colors.Green, LogType.Info);
            }
            else
            {
                var mode = DisplayServer.WindowGetVsyncMode();
                AddLogEntry($"VSync: {mode}", Colors.White, LogType.Info);
            }
        }
        
        private void ExecuteFullscreen(string[] args)
        {
            var mode = DisplayServer.WindowGetMode();
            bool isFullscreen = mode == DisplayServer.WindowMode.Fullscreen;
            DisplayServer.WindowSetMode(isFullscreen ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
            AddLogEntry($"Fullscreen {(isFullscreen ? "disabled" : "enabled")}", Colors.Green, LogType.Info);
        }
        
        private void ExecuteHistory(string[] args)
        {
            if (_commandHistory.Count == 0)
            {
                AddLogEntry("No command history", Colors.Gray, LogType.Info);
                return;
            }
            
            AddLogEntry($"=== COMMAND HISTORY ({_commandHistory.Count}) ===", Colors.Cyan, LogType.Info);
            for (int i = 0; i < _commandHistory.Count; i++)
            {
                AddLogEntry($"  {i + 1}. {_commandHistory[i]}", Colors.White, LogType.Info);
            }
        }

        public void FocusInput()
        {
            _commandInput.GrabFocus();
        }
    }

    public enum LogType
    {
        Info,
        Warning,
        Error,
    }
    
    internal class ConsoleCommand
    {
        public string Name { get; }
        public string Description { get; }
        private readonly Action<string[]> _execute;
        
        public ConsoleCommand(string name, string description, Action<string[]> execute)
        {
            Name = name;
            Description = description;
            _execute = execute;
        }
        
        public void Execute(string[] args) => _execute?.Invoke(args);
    }
}
