using Ascendere.Debug;
using Godot;

namespace Ascendere.Debug.Examples
{
    /// <summary>
    /// Demonstrates all DebugManager features
    /// Press F1 to toggle debug overlay
    /// Press F2 to toggle console
    /// Press F3 to toggle node inspector
    /// Press F4 to toggle performance graph
    /// </summary>
    public partial class DebugExampleScript : Node3D
    {
        private float _time = 0f;
        private Vector3 _targetPosition = Vector3.Zero;
        private int _frameCount = 0;

        public override void _Ready()
        {
            //add debug manager node
            if (DebugManager.Instance == null)
            {
                var debugManager = new DebugManager();
                debugManager.Name = "DebugManager";
                //defered to avoid tree modification during _Ready
                GetTree().Root.CallDeferred("add_child", debugManager);
            }
            // Initialize random target position
            _targetPosition = new Vector3(
                GD.Randf() * 10f - 5f,
                GD.Randf() * 5f,
                GD.Randf() * 10f - 5f
            );

            GD.Print("Debug Example loaded! Press F1-F4 to toggle debug features.");
            
            // Log some example messages to console
            CallDeferred(MethodName.LogExampleMessages);
        }
        
        private void LogExampleMessages()
        {
            DebugManager.Instance?.Log("Debug Example initialized", Ascendere.Debug.UI.LogType.Info);
            DebugManager.Instance?.Log("Target position set", Ascendere.Debug.UI.LogType.Info);
        }

        public override void _Process(double delta)
        {
            _time += (float)delta;
            _frameCount++;

            // Only demonstrate debug drawing every few frames to reduce overhead
            if (_frameCount % 3 == 0)
            {
                DemonstrateDebugDrawing();
            }

            // Watch some values (update every frame)
            DebugManager.Instance?.Watch("Time", _time);
            DebugManager.Instance?.Watch("Frame Count", _frameCount);
            DebugManager.Instance?.Watch("Position", GlobalPosition);
            DebugManager.Instance?.Watch("Target", _targetPosition);
            
            // Log example messages periodically
            if (_frameCount % 300 == 0) // Every 5 seconds at 60fps
            {
                DebugManager.Instance?.Log($"Frame {_frameCount} reached", Ascendere.Debug.UI.LogType.Info);
            }
            
            if (_frameCount % 600 == 0) // Every 10 seconds
            {
                DebugManager.Instance?.Log("This is a warning message", Ascendere.Debug.UI.LogType.Warning);
            }
        }

        private void DemonstrateDebugDrawing()
        {
            var debugMgr = DebugManager.Instance;
            if (debugMgr == null)
                return;

            // Draw animated line
            Vector3 lineStart = new Vector3(-5, 0, 0);
            Vector3 lineEnd = new Vector3(5, Mathf.Sin(_time * 2f) * 2f, 0);
            debugMgr.DrawLine3D(this, lineStart, lineEnd, Colors.Red);

            // Draw rotating sphere
            Vector3 spherePos = new Vector3(Mathf.Cos(_time) * 3f, 2f, Mathf.Sin(_time) * 3f);
            debugMgr.DrawSphere3D(this, spherePos, 0.5f, Colors.Green);

            // Draw box at origin
            debugMgr.DrawBox3D(this, Vector3.Zero, new Vector3(2f, 2f, 2f), Colors.Blue);

            // Draw arrow pointing to target
            debugMgr.DrawArrow3D(this, GlobalPosition, _targetPosition, Colors.Yellow);

            // Draw label at target
            debugMgr.DrawLabel3D(this, _targetPosition, "Target", Colors.White);

            // Draw path - simple circle with fewer points
            Vector3[] pathPoints = new Vector3[8];
            for (int i = 0; i < pathPoints.Length; i++)
            {
                float t = i / (float)pathPoints.Length * Mathf.Tau;
                pathPoints[i] = new Vector3(Mathf.Cos(t) * 4f, 1f, Mathf.Sin(t) * 4f);
            }
            debugMgr.DrawPath3D(this, pathPoints, Colors.Cyan, closed: true);

            // Persistent drawing example (stays for 3 seconds) - reduced frequency
            if (_frameCount % 180 == 0) // Every 180 frames (3 seconds at 60fps)
            {
                Vector3 persistentPos = new Vector3(
                    GD.Randf() * 10f - 5f,
                    GD.Randf() * 5f,
                    GD.Randf() * 10f - 5f
                );
                debugMgr.DrawSphere3D(this, persistentPos, 0.3f, Colors.Orange, duration: 3f);
            }
        }
    }
}
