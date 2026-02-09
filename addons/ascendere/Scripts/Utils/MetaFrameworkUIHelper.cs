#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Ascendere.Utils
{
    /// <summary>
    /// Utility class for creating UI elements to display Meta Framework discovery results.
    /// </summary>
    public static class MetaFrameworkUIHelper
    {
        /// <summary>
        /// Creates a complete UI tree for displaying Meta Framework discovery results.
        /// </summary>
        /// <param name="result">The discovery result to display.</param>
        /// <returns>A VBoxContainer with the complete UI.</returns>
        public static VBoxContainer CreateMetaFrameworkUI(MetaDiscoveryResult result)
        {
            var mainContainer = new VBoxContainer();
            mainContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            mainContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            // Header
            var headerLabel = new Label
            {
                Text = "Meta Framework Discovery",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            headerLabel.AddThemeStyleboxOverride("normal", CreateHeaderStylebox());
            headerLabel.AddThemeColorOverride("font_color", Colors.White);
            headerLabel.AddThemeFontSizeOverride("font_size", 16);
            mainContainer.AddChild(headerLabel);

            // Summary info
            var summaryLabel = new Label
            {
                Text =
                    $"Found: {result.Components.Count} Components, {result.Entities.Count} Entities, {result.Systems.Count} Systems",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            summaryLabel.AddThemeStyleboxOverride("normal", CreateSummaryStylebox());
            summaryLabel.AddThemeColorOverride("font_color", Colors.LightGray);
            mainContainer.AddChild(summaryLabel);

            // Add spacing
            mainContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });

            // Create tabs for different types
            var tabContainer = new TabContainer();
            tabContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            tabContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            tabContainer.CustomMinimumSize = new Vector2(200, 200);
            mainContainer.AddChild(tabContainer);

            // Components tab
            var componentsTab = CreateComponentsTab(result.Components);
            componentsTab.Name = "Components";
            tabContainer.AddChild(componentsTab);

            // Entities tab
            var entitiesTab = CreateEntitiesTab(result.Entities);
            entitiesTab.Name = "Entities";
            tabContainer.AddChild(entitiesTab);

            // Systems tab
            var systemsTab = CreateSystemsTab(result.Systems);
            systemsTab.Name = "Systems";
            tabContainer.AddChild(systemsTab);

            return mainContainer;
        }

        /// <summary>
        /// Creates the components tab UI.
        /// </summary>
        /// <param name="components">List of discovered components.</param>
        /// <returns>A scroll container with components list.</returns>
        public static ScrollContainer CreateComponentsTab(List<MetaComponentInfo> components)
        {
            var scrollContainer = new ScrollContainer();
            var vbox = new VBoxContainer();
            scrollContainer.AddChild(vbox);

            if (components.Count == 0)
            {
                vbox.AddChild(new Label { Text = "No components found." });
                return scrollContainer;
            }

            // Group by category
            var groupedComponents = components.GroupBy(c => c.Category).OrderBy(g => g.Key);

            foreach (var group in groupedComponents)
            {
                // Category header
                var categoryLabel = new Label { Text = $"Category: {group.Key}" };
                categoryLabel.AddThemeStyleboxOverride("normal", CreateCategoryStylebox());
                categoryLabel.AddThemeColorOverride("font_color", Colors.Yellow);
                categoryLabel.AddThemeFontSizeOverride("font_size", 14);
                vbox.AddChild(categoryLabel);

                // Components in category
                foreach (var component in group.OrderBy(c => c.Name))
                {
                    var componentPanel = CreateComponentPanel(component);
                    vbox.AddChild(componentPanel);
                }

                // Add spacing between categories
                vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
            }

            return scrollContainer;
        }

        /// <summary>
        /// Creates the entities tab UI.
        /// </summary>
        /// <param name="entities">List of discovered entities.</param>
        /// <returns>A scroll container with entities list.</returns>
        private static ScrollContainer CreateEntitiesTab(List<MetaEntityInfo> entities)
        {
            var scrollContainer = new ScrollContainer();
            var vbox = new VBoxContainer();
            scrollContainer.AddChild(vbox);

            if (entities.Count == 0)
            {
                vbox.AddChild(new Label { Text = "No entities found." });
                return scrollContainer;
            }

            // Group by category
            var groupedEntities = entities.GroupBy(e => e.Category).OrderBy(g => g.Key);

            foreach (var group in groupedEntities)
            {
                // Category header
                var categoryLabel = new Label { Text = $"Category: {group.Key}" };
                categoryLabel.AddThemeStyleboxOverride("normal", CreateCategoryStylebox());
                categoryLabel.AddThemeColorOverride("font_color", Colors.Yellow);
                categoryLabel.AddThemeFontSizeOverride("font_size", 14);
                vbox.AddChild(categoryLabel);

                // Entities in category
                foreach (var entity in group.OrderBy(e => e.Name))
                {
                    var entityPanel = CreateEntityPanel(entity);
                    vbox.AddChild(entityPanel);
                }

                // Add spacing between categories
                vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
            }

            return scrollContainer;
        }

        /// <summary>
        /// Creates the systems tab UI.
        /// </summary>
        /// <param name="systems">List of discovered systems.</param>
        /// <returns>A scroll container with systems list.</returns>
        private static ScrollContainer CreateSystemsTab(List<MetaSystemInfo> systems)
        {
            var scrollContainer = new ScrollContainer();
            var vbox = new VBoxContainer();
            scrollContainer.AddChild(vbox);

            if (systems.Count == 0)
            {
                vbox.AddChild(new Label { Text = "No systems found." });
                return scrollContainer;
            }

            // Sort by priority (highest first)
            var sortedSystems = systems.OrderByDescending(s => s.Priority).ThenBy(s => s.Name);

            foreach (var system in sortedSystems)
            {
                var systemPanel = CreateSystemPanel(system);
                vbox.AddChild(systemPanel);
            }

            return scrollContainer;
        }

        /// <summary>
        /// Creates a panel for displaying component information.
        /// </summary>
        /// <param name="component">The component information.</param>
        /// <returns>A panel container with component details.</returns>
        private static PanelContainer CreateComponentPanel(MetaComponentInfo component)
        {
            var panel = new PanelContainer();
            panel.AddThemeStyleboxOverride("panel", CreateItemStylebox());

            var vbox = new VBoxContainer();
            panel.AddChild(vbox);

            // Component name
            var nameLabel = new Label { Text = component.Name };
            nameLabel.AddThemeColorOverride("font_color", Colors.LightBlue);
            nameLabel.AddThemeFontSizeOverride("font_size", 12);
            vbox.AddChild(nameLabel);

            // Component description (if any)
            if (!string.IsNullOrEmpty(component.Attribute?.Description))
            {
                var descLabel = new Label
                {
                    Text = component.Attribute.Description,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                };
                descLabel.AddThemeColorOverride("font_color", Colors.LightGray);
                descLabel.AddThemeFontSizeOverride("font_size", 10);
                vbox.AddChild(descLabel);
            }

            // Component type and details
            var detailsLabel = new Label
            {
                Text =
                    $"Type: {component.ComponentType} | Multiple: {(component.AllowMultiple ? "Yes" : "No")}",
            };
            detailsLabel.AddThemeColorOverride("font_color", Colors.LightGray);
            detailsLabel.AddThemeFontSizeOverride("font_size", 10);
            vbox.AddChild(detailsLabel);

            // Type name
            var typeLabel = new Label { Text = $"Class: {component.TypeName}" };
            typeLabel.AddThemeColorOverride("font_color", Colors.Gray);
            typeLabel.AddThemeFontSizeOverride("font_size", 9);
            vbox.AddChild(typeLabel);

            return panel;
        }

        /// <summary>
        /// Creates a panel for displaying entity information.
        /// </summary>
        /// <param name="entity">The entity information.</param>
        /// <returns>A panel container with entity details.</returns>
        private static PanelContainer CreateEntityPanel(MetaEntityInfo entity)
        {
            var panel = new PanelContainer();
            panel.AddThemeStyleboxOverride("panel", CreateItemStylebox());

            var vbox = new VBoxContainer();
            panel.AddChild(vbox);

            // Entity name
            var nameLabel = new Label { Text = entity.Name };
            nameLabel.AddThemeColorOverride("font_color", Colors.LightGreen);
            nameLabel.AddThemeFontSizeOverride("font_size", 12);
            vbox.AddChild(nameLabel);

            // Entity description
            if (!string.IsNullOrEmpty(entity.Description))
            {
                var descLabel = new Label
                {
                    Text = entity.Description,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                };
                descLabel.AddThemeColorOverride("font_color", Colors.LightGray);
                descLabel.AddThemeFontSizeOverride("font_size", 10);
                vbox.AddChild(descLabel);
            }

            // Entity details
            var detailsLabel = new Label
            {
                Text = $"Auto-Register: {(entity.AutoRegister ? "Yes" : "No")}",
            };
            detailsLabel.AddThemeColorOverride("font_color", Colors.LightGray);
            detailsLabel.AddThemeFontSizeOverride("font_size", 10);
            vbox.AddChild(detailsLabel);

            // Type name
            var typeLabel = new Label { Text = $"Class: {entity.TypeName}" };
            typeLabel.AddThemeColorOverride("font_color", Colors.Gray);
            typeLabel.AddThemeFontSizeOverride("font_size", 9);
            vbox.AddChild(typeLabel);

            return panel;
        }

        /// <summary>
        /// Creates a panel for displaying system information.
        /// </summary>
        /// <param name="system">The system information.</param>
        /// <returns>A panel container with system details.</returns>
        private static PanelContainer CreateSystemPanel(MetaSystemInfo system)
        {
            var panel = new PanelContainer();
            panel.AddThemeStyleboxOverride("panel", CreateItemStylebox());

            var vbox = new VBoxContainer();
            panel.AddChild(vbox);

            // System name
            var nameLabel = new Label { Text = system.Name };
            nameLabel.AddThemeColorOverride("font_color", Colors.Orange);
            nameLabel.AddThemeFontSizeOverride("font_size", 12);
            vbox.AddChild(nameLabel);

            // System priority and details
            var detailsLabel = new Label
            {
                Text =
                    $"Priority: {system.Priority} ({system.PriorityDescription}) | Frame: {(system.ProcessEveryFrame ? "Yes" : "No")} | Physics: {(system.NeedsPhysicsProcess ? "Yes" : "No")}",
            };
            detailsLabel.AddThemeColorOverride("font_color", Colors.LightGray);
            detailsLabel.AddThemeFontSizeOverride("font_size", 10);
            vbox.AddChild(detailsLabel);

            // Type name
            var typeLabel = new Label { Text = $"Class: {system.TypeName}" };
            typeLabel.AddThemeColorOverride("font_color", Colors.Gray);
            typeLabel.AddThemeFontSizeOverride("font_size", 9);
            vbox.AddChild(typeLabel);

            return panel;
        }

        /// <summary>
        /// Creates a stylebox for headers.
        /// </summary>
        /// <returns>A StyleBoxFlat for headers.</returns>
        private static StyleBoxFlat CreateHeaderStylebox()
        {
            var stylebox = new StyleBoxFlat();
            stylebox.BgColor = new Color(0.2f, 0.3f, 0.4f, 1.0f);
            stylebox.SetBorderWidthAll(1);
            stylebox.BorderColor = new Color(0.4f, 0.5f, 0.6f, 1.0f);
            stylebox.SetContentMarginAll(8);
            stylebox.SetCornerRadiusAll(4);
            return stylebox;
        }

        /// <summary>
        /// Creates a stylebox for summary information.
        /// </summary>
        /// <returns>A StyleBoxFlat for summary.</returns>
        private static StyleBoxFlat CreateSummaryStylebox()
        {
            var stylebox = new StyleBoxFlat();
            stylebox.BgColor = new Color(0.15f, 0.15f, 0.15f, 1.0f);
            stylebox.SetBorderWidthAll(1);
            stylebox.BorderColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
            stylebox.SetContentMarginAll(6);
            stylebox.SetCornerRadiusAll(3);
            return stylebox;
        }

        /// <summary>
        /// Creates a stylebox for category headers.
        /// </summary>
        /// <returns>A StyleBoxFlat for categories.</returns>
        private static StyleBoxFlat CreateCategoryStylebox()
        {
            var stylebox = new StyleBoxFlat();
            stylebox.BgColor = new Color(0.3f, 0.3f, 0.1f, 1.0f);
            stylebox.SetBorderWidthAll(1);
            stylebox.BorderColor = new Color(0.5f, 0.5f, 0.2f, 1.0f);
            stylebox.SetContentMarginAll(6);
            stylebox.SetCornerRadiusAll(3);
            return stylebox;
        }

        /// <summary>
        /// Creates a stylebox for item panels.
        /// </summary>
        /// <returns>A StyleBoxFlat for items.</returns>
        private static StyleBoxFlat CreateItemStylebox()
        {
            var stylebox = new StyleBoxFlat();
            stylebox.BgColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);
            stylebox.SetBorderWidthAll(1);
            stylebox.BorderColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
            stylebox.SetContentMarginAll(8);
            stylebox.SetCornerRadiusAll(2);
            return stylebox;
        }
    }
}
#endif
