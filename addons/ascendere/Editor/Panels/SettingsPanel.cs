#if TOOLS
using Godot;

namespace Ascendere.Editor
{
    /// <summary>
    /// Settings panel for the Ascendere main panel.
    /// Provides quick access to common settings and paths.
    /// </summary>
    public class SettingsPanel
    {
        public Control CreatePanel()
        {
            var settingsPanel = new PanelContainer
            {
                Name = "Settings",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            };
            settingsPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);

            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            };
            scroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            settingsPanel.AddChild(scroll);

            var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            scroll.AddChild(content);
            content.SetAnchorsPreset(Control.LayoutPreset.FullRect);

            var infoLabel = new Label { Text = "Quick Settings" };
            infoLabel.AddThemeFontSizeOverride("font_size", 16);
            content.AddChild(infoLabel);

            var noteLabel = new Label
            {
                Text = "For full settings, go to Project Settings > Ascendere",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            noteLabel.AddThemeColorOverride("font_color", Colors.Gray);
            content.AddChild(noteLabel);

            content.AddChild(new HSeparator { CustomMinimumSize = new Vector2(0, 10) });

            // Quick toggle settings
            AddSettingToggle(content, "Debug Mode", "ascendere/general/debug_mode");
            AddSettingToggle(content, "Auto Discovery", "ascendere/general/auto_discovery");
            AddSettingToggle(content, "Verbose Logging", "ascendere/general/verbose_logging");

            content.AddChild(new HSeparator { CustomMinimumSize = new Vector2(0, 10) });

            var openSettingsBtn = new Button
            {
                Text = "Open Project Settings",
                TooltipText = "Open full Ascendere settings in Project Settings",
            };
            openSettingsBtn.Pressed += OnOpenProjectSettingsPressed;
            content.AddChild(openSettingsBtn);

            // Add path settings section
            content.AddChild(new HSeparator { CustomMinimumSize = new Vector2(0, 10) });
            var pathsTitle = new Label
            {
                Text = "Default Paths",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            pathsTitle.AddThemeFontSizeOverride("font_size", 16);
            content.AddChild(pathsTitle);

            AddPathSetting(content, "Components Path", "paths/components");
            AddPathSetting(content, "Entities Path", "paths/entities");
            AddPathSetting(content, "Systems Path", "paths/systems");
            AddPathSetting(content, "Events Path", "paths/events");
            AddPathSetting(content, "Modules Path", "paths/modules");
            AddPathSetting(content, "Plugins Path", "paths/plugins");
            AddPathSetting(content, "Game Scenes Path", "paths/gamescenes");

            return settingsPanel;
        }

        private void AddSettingToggle(VBoxContainer container, string label, string settingPath)
        {
            var row = new HBoxContainer();
            container.AddChild(row);

            var labelNode = new Label
            {
                Text = label,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            row.AddChild(labelNode);

            var toggle = new CheckButton { Name = settingPath.Replace("/", "_") };

            // Get current value from ProjectSettings
            if (ProjectSettings.HasSetting(settingPath))
            {
                toggle.ButtonPressed = (bool)ProjectSettings.GetSetting(settingPath);
            }

            toggle.Toggled += (pressed) => OnSettingToggled(settingPath, pressed);
            row.AddChild(toggle);
        }

        private void AddPathSetting(VBoxContainer container, string label, string key)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            container.AddChild(row);

            var labelNode = new Label
            {
                Text = label,
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
            };
            row.AddChild(labelNode);

            // Determine fallback defaults
            string defaultValue = key switch
            {
                "paths/components" => "res://Components",
                "paths/entities" => "res://Entities",
                "paths/systems" => "res://Systems",
                "paths/events" => "res://Events",
                "paths/modules" => "res://addons/ascendere/Modules",
                "paths/plugins" => "res://addons/ascendere/Plugins",
                "paths/gamescenes" => "res://GameScenes",
                _ => "res://",
            };

            var pathEdit = new LineEdit
            {
                Text = defaultValue,
                PlaceholderText = defaultValue,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            row.AddChild(pathEdit);
        }

        private void OnSettingToggled(string settingPath, bool pressed)
        {
            ProjectSettings.SetSetting(settingPath, pressed);
            GD.Print($"[SettingsPanel] Setting '{settingPath}' changed to: {pressed}");
        }

        private void OnOpenProjectSettingsPressed()
        {
            GD.Print("[SettingsPanel] To access full Ascendere settings:");
            GD.Print("  1. Go to Project > Project Settings");
            GD.Print("  2. Navigate to the 'Ascendere' section");
        }
    }
}
#endif
