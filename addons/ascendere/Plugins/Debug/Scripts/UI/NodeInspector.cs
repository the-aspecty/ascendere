using System.Reflection;
using Godot;

namespace Ascendere.Debug.UI
{
    /// <summary>
    /// Node inspector for examining node properties at runtime
    /// </summary>
    public partial class NodeInspector : PanelContainer
    {
        private VBoxContainer _content;
        private Node _target;
        private Label _titleLabel;
        private double _refreshTimer = 0.0;
        private const double REFRESH_INTERVAL = 0.1;

        public NodeInspector()
        {
            Position = new Vector2(10, 420);
            Size = new Vector2(350, 400);

            var vbox = new VBoxContainer();
            AddChild(vbox);

            _titleLabel = new Label();
            _titleLabel.Text = "Node Inspector";
            _titleLabel.AddThemeColorOverride("font_color", Colors.Cyan);
            vbox.AddChild(_titleLabel);

            var scroll = new ScrollContainer();
            scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
            vbox.AddChild(scroll);

            _content = new VBoxContainer();
            scroll.AddChild(_content);

            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.1f, 0.1f, 0.12f, 0.9f);
            style.SetBorderWidthAll(2);
            style.BorderColor = new Color(0.3f, 0.3f, 0.35f);
            AddThemeStyleboxOverride("panel", style);
        }

        public override void _Process(double delta)
        {
            if (!Visible || _target == null)
                return;
                
            _refreshTimer += delta;
            if (_refreshTimer >= REFRESH_INTERVAL)
            {
                _refreshTimer = 0.0;
                Refresh();
            }
        }

        public void SetTarget(Node node)
        {
            _target = node;
            _refreshTimer = REFRESH_INTERVAL; // Trigger immediate refresh
        }

        private void Refresh()
        {
            foreach (Node child in _content.GetChildren())
                child.QueueFree();

            if (_target == null || !IsInstanceValid(_target))
            {
                AddLabel("No target selected", Colors.Gray);
                return;
            }

            _titleLabel.Text = $"Inspector: {_target.Name}";
            AddLabel($"Type: {_target.GetType().Name}", Colors.White);
            AddLabel($"Path: {_target.GetPath()}", Colors.Gray);

            if (_target is Node3D node3D)
            {
                AddSeparator();
                AddLabel("Transform:", Colors.Cyan);
                AddLabel($"  Position: {node3D.Position}", Colors.White);
                AddLabel($"  Rotation: {node3D.RotationDegrees}", Colors.White);
                AddLabel($"  Scale: {node3D.Scale}", Colors.White);
            }

            AddSeparator();
            AddLabel("Properties:", Colors.Cyan);

            var props = _target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                try
                {
                    var value = prop.GetValue(_target);
                    AddLabel($"  {prop.Name}: {value}", Colors.White);
                }
                catch
                {
                    // Skip properties that can't be accessed
                }
            }
        }

        private void AddLabel(string text, Color? color = null)
        {
            var label = new Label();
            label.Text = text;
            label.AddThemeColorOverride("font_color", color ?? Colors.White);
            _content.AddChild(label);
        }

        private void AddSeparator()
        {
            var sep = new HSeparator();
            _content.AddChild(sep);
        }
    }
}
