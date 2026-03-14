using System.Collections.Generic;
using Godot;

namespace Ascendere.Debug.UI
{
    /// <summary>
    /// Performance monitoring graph showing FPS and other metrics
    /// </summary>
    public partial class PerformanceGraph : PanelContainer
    {
        private Control _graphArea;
        private readonly List<float> _fpsHistory = new();
        private Label _statsLabel;
        private const int MAX_SAMPLES = 120;
        private double _updateTimer = 0.0;
        private const double UPDATE_INTERVAL = 0.016; // ~60fps

        public PerformanceGraph()
        {
            Position = new Vector2(10, 10);
            Size = new Vector2(300, 200);

            var vbox = new VBoxContainer();
            AddChild(vbox);

            _statsLabel = new Label();
            _statsLabel.AddThemeColorOverride("font_color", Colors.White);
            vbox.AddChild(_statsLabel);

            _graphArea = new Control();
            _graphArea.CustomMinimumSize = new Vector2(0, 150);
            _graphArea.Draw += DrawGraph;
            vbox.AddChild(_graphArea);

            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.1f, 0.1f, 0.12f, 0.9f);
            style.SetBorderWidthAll(2);
            style.BorderColor = new Color(0.3f, 0.3f, 0.35f);
            AddThemeStyleboxOverride("panel", style);
        }
        
        public override void _Process(double delta)
        {
            if (!Visible)
                return;
                
            _updateTimer += delta;
            if (_updateTimer >= UPDATE_INTERVAL)
            {
                _updateTimer = 0.0;
                UpdateStats();
            }
        }

        private void UpdateStats()
        {
            float fps = (float)Engine.GetFramesPerSecond();
            _fpsHistory.Add(fps);

            if (_fpsHistory.Count > MAX_SAMPLES)
                _fpsHistory.RemoveAt(0);

            var avgFps = 0f;
            foreach (var f in _fpsHistory)
                avgFps += f;
            avgFps /= _fpsHistory.Count;

            _statsLabel.Text = $"FPS: {fps:F0} | Avg: {avgFps:F0} | Objects: {Performance.GetMonitor(Performance.Monitor.ObjectCount)}";
            _graphArea.QueueRedraw();
        }

        private void DrawGraph()
        {
            if (_fpsHistory.Count < 2)
                return;

            var size = _graphArea.Size;
            var maxFps = 144f;
            var step = size.X / MAX_SAMPLES;

            // Draw grid
            _graphArea.DrawLine(
                new Vector2(0, size.Y / 2),
                new Vector2(size.X, size.Y / 2),
                new Color(0.3f, 0.3f, 0.3f),
                1f
            );

            // Draw FPS line
            for (int i = 0; i < _fpsHistory.Count - 1; i++)
            {
                var x1 = i * step;
                var y1 = size.Y - (_fpsHistory[i] / maxFps * size.Y);
                var x2 = (i + 1) * step;
                var y2 = size.Y - (_fpsHistory[i + 1] / maxFps * size.Y);

                var color =
                    _fpsHistory[i] >= 60 ? Colors.Green
                    : _fpsHistory[i] >= 30 ? Colors.Yellow
                    : Colors.Red;

                _graphArea.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), color, 2f);
            }
        }
    }
}
