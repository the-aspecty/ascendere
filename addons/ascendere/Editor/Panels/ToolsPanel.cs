#if TOOLS
using Godot;
using System;

namespace Ascendere.Editor
{
    /// <summary>
    /// Tools panel for the Ascendere main panel.
    /// Provides access to development tools.
    /// </summary>
    public class ToolsPanel
    {
        public Control CreatePanel()
        {
            var toolsPanel = new PanelContainer
            {
                Name = "Tools",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            };
            toolsPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);

            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            };
            scroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            toolsPanel.AddChild(scroll);

            var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            scroll.AddChild(content);
            content.SetAnchorsPreset(Control.LayoutPreset.FullRect);

            var infoLabel = new Label { Text = "Development Tools" };
            infoLabel.AddThemeFontSizeOverride("font_size", 16);
            content.AddChild(infoLabel);

            // Tool buttons
            var toolsGrid = new GridContainer { Columns = 2 };
            content.AddChild(toolsGrid);

            AddToolButton(
                toolsGrid,
                "Scene Discovery",
                "Discover and analyze scene templates",
                OnSceneDiscoveryPressed
            );
            AddToolButton(
                toolsGrid,
                "Entity Inspector",
                "Inspect game entities and components",
                OnEntityInspectorPressed
            );
            AddToolButton(
                toolsGrid,
                "Event Debugger",
                "Debug event system in real-time",
                OnEventDebuggerPressed
            );
            AddToolButton(
                toolsGrid,
                "Module Manager",
                "Enable/disable framework modules",
                OnModuleManagerPressed
            );

            return toolsPanel;
        }

        private void AddToolButton(
            GridContainer grid,
            string name,
            string tooltip,
            Action onPressed
        )
        {
            var btn = new Button
            {
                Text = name,
                TooltipText = tooltip,
                CustomMinimumSize = new Vector2(150, 40),
            };
            btn.Pressed += onPressed;
            grid.AddChild(btn);
        }

        private void OnSceneDiscoveryPressed()
        {
            GD.Print("[ToolsPanel] Opening Scene Discovery...");
        }

        private void OnEntityInspectorPressed()
        {
            GD.Print("[ToolsPanel] Opening Entity Inspector...");
        }

        private void OnEventDebuggerPressed()
        {
            GD.Print("[ToolsPanel] Opening Event Debugger...");
        }

        private void OnModuleManagerPressed()
        {
            GD.Print("[ToolsPanel] Opening Module Manager...");
        }
    }
}
#endif
