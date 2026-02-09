#if TOOLS
using Godot;
using System.Collections.Generic;

namespace Ascendere.Editor
{
    /// <summary>
    /// Manages panel tabs for the Ascendere main panel.
    /// Provides public API for modules to register custom tabs.
    /// </summary>
    public class PanelsManager
    {
        private static PanelsManager _instance;
        public static PanelsManager Instance => _instance ??= new PanelsManager();

        private TabContainer _tabContainer;
        private readonly Dictionary<string, TabInfo> _registeredTabs =
            new Dictionary<string, TabInfo>();

        /// <summary>
        /// Initializes the panels manager with the tab container
        /// </summary>
        public void Initialize(TabContainer tabContainer)
        {
            _tabContainer = tabContainer;
            GD.Print("[PanelsManager] Initialized");
        }

        /// <summary>
        /// Cleans up the panels manager
        /// </summary>
        public void Cleanup()
        {
            _registeredTabs.Clear();
            _tabContainer = null;
            GD.Print("[PanelsManager] Cleaned up");
        }

        #region Public API

        /// <summary>
        /// Register a new tab to be added to the panel. Called by modules to contribute custom tabs.
        /// </summary>
        /// <param name="tabId">Unique identifier for the tab (e.g., "module_name_tab")</param>
        /// <param name="tabName">Display name of the tab</param>
        /// <param name="tabContent">The Control content for the tab</param>
        public void RegisterTab(string tabId, string tabName, Control tabContent)
        {
            if (string.IsNullOrEmpty(tabId) || tabContent == null)
            {
                GD.PrintErr("[PanelsManager] Invalid tab registration parameters");
                return;
            }

            if (_registeredTabs.ContainsKey(tabId))
            {
                GD.PushWarning(
                    $"[PanelsManager] Tab with ID '{tabId}' already registered. Skipping."
                );
                return;
            }

            if (_tabContainer == null)
            {
                GD.PrintErr("[PanelsManager] TabContainer not initialized");
                return;
            }

            // Create panel container for the tab
            var panel = new PanelContainer
            {
                Name = tabName,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            };
            panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            panel.AddChild(tabContent);

            // Add to tab container
            _tabContainer.AddChild(panel);
            var tabIndex = _tabContainer.GetTabCount() - 1;

            // Register tab info
            var tabInfo = new TabInfo
            {
                TabId = tabId,
                TabName = tabName,
                TabContent = tabContent,
                TabIndex = tabIndex,
            };
            _registeredTabs[tabId] = tabInfo;

            GD.Print($"[PanelsManager] Registered tab: {tabName} (ID: {tabId})");
        }

        /// <summary>
        /// Unregister and remove a previously registered tab.
        /// </summary>
        /// <param name="tabId">The ID of the tab to remove</param>
        public void UnregisterTab(string tabId)
        {
            if (!_registeredTabs.ContainsKey(tabId))
            {
                GD.PushWarning($"[PanelsManager] Tab '{tabId}' not found");
                return;
            }

            var tabInfo = _registeredTabs[tabId];
            if (_tabContainer != null && GodotObject.IsInstanceValid(_tabContainer))
            {
                var panel = _tabContainer.GetChild(tabInfo.TabIndex);
                if (GodotObject.IsInstanceValid(panel))
                {
                    _tabContainer.RemoveChild(panel);
                    panel.QueueFree();
                }
            }

            _registeredTabs.Remove(tabId);
            GD.Print($"[PanelsManager] Unregistered tab: {tabId}");
        }

        /// <summary>
        /// Get a registered tab by its ID.
        /// </summary>
        /// <param name="tabId">The ID of the tab</param>
        /// <returns>The TabInfo or null if not found</returns>
        public TabInfo GetRegisteredTab(string tabId)
        {
            _registeredTabs.TryGetValue(tabId, out var tab);
            return tab;
        }

        /// <summary>
        /// Get all registered tabs.
        /// </summary>
        /// <returns>Dictionary of all registered tabs</returns>
        public IReadOnlyDictionary<string, TabInfo> GetAllRegisteredTabs() => _registeredTabs;

        /// <summary>
        /// Switch to a specific tab by ID.
        /// </summary>
        /// <param name="tabId">The ID of the tab to switch to</param>
        public void SwitchToTab(string tabId)
        {
            if (!_registeredTabs.ContainsKey(tabId))
            {
                GD.PushWarning($"[PanelsManager] Cannot switch to tab '{tabId}' - not found");
                return;
            }

            if (_tabContainer != null && GodotObject.IsInstanceValid(_tabContainer))
            {
                _tabContainer.CurrentTab = _registeredTabs[tabId].TabIndex;
            }
        }

        #endregion
    }
}
#endif
