using System;
using Godot;

namespace Ascendere.Debug.UI
{
    /// <summary>
    /// A draggable debug window container
    /// </summary>
    public partial class DebugWindow : PanelContainer
    {
        private VBoxContainer _content;
        private Label _titleLabel;
        private Button _closeButton;
        private bool _dragging = false;
        private Vector2 _dragOffset;

        public DebugWindow(string title)
        {
            var vbox = new VBoxContainer();
            AddChild(vbox);

            var titleBar = new Panel();
            var titleHbox = new HBoxContainer();
            titleBar.AddChild(titleHbox);

            _titleLabel = new Label();
            _titleLabel.Text = "  " + title;
            _titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _titleLabel.AddThemeColorOverride("font_color", Colors.White);
            titleHbox.AddChild(_titleLabel);

            _closeButton = new Button();
            _closeButton.Text = "×";
            _closeButton.CustomMinimumSize = new Vector2(30, 0);
            _closeButton.Pressed += () => QueueFree();
            titleHbox.AddChild(_closeButton);

            var titleStyle = new StyleBoxFlat();
            titleStyle.BgColor = new Color(0.2f, 0.2f, 0.25f);
            titleBar.AddThemeStyleboxOverride("panel", titleStyle);
            vbox.AddChild(titleBar);

            var scroll = new ScrollContainer();
            scroll.CustomMinimumSize = new Vector2(0, 200);
            vbox.AddChild(scroll);

            _content = new VBoxContainer();
            scroll.AddChild(_content);

            var panelStyle = new StyleBoxFlat();
            panelStyle.BgColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
            panelStyle.SetBorderWidthAll(2);
            panelStyle.BorderColor = new Color(0.4f, 0.4f, 0.5f);
            panelStyle.SetCornerRadiusAll(4);
            AddThemeStyleboxOverride("panel", panelStyle);

            titleBar.GuiInput += OnTitleBarInput;
        }

        private void OnTitleBarInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mb)
            {
                if (mb.ButtonIndex == MouseButton.Left)
                {
                    if (mb.Pressed)
                    {
                        _dragging = true;
                        _dragOffset = mb.Position;
                    }
                    else
                    {
                        _dragging = false;
                    }
                }
            }
            else if (@event is InputEventMouseMotion mm && _dragging)
            {
                Position += mm.Position - _dragOffset;
            }
        }

        public void AddLabel(string text, Color? color = null)
        {
            var label = new Label();
            label.Text = text;
            label.AddThemeColorOverride("font_color", color ?? Colors.White);
            _content.AddChild(label);
        }

        public void AddButton(string text, Action callback)
        {
            var btn = new Button();
            btn.Text = text;
            btn.Pressed += () => callback();
            _content.AddChild(btn);
        }

        public void AddSlider(
            string label,
            float min,
            float max,
            float value,
            Action<float> callback
        )
        {
            var hbox = new HBoxContainer();
            _content.AddChild(hbox);

            var lbl = new Label();
            lbl.Text = label;
            lbl.CustomMinimumSize = new Vector2(100, 0);
            lbl.AddThemeColorOverride("font_color", Colors.White);
            hbox.AddChild(lbl);

            var slider = new HSlider();
            slider.MinValue = min;
            slider.MaxValue = max;
            slider.Value = value;
            slider.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            slider.ValueChanged += (v) => callback((float)v);
            hbox.AddChild(slider);

            var valueLbl = new Label();
            valueLbl.Text = value.ToString("F2");
            valueLbl.CustomMinimumSize = new Vector2(50, 0);
            valueLbl.AddThemeColorOverride("font_color", Colors.Cyan);
            slider.ValueChanged += (v) => valueLbl.Text = v.ToString("F2");
            hbox.AddChild(valueLbl);
        }

        public void AddCheckbox(string label, bool value, Action<bool> callback)
        {
            var hbox = new HBoxContainer();
            _content.AddChild(hbox);

            var checkbox = new CheckBox();
            checkbox.ButtonPressed = value;
            checkbox.Toggled += (pressed) => callback(pressed);
            hbox.AddChild(checkbox);

            var lbl = new Label();
            lbl.Text = label;
            lbl.AddThemeColorOverride("font_color", Colors.White);
            hbox.AddChild(lbl);
        }

        public void AddSeparator()
        {
            var sep = new HSeparator();
            _content.AddChild(sep);
        }

        public void Clear()
        {
            foreach (Node child in _content.GetChildren())
            {
                child.QueueFree();
            }
        }
        
        public VBoxContainer GetContentContainer()
        {
            return _content;
        }
    }
}
