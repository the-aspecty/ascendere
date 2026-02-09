#if TOOLS
using Godot;
using System;
using Ascendere.Utils;

[Tool]
public partial class AscendereMainDock : Control
{
    private VBoxContainer _mainContainer;
    private ScrollContainer _contentContainer;

    //private MetaDiscoveryResult _lastDiscoveryResult;
    private bool _isRefreshing = false;
    private Label _statusLabel;

    public override void _Ready()
    {
        InitializeUI();
        PerformDiscovery();
    }

    private void InitializeUI()
    {
        // Main container setup
        _mainContainer = new VBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        AddChild(_mainContainer);

        // Header with title and refresh button
        var header = new HBoxContainer();
        _mainContainer.AddChild(header);

        var title = new Label
        {
            Text = "Meta Framework",
            HorizontalAlignment = HorizontalAlignment.Left,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        title.AddThemeFontSizeOverride("font_size", 14);
        title.AddThemeColorOverride("font_color", Colors.White);
        header.AddChild(title);

        var refreshBtn = new Button
        {
            Text = "Refresh",
            TooltipText = "Refresh Meta Framework discovery",
            Disabled = false,
        };
        refreshBtn.Pressed += OnRefreshPressed;
        header.AddChild(refreshBtn);

        // Status label
        _statusLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Visible = false,
        };
        _mainContainer.AddChild(_statusLabel);

        // Separator
        _mainContainer.AddChild(new HSeparator());

        // Content area
        _contentContainer = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(200, 300),
        };
        _mainContainer.AddChild(_contentContainer);
    }

    private void PerformDiscovery()
    {
        if (_isRefreshing)
            return;

        _isRefreshing = true;
        SetStatus("Discovering meta types...", Colors.Yellow);
        GD.Print("[AscendereMainDock] Starting discovery...");

        try { }
        catch (Exception ex)
        {
            GD.PrintErr($"[AscendereMainDock] Discovery error: {ex}");
            ShowErrorUI($"Discovery failed: {ex.Message}");
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void ClearContentContainer()
    {
        foreach (Node child in _contentContainer.GetChildren())
        {
            _contentContainer.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void ShowErrorUI(string message)
    {
        ClearContentContainer();

        var errorLabel = new Label
        {
            Text = $"Error: {message}",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        errorLabel.AddThemeColorOverride("font_color", Colors.Red);
        _contentContainer.AddChild(errorLabel);

        SetStatus(message, Colors.Red);
    }

    private void SetStatus(string message, Color color)
    {
        _statusLabel.Text = message;
        _statusLabel.AddThemeColorOverride("font_color", color);
        _statusLabel.Visible = !string.IsNullOrEmpty(message);
    }

    private void OnRefreshPressed()
    {
        if (!_isRefreshing)
        {
            PerformDiscovery();
        }
    }
}
#endif
