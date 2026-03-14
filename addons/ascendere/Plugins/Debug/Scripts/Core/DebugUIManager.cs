using System;
using System.Collections.Generic;
using Godot;

namespace Ascendere.Debug.UI
{
    /// <summary>
    /// Manages the debug UI layer, windows, and visual components
    /// </summary>
    public partial class DebugUIManager : Node
    {
        private CanvasLayer _debugLayer;
        private Control _windowContainer;
        private readonly Dictionary<string, DebugWindow> _windows = new();
        private readonly Dictionary<string, object> _watchedValues = new();
        private double _watchUpdateTimer = 0.0;
        private const double WATCH_UPDATE_INTERVAL = 0.05;

        public CanvasLayer DebugLayer => _debugLayer;
        public Control WindowContainer => _windowContainer;

        public void Initialize()
        {
            SetupDebugLayer();
        }

        public override void _Process(double delta)
        {
            if (_watchedValues.Count > 0)
            {
                _watchUpdateTimer += delta;
                if (_watchUpdateTimer >= WATCH_UPDATE_INTERVAL)
                {
                    _watchUpdateTimer = 0.0;
                    UpdateWatchWindow();
                }
            }
        }

        private void SetupDebugLayer()
        {
            _debugLayer = new CanvasLayer();
            _debugLayer.Layer = 100;
            AddChild(_debugLayer);

            _windowContainer = new Control();
            _windowContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _windowContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
            _debugLayer.AddChild(_windowContainer);
        }

        public void SetVisible(bool visible)
        {
            if (_debugLayer != null)
                _debugLayer.Visible = visible;
        }

        public bool IsVisible() => _debugLayer?.Visible ?? false;

        // ===== WINDOW MANAGEMENT =====

        public DebugWindow CreateWindow(string title, Vector2 position, Vector2 size)
        {
            if (_windows.ContainsKey(title))
            {
                return _windows[title];
            }

            var window = new DebugWindow(title);
            window.Position = position;
            window.Size = size;
            _windowContainer.AddChild(window);
            _windows[title] = window;
            return window;
        }

        public DebugWindow GetWindow(string title)
        {
            return _windows.GetValueOrDefault(title);
        }

        public void RemoveWindow(string title)
        {
            if (_windows.TryGetValue(title, out var window))
            {
                window.QueueFree();
                _windows.Remove(title);
            }
        }

        // ===== VALUE WATCHING =====

        public void Watch(string key, object value)
        {
            bool wasEmpty = _watchedValues.Count == 0;
            _watchedValues[key] = value;

            // Auto-show watch window on first watch
            if (wasEmpty && _watchedValues.Count > 0)
            {
                ShowWatchWindow();
            }
        }

        public void Unwatch(string key)
        {
            _watchedValues.Remove(key);
        }

        public void ClearWatches()
        {
            _watchedValues.Clear();
        }

        public void ShowWatchWindow()
        {
            if (_windows.ContainsKey("Watch Values"))
            {
                _windows["Watch Values"].Visible = true;
                return;
            }

            var window = CreateWindow("Watch Values", new Vector2(20, 20), new Vector2(400, 350));

            // Customize window styling
            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.12f, 0.12f, 0.15f, 0.95f);
            style.SetBorderWidthAll(2);
            style.BorderColor = new Color(0.4f, 0.7f, 0.9f, 1.0f);
            style.SetCornerRadiusAll(8);
            style.SetExpandMarginAll(4);
            style.ShadowSize = 8;
            style.ShadowColor = new Color(0, 0, 0, 0.5f);
            window.AddThemeStyleboxOverride("panel", style);

            UpdateWatchWindow();
        }

