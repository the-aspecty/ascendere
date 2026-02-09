#if TOOLS
using Godot;
using System.Collections.Generic;
using System.Linq;
using Ascendere.Utils;

namespace Ascendere.Editor
{
    /// <summary>
    /// Components panel for the Ascendere main panel.
    /// Displays discovered components with search and filtering capabilities.
    /// </summary>
    public class ComponentsPanel
    {
        private VBoxContainer _componentsVBox;
        private LineEdit _componentSearchBox;
        private string _currentComponentFilter = string.Empty;

        public Control CreatePanel()
        {
            var componentsPanel = new PanelContainer
            {
                Name = "Components",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            };
            componentsPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);

            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            };
            scroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            componentsPanel.AddChild(scroll);

            var vbox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            scroll.AddChild(vbox);
            _componentsVBox = vbox;

            // Add header bar with refresh button and search box
            CreateHeaderBar(vbox);

            // Build initial components UI
            var components = AttributeDiscovery.DiscoverComponents();
            BuildComponentsUI(vbox, components);

            return componentsPanel;
        }

        private void CreateHeaderBar(VBoxContainer vbox)
        {
            var headerBar = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };

            var refreshBtn = new Button { Text = "Refresh" };
            refreshBtn.Pressed += OnRefreshPressed;
            headerBar.AddChild(refreshBtn);

            var searchLabel = new Label { Text = "Search:" };
            headerBar.AddChild(searchLabel);

            _componentSearchBox = new LineEdit
            {
                PlaceholderText = "Filter components...",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(200, 0),
            };
            _componentSearchBox.TextChanged += OnSearchChanged;
            headerBar.AddChild(_componentSearchBox);

            vbox.AddChild(headerBar);
        }

        private void BuildComponentsUI(VBoxContainer vbox, List<MetaComponentInfo> components)
        {
            // Clear any existing entries (preserve header bar if present)
            var children = vbox.GetChildren();
            for (int i = children.Count - 1; i >= 0; i--)
            {
                var childObj = children[i];
                var child = childObj as Node;

                // Skip header bar if it's the first child and it's an HBoxContainer
                if (i == 0 && child is HBoxContainer)
                    continue;

                if (child != null)
                {
                    // Remove from parent before freeing to avoid invalid parent states
                    if (child.GetParent() == vbox)
                    {
                        vbox.RemoveChild(child);
                    }

                    if (GodotObject.IsInstanceValid(child))
                    {
                        child.QueueFree();
                    }
                }
            }

            if (components == null || components.Count == 0)
            {
                vbox.AddChild(new Label { Text = "No components discovered." });
                return;
            }

            // Filter components based on search text
            var filteredComponents = components;
            if (!string.IsNullOrEmpty(_currentComponentFilter))
            {
                var filterLower = _currentComponentFilter.ToLower();
                filteredComponents = components
                    .Where(c =>
                        c.Name.ToLower().Contains(filterLower)
                        || (
                            c.Attribute?.Description != null
                            && c.Attribute.Description.ToLower().Contains(filterLower)
                        )
                    )
                    .ToList();
            }

            if (filteredComponents.Count == 0)
            {
                vbox.AddChild(
                    new Label { Text = $"No components match '{_currentComponentFilter}'." }
                );
                return;
            }

            var grouped = filteredComponents.GroupBy(c => c.Category).OrderBy(g => g.Key);
            foreach (var group in grouped)
            {
                var categoryLabel = new Label { Text = $"Category: {group.Key}" };
                categoryLabel.AddThemeFontSizeOverride("font_size", 14);
                categoryLabel.AddThemeColorOverride("font_color", Colors.Yellow);
                vbox.AddChild(categoryLabel);

                foreach (var comp in group)
                {
                    var compRow = new HBoxContainer();
                    var nameLabel = new Label
                    {
                        Text = $"  • {comp.Name}",
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    };
                    compRow.AddChild(nameLabel);

                    if (comp.Attribute?.Description != null)
                    {
                        var descLabel = new Label
                        {
                            Text = comp.Attribute.Description,
                            AutowrapMode = TextServer.AutowrapMode.WordSmart,
                            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                        };
                        descLabel.AddThemeColorOverride("font_color", Colors.Gray);
                        compRow.AddChild(descLabel);
                    }

                    vbox.AddChild(compRow);
                }

                vbox.AddChild(new HSeparator());
            }
        }

        private void OnSearchChanged(string newText)
        {
            _currentComponentFilter = newText;
            if (_componentsVBox != null)
            {
                var components = AttributeDiscovery.DiscoverComponents();
                BuildComponentsUI(_componentsVBox, components);
            }
        }

        private void OnRefreshPressed()
        {
            if (_componentsVBox == null)
            {
                GD.Print("[ComponentsPanel] Components UI not initialized");
                return;
            }

            var components = AttributeDiscovery.DiscoverComponents();
            BuildComponentsUI(_componentsVBox, components);
            GD.Print("[ComponentsPanel] Components refreshed");
        }
    }
}
#endif
