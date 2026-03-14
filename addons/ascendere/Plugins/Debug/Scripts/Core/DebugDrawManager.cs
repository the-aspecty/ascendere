using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Ascendere.Debug
{
    /// <summary>
    /// Manages all debug drawing commands and rendering
    /// </summary>
    public partial class DebugDrawManager : Control
    {
        private readonly Dictionary<Node, List<DebugDrawCommand>> _nodeDrawCommands = new();
        private readonly Queue<DebugDrawCommand> _commandPool = new(capacity: 256);
        private readonly List<(Node node, DebugDrawCommand cmd, double expireTime)> _timedCommands = new();
        private double _time = 0.0;
        private double _lastClearTime = 0.0;
        private const double CLEAR_INTERVAL = 1.0; // Clear non-persistent commands every second

        public override void _Ready()
        {
            // Make this control fullscreen but non-blocking
            SetAnchorsPreset(LayoutPreset.FullRect);
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public void Process(double delta)
        {
            _time += delta;
            
            // Clear non-persistent commands every second
            if (_time - _lastClearTime >= CLEAR_INTERVAL)
            {
                foreach (var kvp in _nodeDrawCommands)
                {
                    // Only remove commands with no duration (non-persistent)
                    kvp.Value.RemoveAll(cmd => cmd.Duration <= 0);
                }
                _lastClearTime = _time;
            }
            
            // Clean up invalid nodes
            var invalidNodes = _nodeDrawCommands.Keys.Where(n => !IsInstanceValid(n)).ToList();
            foreach (var node in invalidNodes)
            {
                _nodeDrawCommands.Remove(node);
            }
            
            // Process timed commands
            for (int i = _timedCommands.Count - 1; i >= 0; i--)
            {
                var (node, cmd, expireTime) = _timedCommands[i];
                
                if (_time >= expireTime)
                {
                    if (_nodeDrawCommands.ContainsKey(node))
                    {
                        _nodeDrawCommands[node].Remove(cmd);
                    }
                    
                    // Return command to pool
                    if (_commandPool.Count < 512)
                    {
                        _commandPool.Enqueue(cmd);
                    }
                    
                    _timedCommands.RemoveAt(i);
                }
            }

            // Trigger redraw
            QueueRedraw();
        }

        public override void _Draw()
        {
            foreach (var kvp in _nodeDrawCommands)
            {
                if (!IsInstanceValid(kvp.Key))
                    continue;

                foreach (var cmd in kvp.Value)
                {
                    cmd.Execute(this, kvp.Key);
                }
            }
        }

        // ===== PUBLIC API =====

        public void DrawLine3D(
            Node node,
            Vector3 from,
            Vector3 to,
            Color color,
            float duration = 0f,
            float thickness = 2f
        )
        {
            AddDrawCommand(node, new DebugDrawLine3D(from, to, color, duration, thickness));
        }

        public void DrawSphere3D(
            Node node,
            Vector3 position,
            float radius,
            Color color,
            float duration = 0f
        )
        {
            AddDrawCommand(node, new DebugDrawSphere(position, radius, color, duration));
        }

        public void DrawBox3D(
            Node node,
            Vector3 position,
            Vector3 size,
            Color color,
            float duration = 0f
        )
        {
            AddDrawCommand(node, new DebugDrawBox(position, size, color, duration));
        }

        public void DrawArrow3D(
            Node node,
            Vector3 from,
            Vector3 to,
            Color color,
            float duration = 0f
        )
        {
            AddDrawCommand(node, new DebugDrawArrow(from, to, color, duration));
        }

        public void DrawLabel3D(
            Node node,
            Vector3 position,
            string text,
            Color color,
            float duration = 0f
        )
        {
            AddDrawCommand(node, new DebugDrawLabel(position, text, color, duration));
        }

        public void DrawPath3D(
            Node node,
            Vector3[] points,
            Color color,
            float duration = 0f,
            bool closed = false
        )
        {
            AddDrawCommand(node, new DebugDrawPath(points, color, duration, closed));
        }

        public void DrawRay3D(
            Node node,
            Vector3 origin,
            Vector3 direction,
            float length,
            Color color,
            float duration = 0f
        )
        {
            AddDrawCommand(node, new DebugDrawRay(origin, direction, length, color, duration));
        }

        public void ClearDrawCommands(Node node)
        {
            if (_nodeDrawCommands.ContainsKey(node))
                _nodeDrawCommands[node].Clear();
        }

        public void ClearAllDrawCommands()
        {
            _nodeDrawCommands.Clear();
        }

        // ===== PRIVATE METHODS =====

        private void AddDrawCommand(Node node, DebugDrawCommand cmd)
        {
            if (!_nodeDrawCommands.ContainsKey(node))
                _nodeDrawCommands[node] = new List<DebugDrawCommand>();

            _nodeDrawCommands[node].Add(cmd);

            if (cmd.Duration > 0)
            {
                _timedCommands.Add((node, cmd, _time + cmd.Duration));
            }
        }
    }
}
