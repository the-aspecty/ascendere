using Ascendere.Debug.UI;
using Ascendere.Log;
using Godot;

namespace Ascendere.Debug
{
#if TOOLS
    [Tool]
#endif
    /// <summary>
    /// Central coordinator for all debug functionality
    /// Follows SRP by delegating responsibilities to specialized managers
    /// </summary>
    public partial class DebugManager : Node
    {
        private static DebugManager _instance;
        public static DebugManager Instance => _instance;

        // Specialized managers
        private DebugDrawManager _drawManager;
        private DebugUIManager _uiManager;
        private DebugInputHandler _inputHandler;

        // UI Components
        private DebugConsole _console;
        private PerformanceGraph _performanceGraph;
        private NodeInspector _nodeInspector;

        // Services
        private ILogService _log;

        private bool _enabled = true;

        public override void _Ready()
        {
            _instance = this;

            // Get log service from ServiceLocator
            _log = ServiceLocator.Get<ILogService>();

            InitializeManagers();
            InitializeUI();
            SetupInputHandlers();

            _log.Info(this, "DebugManager initialized - Press F1 to toggle");
        }

        private void InitializeManagers()
        {
            // Draw Manager
            _drawManager = new DebugDrawManager();
            _drawManager.Name = "DrawManager";
            AddChild(_drawManager);

            // UI Manager
            _uiManager = new DebugUIManager();
            _uiManager.Name = "UIManager";
            AddChild(_uiManager);
            _uiManager.Initialize();

            // Input Handler
            _inputHandler = new DebugInputHandler();
            _inputHandler.Name = "InputHandler";
            AddChild(_inputHandler);

            // DrawManager will use its own _Draw method
        }

        private void InitializeUI()
        {
            // Console
            _console = new DebugConsole(_log);
            _console.Visible = false;
            _uiManager.WindowContainer.AddChild(_console);

            // Forward logs to console
            ForwardLogsToConsole();
        }

        private void ForwardLogsToConsole()
        {
            // Send welcome messages to console
            _console?.AddLog(
                "Debug system initialized successfully",
                Colors.Green,
                UI.LogType.Info
            );
            _console?.AddLog("Press F4 to toggle console visibility", Colors.Cyan, UI.LogType.Info);
            _console?.AddLog(
                "Use DebugManager.Instance.Log() to send messages here",
                Colors.Gray,
                UI.LogType.Info
            );
        }

        private void SetupInputHandlers()
        {
            _inputHandler.OnToggleDebug += ToggleDebug;
            _inputHandler.OnToggleConsole += ToggleConsole;
            _inputHandler.OnToggleInspector += ToggleInspector;
            _inputHandler.OnTogglePerformance += TogglePerformance;
        }

        public override void _Process(double delta)
        {
            if (!_enabled)
                return;

            _drawManager.Process(delta);
        }

        // ===== CORE CONTROLS =====

        public void ToggleDebug()
        {
            _enabled = !_enabled;
            _uiManager.SetVisible(_enabled);
            _log.Info(this, $"Debug System: {(_enabled ? "ENABLED" : "DISABLED")}");
        }

        public void ToggleConsole()
        {
            if (_console == null)
                return;
            _console.Visible = !_console.Visible;
            if (_console.Visible)
                _console.FocusInput();
        }

        public void ToggleInspector()
        {
            if (_nodeInspector == null)
            {
                _nodeInspector = new NodeInspector();
                // Default to inspecting the current scene root
                var sceneRoot = GetTree().CurrentScene;
                if (sceneRoot != null)
                    _nodeInspector.SetTarget(sceneRoot);
                _uiManager.WindowContainer.AddChild(_nodeInspector);
            }
            else
            {
                _nodeInspector.Visible = !_nodeInspector.Visible;
            }
        }

        public void ToggleInspector(Node target)
        {
            if (_nodeInspector == null)
            {
                _nodeInspector = new NodeInspector();
                _nodeInspector.SetTarget(target);
                _uiManager.WindowContainer.AddChild(_nodeInspector);
            }
            else
            {
                _nodeInspector.SetTarget(target);
                _nodeInspector.Visible = true;
            }
        }

