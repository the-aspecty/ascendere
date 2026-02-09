#if TOOLS
using Godot;

namespace Ascendere.Editor
{
    /// <summary>
    /// Main editor panel for Ascendere, displayed in the top bar alongside 2D, 3D, and AssetLib.
    /// Coordinates panel creation and delegates tab management to PanelsManager.
    /// </summary>
    [Tool]
    public partial class AscendereMainPanel : Control
    {
        private const int DefaultMargin = 8;
        private VBoxContainer _mainContainer;
        private TabContainer _tabContainer;

        public override void _Ready()
        {
            InitializeUI();
        }

        public override void _ExitTree()
        {
            PanelsManager.Instance.Cleanup();
            base._ExitTree();
        }

        private void InitializeUI()
        {
            // Set anchors to fill the entire area
            SetAnchorsPreset(LayoutPreset.FullRect);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;

            // Main container
            _mainContainer = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            _mainContainer.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(_mainContainer);

            // Add a margin container around the content to provide padding
            var marginContainer = new MarginContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            marginContainer.SetAnchorsPreset(LayoutPreset.FullRect);
            marginContainer.AddThemeConstantOverride("margin_left", DefaultMargin);
            marginContainer.AddThemeConstantOverride("margin_right", DefaultMargin);
            marginContainer.AddThemeConstantOverride("margin_top", DefaultMargin);
            marginContainer.AddThemeConstantOverride("margin_bottom", DefaultMargin);
            _mainContainer.AddChild(marginContainer);

            // Header
            var header = CreateHeader();
            marginContainer.AddChild(header);

            // Separator
            marginContainer.AddChild(new HSeparator());

            // Tab container for different sections
            _tabContainer = new TabContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            _tabContainer.SetAnchorsPreset(LayoutPreset.FullRect);
            marginContainer.AddChild(_tabContainer);

            // Initialize panels manager
            PanelsManager.Instance.Initialize(_tabContainer);

            // Create built-in panels
            CreateBuiltInPanels();

            // Example custom tabs (commented out - modules can register their own)
            // Control customTabContent = new Control();
            // PanelsManager.Instance.RegisterTab("input_tab", "Input", customTabContent);

            //create a control with a text
            Control customTabContent = new Label() { Text = "Input" };
            Control customTabContent2 = new Label() { Text = "Services" };
            Control customTabContent3 = new Label() { Text = "Config" };

            //@todo pick input
            PanelsManager.Instance.RegisterTab("input_tab", "Input", customTabContent);
            //@todo pick services
            PanelsManager.Instance.RegisterTab("services_tab", "Services", customTabContent2);
            //@todo pick config
            PanelsManager.Instance.RegisterTab("config_tab", "Config", customTabContent3);
        }

        private void CreateBuiltInPanels()
        {
            // Create core panels
            var overviewPanel = new OverviewPanel();
            _tabContainer.AddChild(overviewPanel.CreatePanel());

            var componentsPanel = new ComponentsPanel();
            _tabContainer.AddChild(componentsPanel.CreatePanel());

            var modulesPanel = new ModulesPanel();
            _tabContainer.AddChild(modulesPanel.CreatePanel());

            //var toolsPanel = new ToolsPanel();
            // _tabContainer.AddChild(toolsPanel.CreatePanel());

            var settingsPanel = new SettingsPanel();
            _tabContainer.AddChild(settingsPanel.CreatePanel());
        }

        private HBoxContainer CreateHeader()
        {
            var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            // Logo/Title area
            var titleContainer = new HBoxContainer();
            header.AddChild(titleContainer);

            var titleLabel = new Label
            {
                Text = "Ascendere Framework",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            titleLabel.AddThemeFontSizeOverride("font_size", 20);
            titleContainer.AddChild(titleLabel);

            // Spacer
            var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            header.AddChild(spacer);

            // Quick actions
            var actionsContainer = new HBoxContainer();
            header.AddChild(actionsContainer);

            var refreshBtn = new Button
            {
                Text = "Refresh",
                TooltipText = "Refresh framework discovery",
            };
            refreshBtn.Pressed += OnRefreshPressed;
            actionsContainer.AddChild(refreshBtn);

            var docsBtn = new Button
            {
                Text = "Documentation",
                TooltipText = "Open Ascendere documentation",
            };
            docsBtn.Pressed += OnDocsPressed;
            actionsContainer.AddChild(docsBtn);

            return header;
        }

        #region Event Handlers

        private void OnRefreshPressed()
        {
            GD.Print("[AscendereMainPanel] Refreshing framework discovery...");
        }

        private void OnDocsPressed()
        {
            OS.ShellOpen("https://github.com/the-aspecty/ascendere/wiki");
        }

        #endregion
    }
}
#endif
