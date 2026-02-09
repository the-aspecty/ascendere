#if TOOLS
using Godot;

namespace Ascendere.Editor
{
    /// <summary>
    /// Overview panel for the Ascendere main panel.
    /// Displays framework status, statistics, and getting started information.
    /// </summary>
    public class OverviewPanel
    {
        private const int DefaultMargin = 8;

        public Control CreatePanel()
        {
            var overviewPanel = new PanelContainer
            {
                Name = "Overview",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            };
            overviewPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);

            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            };
            scroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            overviewPanel.AddChild(scroll);

            var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            scroll.AddChild(content);
            content.SetAnchorsPreset(Control.LayoutPreset.FullRect);

            // Welcome section
            CreateWelcomeSection(content);

            // Quick stats section
            var statsSection = CreateStatsSection();
            content.AddChild(statsSection);

            // Getting started section
            var gettingStarted = CreateGettingStartedSection();
            content.AddChild(gettingStarted);

            return overviewPanel;
        }

        private void CreateWelcomeSection(VBoxContainer content)
        {
            var welcomeSection = new VBoxContainer();
            content.AddChild(welcomeSection);

            var welcomeLabel = new Label
            {
                Text = "Welcome to Ascendere Framework",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            welcomeLabel.AddThemeFontSizeOverride("font_size", 18);
            welcomeSection.AddChild(welcomeLabel);

            var descLabel = new Label
            {
                Text = "A modular game development framework for Godot 4.x",
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            welcomeSection.AddChild(descLabel);

            welcomeSection.AddChild(new HSeparator { CustomMinimumSize = new Vector2(0, 20) });
        }

        private VBoxContainer CreateStatsSection()
        {
            var section = new VBoxContainer();

            var sectionTitle = new Label { Text = "Framework Status" };
            sectionTitle.AddThemeFontSizeOverride("font_size", 16);
            section.AddChild(sectionTitle);

            var statsGrid = new GridContainer { Columns = 2 };
            section.AddChild(statsGrid);

            // These will be updated dynamically
            AddStatRow(statsGrid, "Modules Loaded:", "Checking...");
            AddStatRow(statsGrid, "Systems Active:", "Checking...");
            AddStatRow(statsGrid, "Entities Registered:", "Checking...");
            AddStatRow(statsGrid, "Components:", "Checking...");

            return section;
        }

        private void AddStatRow(GridContainer grid, string label, string value)
        {
            var labelNode = new Label { Text = label };
            grid.AddChild(labelNode);

            var valueNode = new Label
            {
                Text = value,
                Name = label.Replace(":", "").Replace(" ", ""),
            };
            valueNode.AddThemeColorOverride("font_color", Colors.Cyan);
            grid.AddChild(valueNode);
        }

        private VBoxContainer CreateGettingStartedSection()
        {
            var section = new VBoxContainer();

            section.AddChild(new HSeparator { CustomMinimumSize = new Vector2(0, 20) });

            var sectionTitle = new Label { Text = "Quick Start" };
            sectionTitle.AddThemeFontSizeOverride("font_size", 16);
            section.AddChild(sectionTitle);

            var steps = new string[]
            {
                "1. Create a new scene with your game entities",
                "2. Add [MetaComponent] attributes to your component classes",
                "3. Use [MetaSystem] for game logic systems",
                "4. Configure modules in Project Settings > Ascendere",
            };

            foreach (var step in steps)
            {
                var stepLabel = new Label { Text = step };
                section.AddChild(stepLabel);
            }

            return section;
        }
    }
}
#endif