        public void TogglePerformance()
        {
            if (_performanceGraph == null)
            {
                _performanceGraph = new PerformanceGraph();
                _uiManager.WindowContainer.AddChild(_performanceGraph);
            }
            else
            {
                _performanceGraph.Visible = !_performanceGraph.Visible;
            }
        }

        // ===== DEBUG DRAWING API =====

        public void DrawLine3D(
            Node node,
            Vector3 from,
            Vector3 to,
            Color color,
            float duration = 0f,
            float thickness = 2f
        ) => _drawManager.DrawLine3D(node, from, to, color, duration, thickness);

        public void DrawSphere3D(
            Node node,
            Vector3 position,
            float radius,
            Color color,
            float duration = 0f
        ) => _drawManager.DrawSphere3D(node, position, radius, color, duration);

        public void DrawBox3D(
            Node node,
            Vector3 position,
            Vector3 size,
            Color color,
            float duration = 0f
        ) => _drawManager.DrawBox3D(node, position, size, color, duration);

        public void DrawArrow3D(
            Node node,
            Vector3 from,
            Vector3 to,
            Color color,
            float duration = 0f
        ) => _drawManager.DrawArrow3D(node, from, to, color, duration);

        public void DrawLabel3D(
            Node node,
            Vector3 position,
            string text,
            Color color,
            float duration = 0f
        ) => _drawManager.DrawLabel3D(node, position, text, color, duration);

        public void DrawPath3D(
            Node node,
            Vector3[] points,
            Color color,
            float duration = 0f,
            bool closed = false
        ) => _drawManager.DrawPath3D(node, points, color, duration, closed);

        public void DrawRay3D(
            Node node,
            Vector3 origin,
            Vector3 direction,
            float length,
            Color color,
            float duration = 0f
        ) => _drawManager.DrawRay3D(node, origin, direction, length, color, duration);

        public void ClearDrawCommands(Node node) => _drawManager.ClearDrawCommands(node);

        public void ClearAllDrawCommands() => _drawManager.ClearAllDrawCommands();

        // ===== LOGGING =====

        public void Log(string message, UI.LogType type = UI.LogType.Info)
        {
            Color color = type switch
            {
                UI.LogType.Error => Colors.Red,
                UI.LogType.Warning => Colors.Yellow,
                _ => Colors.White,
            };
            _console?.AddLog(message, color, type);
        }

        // ===== VALUE WATCHING =====

        public void Watch(string key, object value) => _uiManager.Watch(key, value);

        public void Unwatch(string key) => _uiManager.Unwatch(key);

        public void ClearWatches() => _uiManager.ClearWatches();

        public void ShowWatchWindow() => _uiManager.ShowWatchWindow();

        // ===== LOGGING =====

        public void Log(string message, Color? color = null)
        {
            _log.Info(this, message);
            _console?.AddLog(message, color ?? Colors.White, LogType.Info);
        }

        public void LogWarning(string message)
        {
            _log.Warning(this, message);
            _console?.AddLog(message, Colors.Yellow, LogType.Warning);
        }

        public void LogError(string message)
        {
            _log.Error(this, message);
            _console?.AddLog(message, Colors.Red, LogType.Error);
        }

        // ===== WINDOW MANAGEMENT =====

        public DebugWindow CreateWindow(string title, Vector2 position, Vector2 size) =>
            _uiManager.CreateWindow(title, position, size);

        public DebugWindow GetWindow(string title) => _uiManager.GetWindow(title);

        public void RemoveWindow(string title) => _uiManager.RemoveWindow(title);

        // ===== NODE INSPECTION =====

        public void InspectNode(Node node)
        {
            if (_nodeInspector == null)
            {
                _nodeInspector = new NodeInspector();
                _uiManager.WindowContainer.AddChild(_nodeInspector);
            }

            _nodeInspector.SetTarget(node);
            _nodeInspector.Visible = true;
        }

        // ===== UTILITY =====

        public void Breakpoint(string message = "Breakpoint Hit")
        {
            _log.Warning(this, $"⚠ BREAKPOINT: {message}");
            GetTree().Paused = true;
        }

        public void TimeScale(float scale)
        {
            Engine.TimeScale = scale;
            _log.Info(this, $"Time scale set to {scale}x");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _instance == this)
            {
                _instance = null;
            }
            base.Dispose(disposing);
        }
    }
}
