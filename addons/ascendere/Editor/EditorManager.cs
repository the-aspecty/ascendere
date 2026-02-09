using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Ascendere.Editor
{
    /// <summary>
    /// Provides utility methods for managing Godot editor windows and dock panels.
    /// This class serves as a high-level interface for editor integration within the Ascendere framework.
    ///
    /// Usage example:
    /// <code>
    /// var dock = EditorManager.CreateDockPanel("MyDock", new VBoxContainer());
    /// EditorManager.AddDockToEditor(dock, EditorManager.DockSlot.LeftBr);
    /// </code>
    ///
    /// Threading: All methods must be called from the main thread.
    /// Performance: Dock operations are cached to prevent unnecessary editor calls.
    /// Security: Input validation is performed on all public methods.
    /// </summary>
    public static class EditorManager
    {
        #region Constants

        private const string LogPrefix = "[Ascendere]";
        private const int DefaultMinDockWidth = 200;
        private const int DefaultMinDockHeight = 100;
        private const int DefaultMargin = 8;

        #endregion
        #region Private Fields

        private static readonly Dictionary<string, Control> _registeredDocks = new();
        private static readonly Dictionary<Control, DockSlot> _dockSlots = new();
        private static readonly object _lock = new object();

        #endregion

        #region Events

        /// <summary>
        /// Fired when a dock panel is successfully added to the editor.
        /// </summary>
        public static event Action<string, DockSlot> DockAdded;

        /// <summary>
        /// Fired when a dock panel is successfully removed from the editor.
        /// </summary>
        public static event Action<string> DockRemoved;

        /// <summary>
        /// Fired when dock management operations fail.
        /// </summary>
        public static event Action<string, string> DockOperationFailed;

        #endregion

        #region Public Enums

        /// <summary>
        /// Dock slot positions in the Godot editor.
        /// Maps to Godot's EditorPlugin.DockSlot enum.
        /// </summary>
        public enum DockSlot
        {
            /// <summary>Left Upper slot</summary>
            LeftUl = 0,

            /// <summary>Left Lower slot</summary>
            LeftBl = 1,

            /// <summary>Left Bottom Right slot</summary>
            LeftBr = 2,

            /// <summary>Right Upper slot</summary>
            RightUl = 3,

            /// <summary>Right Lower slot</summary>
            RightBl = 4,

            /// <summary>Right Bottom Right slot</summary>
            RightBr = 5,

            /// <summary>Bottom slot</summary>
            Bottom = 6,
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets whether the editor plugin is currently available.
        /// </summary>
        public static bool IsEditorAvailable
        {
            get
            {
#if TOOLS
                return AscenderePlugin.IsAvailable;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Gets the number of currently registered dock panels.
        /// </summary>
        public static int RegisteredDocksCount => _registeredDocks.Count;

        /// <summary>
        /// Gets the names of all registered dock panels.
        /// </summary>
        public static IEnumerable<string> RegisteredDockNames => _registeredDocks.Keys;

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a new dock panel with the specified name and content.
        /// The panel is automatically wrapped in a PanelContainer for consistent styling.
        /// </summary>
        /// <param name="name">The unique name of the dock panel. Must not be null or empty.</param>
        /// <param name="content">The Control node to display in the dock. Must not be null.</param>
        /// <returns>The created Control node representing the dock panel.</returns>
        /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a dock with the same name already exists.</exception>
        public static Control CreateDockPanel(string name, Control content)
        {
            ValidateCreateDockPanelParameters(name, content);

            lock (_lock)
            {
                if (_registeredDocks.ContainsKey(name))
                {
                    throw new InvalidOperationException(
                        $"[MetaFramework] Dock panel with name '{name}' already exists."
                    );
                }
            }

            var panel = new PanelContainer
            {
                Name = name,
                CustomMinimumSize = new Vector2(DefaultMinDockWidth, DefaultMinDockHeight),
            };

            // Add a margin container for better content spacing
            var marginContainer = new MarginContainer();
            marginContainer.AddThemeConstantOverride("margin_left", DefaultMargin);
            marginContainer.AddThemeConstantOverride("margin_right", DefaultMargin);
            marginContainer.AddThemeConstantOverride("margin_top", DefaultMargin);
            marginContainer.AddThemeConstantOverride("margin_bottom", DefaultMargin);

            marginContainer.AddChild(content);
            panel.AddChild(marginContainer);

            GD.Print($"{LogPrefix} Created dock panel: {name}");
            return panel;
        }

        /// <summary>
        /// Adds a dock panel to the Godot editor at the specified slot.
        /// The dock is automatically registered for management and cleanup.
        /// </summary>
        /// <param name="dockPanel">The dock panel to add. Must not be null.</param>
        /// <param name="slot">The dock slot position.</param>
        /// <returns>True if the dock was successfully added, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when dockPanel is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the editor plugin is not available.</exception>
        public static bool AddDockToEditor(Control dockPanel, DockSlot slot)
        {
            if (dockPanel == null)
                throw new ArgumentNullException(nameof(dockPanel));

            if (!IsEditorAvailable)
            {
                var errorMsg = "Cannot add dock: Editor plugin not available.";
                GD.PrintErr($"{LogPrefix} {errorMsg}");
                DockOperationFailed?.Invoke(dockPanel.Name, errorMsg);
                return false;
            }

            lock (_lock)
            {
                if (_registeredDocks.ContainsKey(dockPanel.Name))
                {
                    GD.PushWarning(
                        $"[MetaFramework] Dock '{dockPanel.Name}' is already registered. Skipping addition."
                    );
                    return false;
                }
            }

#if TOOLS
            try
            {
                var plugin = AscenderePlugin.Instance;
                plugin.AddControlToDock((EditorPlugin.DockSlot)slot, dockPanel);

                // Register the dock for management
                lock (_lock)
                {
                    _registeredDocks[dockPanel.Name] = dockPanel;
                    _dockSlots[dockPanel] = slot;
                }

                GD.Print(
                    $"{LogPrefix} Successfully added dock panel '{dockPanel.Name}' to slot {slot}"
                );
                DockAdded?.Invoke(dockPanel.Name, slot);
                return true;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Failed to add dock panel '{dockPanel.Name}': {ex.Message}";
                GD.PrintErr($"[MetaFramework] {errorMsg}");
                DockOperationFailed?.Invoke(dockPanel.Name, errorMsg);
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// Removes a dock panel from the Godot editor and cleans up resources.
        /// </summary>
        /// <param name="dockPanel">The dock panel to remove. Must not be null.</param>
        /// <returns>True if the dock was successfully removed, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when dockPanel is null.</exception>
        public static bool RemoveDockFromEditor(Control dockPanel)
        {
            if (dockPanel == null)
                throw new ArgumentNullException(nameof(dockPanel));

            if (!IsEditorAvailable)
            {
                GD.PushWarning("[MetaFramework] Cannot remove dock: Editor plugin not available.");
                return false;
            }

            lock (_lock)
            {
                if (!_registeredDocks.ContainsKey(dockPanel.Name))
                {
                    GD.PushWarning(
                        $"[MetaFramework] Dock '{dockPanel.Name}' is not registered. Cannot remove."
                    );
                    return false;
                }
            }

#if TOOLS
            try
            {
                var plugin = AscenderePlugin.Instance;
                plugin.RemoveControlFromDocks(dockPanel);

                // Unregister the dock
                lock (_lock)
                {
                    _registeredDocks.Remove(dockPanel.Name);
                    _dockSlots.Remove(dockPanel);
                }

                // Clean up the control
                if (GodotObject.IsInstanceValid(dockPanel))
                {
                    dockPanel.QueueFree();
                }

                GD.Print($"[MetaFramework] Successfully removed dock panel '{dockPanel.Name}'");
                DockRemoved?.Invoke(dockPanel.Name);
                return true;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Failed to remove dock panel '{dockPanel.Name}': {ex.Message}";
                GD.PrintErr($"[MetaFramework] {errorMsg}");
                DockOperationFailed?.Invoke(dockPanel.Name, errorMsg);
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// Removes a dock panel by name.
        /// </summary>
        /// <param name="dockName">The name of the dock panel to remove.</param>
        /// <returns>True if the dock was found and removed, false otherwise.</returns>
        public static bool RemoveDockFromEditor(string dockName)
        {
            if (string.IsNullOrEmpty(dockName))
            {
                GD.PushWarning("[MetaFramework] Cannot remove dock: dock name is null or empty.");
                return false;
            }

            if (_registeredDocks.TryGetValue(dockName, out var dockPanel))
            {
                return RemoveDockFromEditor(dockPanel);
            }

            GD.PushWarning($"[MetaFramework] Dock '{dockName}' not found for removal.");
            return false;
        }

        /// <summary>
        /// Gets a registered dock panel by name.
        /// </summary>
        /// <param name="dockName">The name of the dock panel.</param>
        /// <returns>The dock panel if found, null otherwise.</returns>
        public static Control GetDockPanel(string dockName)
        {
            if (string.IsNullOrEmpty(dockName))
                return null;

            _registeredDocks.TryGetValue(dockName, out var dockPanel);
            return dockPanel;
        }

        /// <summary>
        /// Gets the dock slot for a registered dock panel.
        /// </summary>
        /// <param name="dockPanel">The dock panel.</param>
        /// <returns>The dock slot if the panel is registered, null otherwise.</returns>
        public static DockSlot? GetDockSlot(Control dockPanel)
        {
            if (dockPanel == null)
                return null;

            return _dockSlots.TryGetValue(dockPanel, out var slot) ? slot : null;
        }

        /// <summary>
        /// Removes all registered dock panels from the editor.
        /// This method is typically called during plugin cleanup.
        /// </summary>
        public static void RemoveAllDocks()
        {
            if (_registeredDocks.Count == 0)
                return;

            GD.Print(
                $"[MetaFramework] Removing {_registeredDocks.Count} registered dock panels..."
            );

            // Create a copy of the collection to avoid modification during iteration
            var docksToRemove = new List<Control>(_registeredDocks.Values);

            foreach (var dock in docksToRemove)
            {
                RemoveDockFromEditor(dock);
            }

            GD.Print("[MetaFramework] All dock panels removed.");
        }

        /// <summary>
        /// Checks if a dock panel with the specified name is registered.
        /// </summary>
        /// <param name="dockName">The name of the dock panel.</param>
        /// <returns>True if the dock is registered, false otherwise.</returns>
        public static bool IsDockRegistered(string dockName)
        {
            if (string.IsNullOrEmpty(dockName))
                return false;

            lock (_lock)
            {
                return _registeredDocks.ContainsKey(dockName);
            }
        }

        /// <summary>
        /// Gets all registered dock panels grouped by their dock slots.
        /// </summary>
        /// <returns>A dictionary mapping dock slots to lists of dock panel names.</returns>
        public static IReadOnlyDictionary<DockSlot, List<string>> GetDocksBySlot()
        {
            lock (_lock)
            {
                var result = new Dictionary<DockSlot, List<string>>();

                foreach (var kvp in _dockSlots)
                {
                    if (!result.ContainsKey(kvp.Value))
                        result[kvp.Value] = new List<string>();

                    result[kvp.Value].Add(kvp.Key.Name);
                }

                return result;
            }
        }

        /// <summary>
        /// Validates that a dock panel is properly configured and accessible.
        /// </summary>
        /// <param name="dockPanel">The dock panel to validate.</param>
        /// <returns>True if the dock panel is valid, false otherwise.</returns>
        public static bool ValidateDockPanel(Control dockPanel)
        {
            if (dockPanel == null)
                return false;

            if (!GodotObject.IsInstanceValid(dockPanel))
                return false;

            if (string.IsNullOrEmpty(dockPanel.Name))
                return false;

            return true;
        }

        /// <summary>
        /// Creates a simple dock panel with a label for quick prototyping.
        /// </summary>
        /// <param name="name">The name of the dock panel.</param>
        /// <param name="text">The text to display in the label.</param>
        /// <returns>The created dock panel.</returns>
        public static Control CreateSimpleDockPanel(string name, string text)
        {
            var label = new Label
            {
                Text = text ?? name,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            return CreateDockPanel(name, label);
        }

        /// <summary>
        /// Attempts to safely remove all docks that match a predicate.
        /// </summary>
        /// <param name="predicate">The predicate to match dock names.</param>
        /// <returns>The number of docks successfully removed.</returns>
        public static int RemoveDocksWhere(Func<string, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            List<string> docksToRemove;

            lock (_lock)
            {
                docksToRemove = _registeredDocks.Keys.Where(predicate).ToList();
            }

            int removedCount = 0;
            foreach (var dockName in docksToRemove)
            {
                if (RemoveDockFromEditor(dockName))
                    removedCount++;
            }

            return removedCount;
        }

        /// <summary>
        /// Gets diagnostic information about the current state of the editor manager.
        /// Useful for debugging and monitoring dock management.
        /// </summary>
        /// <returns>A formatted string containing diagnostic information.</returns>
        public static string GetDiagnosticInfo()
        {
            lock (_lock)
            {
                var info = new System.Text.StringBuilder();
                info.AppendLine($"{LogPrefix} Editor Manager Diagnostics");
                info.AppendLine($"Editor Available: {IsEditorAvailable}");
                info.AppendLine($"Total Registered Docks: {_registeredDocks.Count}");
                info.AppendLine($"Dock Slots Used: {_dockSlots.Values.Distinct().Count()}");

                if (_registeredDocks.Count > 0)
                {
                    info.AppendLine("Registered Docks:");
                    foreach (var kvp in _registeredDocks)
                    {
                        var slot = _dockSlots.TryGetValue(kvp.Value, out var dockSlot)
                            ? dockSlot.ToString()
                            : "Unknown";
                        var isValid = GodotObject.IsInstanceValid(kvp.Value) ? "Valid" : "Invalid";
                        info.AppendLine($"  - {kvp.Key} (Slot: {slot}, State: {isValid})");
                    }
                }

                return info.ToString();
            }
        }

        /// <summary>
        /// Performs a health check on all registered dock panels and reports any issues.
        /// </summary>
        /// <returns>A list of health check results.</returns>
        public static List<string> PerformHealthCheck()
        {
            var issues = new List<string>();

            lock (_lock)
            {
                foreach (var kvp in _registeredDocks)
                {
                    if (!GodotObject.IsInstanceValid(kvp.Value))
                    {
                        issues.Add($"Dock '{kvp.Key}' has an invalid Control reference");
                    }

                    if (string.IsNullOrEmpty(kvp.Value.Name))
                    {
                        issues.Add($"Dock '{kvp.Key}' has an empty name");
                    }

                    if (!_dockSlots.ContainsKey(kvp.Value))
                    {
                        issues.Add($"Dock '{kvp.Key}' is missing slot information");
                    }
                }

                // Check for orphaned slot references
                foreach (var slotKvp in _dockSlots)
                {
                    if (!_registeredDocks.ContainsValue(slotKvp.Key))
                    {
                        issues.Add($"Found orphaned slot reference for dock");
                    }
                }
            }

            return issues;
        }

        #endregion
        #region Private Methods

        /// <summary>
        /// Validates parameters for CreateDockPanel method.
        /// </summary>
        private static void ValidateCreateDockPanelParameters(string name, Control content)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(
                    "Dock panel name cannot be null or empty.",
                    nameof(name)
                );

            if (content == null)
                throw new ArgumentNullException(
                    nameof(content),
                    "Dock panel content cannot be null."
                );
        }

        #endregion
    }
}
