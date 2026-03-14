using System;
using Godot;

namespace Ascendere.Debug
{
    /// <summary>
    /// Handles all debug input processing and keybindings
    /// </summary>
    public partial class DebugInputHandler : Node
    {
        [Export]
        public Key ToggleKey = Key.F1;

        [Export]
        public Key ConsoleKey = Key.F4;

        [Export]
        public Key InspectorKey = Key.F2;

        [Export]
        public Key PerformanceKey = Key.F3;

        public event Action OnToggleDebug;
        public event Action OnToggleConsole;
        public event Action OnToggleInspector;
        public event Action OnTogglePerformance;

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey key && key.Pressed && !key.Echo)
            {
                if (key.Keycode == ToggleKey)
                {
                    OnToggleDebug?.Invoke();
                }
                else if (key.Keycode == ConsoleKey)
                {
                    OnToggleConsole?.Invoke();
                }
                else if (key.Keycode == InspectorKey)
                {
                    OnToggleInspector?.Invoke();
                }
                else if (key.Keycode == PerformanceKey)
                {
                    OnTogglePerformance?.Invoke();
                }
            }
        }
    }
}
