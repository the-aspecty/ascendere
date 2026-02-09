#if TOOLS
using Godot;
using Ascendere.Editor;
using Ascendere.Editor.CustomCommands;
using Ascendere.Editor.CustomTools;
using Ascendere.Log;
using System;

[Tool]
public partial class AscenderePlugin : EditorPlugin
{
    public static AscenderePlugin Instance { get; private set; }
    public static string MainPath { get; private set; } = "res://./ascendere";

    private Control _dock; //main dock to showcase everything
    private AscendereMainPanel _mainPanel; //main screen panel (top bar)
    private EditorCommandPalette _commandPalette;
    private CommandPaletteManager _commandPaletteManager;
    private ToolMenuManager _toolMenuManager;

    //todo create the _moduleGraphEditor dock

    //create a dict here for all the autoload singletons
    private readonly Godot.Collections.Dictionary<string, string> _autoloadSingletons = new()
    {
        {
            "AscendereManager", //The main Manager
            "res://addons/ascendere/Autoload/AscendereManager.cs"
        },
    };

    //create a dict here for all the custom docks and simple config for them
    private readonly Godot.Collections.Dictionary<string, string> _customDocks = new()
    {
        //todo create docks
        //{ "AscendereMainDock", "res://addons/ascendere/Scenes/Editor/AscendereMainDock.tscn" },
    };

    public override void _EnterTree()
    {
        Instance = this;
        _commandPalette = EditorInterface.Singleton.GetCommandPalette();

        // Initialize Logger (singleton pattern, no need to store reference)
        var logger = new Ascendere.Log.Logger();
        logger.Name = "AscendereLogger";
        AddChild(logger);

        // Configure Logger settings
        Ascendere.Log.Logger.Instance.GlobalMinimumLevel = LogLevel.Debug;
        Ascendere.Log.Logger.Instance.EnableTimestamps = true;
        Ascendere.Log.Logger.Instance.EnableTypeNames = true;

        // Register LogService with ServiceLocator
        ServiceLocator.Register<Ascendere.Log.ILogService>(new Ascendere.Log.LogService());

        // Register autoload singletons
        AddAutoLoads();

        // Register Ascendere project settings for the editor
        Ascendere.Editor.EditorSettings.RegisterDefaultSettings();

        // Initialize command palette manager
        _commandPaletteManager = new CommandPaletteManager();
        _commandPaletteManager.Initialize(_commandPalette);

        // Initialize tool menu manager
        _toolMenuManager = new ToolMenuManager();
        _toolMenuManager.Initialize(this);

        // Create the main panel for the top bar (deferred to avoid timing issues)
        CallDeferred(nameof(InitializeMainPanel));

        // Create and add docks
        //todo AddCustomDocs();

        GD.Print("[Ascendere MetaFramework] Plugin initialized successfully");
    }

    private void InitializeMainPanel()
    {
        _mainPanel = new AscendereMainPanel();
        _mainPanel.Name = "AscendereMainPanel";
        EditorInterface.Singleton.GetEditorMainScreen().AddChild(_mainPanel);
        _mainPanel.Visible = false;
    }

    public override void _ExitTree()
    {
        // Clean-up Logger instance
        var loggerInstance = Ascendere.Log.Logger.Instance;
        if (loggerInstance != null && IsInstanceValid(loggerInstance))
        {
            loggerInstance.QueueFree();
        }

        // Clean-up command palette manager
        if (_commandPaletteManager != null)
        {
            _commandPaletteManager.Cleanup();
            _commandPaletteManager = null;
        }

        // Clean-up tool menu manager
        if (_toolMenuManager != null)
        {
            _toolMenuManager.Cleanup();
            _toolMenuManager = null;
        }

        _commandPalette = null;
        RemoveAutoloadSingletons();

        // Remove main panel
        if (_mainPanel != null)
        {
            if (IsInstanceValid(_mainPanel))
            {
                if (_mainPanel.GetParent() != null)
                {
                    _mainPanel.GetParent().RemoveChild(_mainPanel);
                }
                _mainPanel.QueueFree();
            }
            _mainPanel = null;
        }

        // Remove dock
        if (_dock != null)
        {
            RemoveControlFromDocks(_dock);
            _dock.QueueFree();
            _dock = null;
        }

        Instance = null;
        GD.Print("[Ascendere MetaFramework] Plugin cleanup completed");
    }

    private void AddAutoLoads()
    {
        foreach (var singleton in _autoloadSingletons)
        {
            AddAutoloadSingleton(singleton.Key, singleton.Value);
        }
    }

    private void RemoveAutoloadSingletons()
    {
        foreach (var singleton in _autoloadSingletons)
        {
            RemoveAutoloadSingleton(singleton.Key);
        }
    }

    private void AddCustomDocs()
    {
        _dock = GD.Load<PackedScene>("res://addons/ascendere/Editor/Docks/AscendereMainDock.tscn")
            .Instantiate<Control>();
        AddControlToDock(DockSlot.LeftUl, _dock);
    }

    private void RemoveCustomDocs()
    {
        if (_dock != null)
        {
            RemoveControlFromDocks(_dock);
            _dock.QueueFree();
            _dock = null;
        }
    }

    /// <summary>
    /// Checks if the plugin is currently active and available.
    /// </summary>
    /// <returns>True if the plugin instance is available, false otherwise.</returns>
    public static bool IsAvailable => Instance != null;

    #region Main Screen Methods (Top Bar Panel)

    /// <summary>
    /// Indicates that this plugin has a main screen tab.
    /// </summary>
    public override bool _HasMainScreen() => true;

    /// <summary>
    /// Returns the name displayed in the top bar tab.
    /// </summary>
    public override string _GetPluginName() => "Ascendere";

    /// <summary>
    /// Returns the icon for the top bar tab.
    /// </summary>
    public override Texture2D _GetPluginIcon()
    {
        // Try to load custom icon, fallback to editor icon
        var customIcon = GD.Load<Texture2D>(
            "res://addons/ascendere/Assets/Icons/module_manager.svg"
        );
        if (customIcon != null)
            return customIcon;

        // Fallback to a built-in editor icon
        return EditorInterface.Singleton.GetEditorTheme().GetIcon("Node", "EditorIcons");
    }

    /// <summary>
    /// Called when the main screen tab is selected/deselected.
    /// </summary>
    public override void _MakeVisible(bool visible)
    {
        if (_mainPanel != null && IsInstanceValid(_mainPanel))
        {
            _mainPanel.Visible = visible;
        }
    }

    #endregion
}
#endif