        private void UpdateWatchWindow()
        {
            var window = GetWindow("Watch Values");
            if (window == null || !window.Visible)
                return;

            window.Clear();

            if (_watchedValues.Count == 0)
            {
                var emptyLabel = new Label();
                emptyLabel.Text = "No values being watched";
                emptyLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
                emptyLabel.AddThemeConstantOverride("outline_size", 1);
                emptyLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
                emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
                window.GetContentContainer().AddChild(emptyLabel);

                var hintLabel = new Label();
                hintLabel.Text =
                    "\nUse DebugManager.Instance.Watch(\"key\", value)\nto add values here";
                hintLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
                hintLabel.HorizontalAlignment = HorizontalAlignment.Center;
                window.GetContentContainer().AddChild(hintLabel);
                return;
            }

            // Add header
            var header = new HBoxContainer();
            var headerBg = new Panel();
            var headerStyle = new StyleBoxFlat();
            headerStyle.BgColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);
            headerBg.AddThemeStyleboxOverride("panel", headerStyle);

            var countLabel = new Label();
            countLabel.Text =
                $"  {_watchedValues.Count} Value{(_watchedValues.Count != 1 ? "s" : "")}";
            countLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.8f, 1.0f));
            countLabel.AddThemeConstantOverride("outline_size", 1);
            countLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
            header.AddChild(countLabel);

            var spacer = new Control();
            spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            header.AddChild(spacer);

            var clearBtn = new Button();
            clearBtn.Text = "Clear All";
            clearBtn.AddThemeColorOverride("font_color", new Color(1.0f, 0.5f, 0.5f));
            clearBtn.Pressed += () =>
            {
                ClearWatches();
                UpdateWatchWindow();
            };
            header.AddChild(clearBtn);

            window.GetContentContainer().AddChild(header);

            // Add separator
            var sep = new HSeparator();
            window.GetContentContainer().AddChild(sep);

            // Add watched values with improved formatting
            foreach (var kvp in _watchedValues)
            {
                var row = new PanelContainer();
                var rowStyle = new StyleBoxFlat();
                rowStyle.BgColor = new Color(0.15f, 0.15f, 0.18f, 0.6f);
                rowStyle.SetContentMarginAll(8);
                rowStyle.SetCornerRadiusAll(4);
                row.AddThemeStyleboxOverride("panel", rowStyle);

                var hbox = new HBoxContainer();
                row.AddChild(hbox);

                // Key label
                var keyLabel = new Label();
                keyLabel.Text = kvp.Key;
                keyLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.9f, 1.0f));
                keyLabel.AddThemeConstantOverride("outline_size", 1);
                keyLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
                keyLabel.CustomMinimumSize = new Vector2(120, 0);
                hbox.AddChild(keyLabel);

                // Colon
                var colonLabel = new Label();
                colonLabel.Text = ": ";
                colonLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
                hbox.AddChild(colonLabel);

                // Value label with color coding based on type
                var valueLabel = new Label();
                string valueText = kvp.Value?.ToString() ?? "null";
                Color valueColor = kvp.Value switch
                {
                    null => new Color(0.7f, 0.3f, 0.3f),
                    bool => new Color(0.8f, 0.6f, 1.0f),
                    int or float or double => new Color(0.6f, 1.0f, 0.6f),
                    string => new Color(1.0f, 0.9f, 0.6f),
                    Vector2 or Vector3 or Vector2I or Vector3I => new Color(0.6f, 0.8f, 1.0f),
                    _ => new Color(0.9f, 0.9f, 0.9f),
                };

                valueLabel.Text = valueText;
                valueLabel.AddThemeColorOverride("font_color", valueColor);
                valueLabel.AddThemeConstantOverride("outline_size", 1);
                valueLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
                valueLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                valueLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                hbox.AddChild(valueLabel);

                window.GetContentContainer().AddChild(row);

                // Add small spacer between rows
                var rowSpacer = new Control();
                rowSpacer.CustomMinimumSize = new Vector2(0, 4);
                window.GetContentContainer().AddChild(rowSpacer);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var window in _windows.Values)
                {
                    window?.QueueFree();
                }
                _windows.Clear();
                _watchedValues.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
