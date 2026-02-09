#if TOOLS
using Godot;

namespace Ascendere.Editor
{
    /// <summary>
    /// Modules panel for the Ascendere main panel.
    /// Displays available framework modules.
    /// </summary>
    public class ModulesPanel
    {
        public Control CreatePanel()
        {
            var modulesPanel = new PanelContainer
            {
                Name = "Modules",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            };
            modulesPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);

            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            };
            scroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            modulesPanel.AddChild(scroll);

            var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            scroll.AddChild(content);
            content.SetAnchorsPreset(Control.LayoutPreset.FullRect);

            var infoLabel = new Label { Text = "Available Modules" };
            infoLabel.AddThemeFontSizeOverride("font_size", 16);
            content.AddChild(infoLabel);

            // Module list will be populated dynamically
            var moduleList = new ItemList
            {
                Name = "ModuleList",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(0, 200),
            };
            content.AddChild(moduleList);

            // Add known modules
            // @todo pick from reflection
            string[] modules =
            {
                "Events",
                "ExtendedStateMachine",
                "Input",
                "Inventory",
                "Networking",
                "Quests",
                "SaveLoad",
                "UI",
                "AI",
            };
            foreach (var module in modules)
            {
                moduleList.AddItem(module);
            }

            return modulesPanel;
        }
    }
}
#endif
