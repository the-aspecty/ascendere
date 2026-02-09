#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace Ascendere.Editor.CustomTools
{
    /// <summary>
    /// Manages tool menu items for the editor.
    /// Provides attribute-based tool discovery and registration with the Tools menu.
    /// </summary>
    public class ToolMenuManager
    {
        private EditorPlugin _editorPlugin;
        private readonly Dictionary<string, ToolMenuItemInfo> _toolItems =
            new Dictionary<string, ToolMenuItemInfo>();
        private readonly Dictionary<string, Callable> _registeredCallables =
            new Dictionary<string, Callable>();
        private readonly Dictionary<string, PopupMenu> _submenus =
            new Dictionary<string, PopupMenu>();

        #region Lifecycle

        /// <summary>
        /// Initializes the tool menu manager and discovers tools
        /// </summary>
        public void Initialize(EditorPlugin editorPlugin)
        {
            if (editorPlugin == null)
            {
                GD.PrintErr("[ToolMenuManager] EditorPlugin is null");
                return;
            }

            _editorPlugin = editorPlugin;
            DiscoverTools();
            RegisterAllTools();
            GD.Print($"[ToolMenuManager] Initialized with {_toolItems.Count} tools");
        }

        /// <summary>
        /// Cleans up all registered tool menu items
        /// </summary>
        public void Cleanup()
        {
            UnregisterAllTools();

            // Free submenu PopupMenus (check validity first)
            foreach (var submenu in _submenus.Values)
            {
                if (GodotObject.IsInstanceValid(submenu))
                {
                    submenu.QueueFree();
                }
            }

            _toolItems.Clear();
            _registeredCallables.Clear();
            _submenus.Clear();
            _editorPlugin = null;
            GD.Print("[ToolMenuManager] Cleaned up");
        }

        #endregion

        #region Command Discovery

        /// <summary>
        /// Discovers and registers all tools marked with [ToolMenuItem] attribute
        /// </summary>
        private void DiscoverTools()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly
                        .GetTypes()
                        .Where(t => t.GetCustomAttribute<ToolMenuProviderAttribute>() != null);

                    foreach (var type in types)
                    {
                        RegisterToolsFromType(type);
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    GD.PrintErr(
                        $"[ToolMenuManager] Failed to load types from assembly {assembly.FullName}: {ex.Message}"
                    );
                }
            }

            GD.Print($"[ToolMenuManager] Discovered {_toolItems.Count} tools");
        }

        /// <summary>
        /// Registers tools from a specific type
        /// </summary>
        private void RegisterToolsFromType(Type type)
        {
            var methods = type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            );

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<ToolMenuItemAttribute>();
                if (attribute == null)
                    continue;

                var toolInfo = new ToolMenuItemInfo
                {
                    Name = attribute.Name ?? method.Name,
                    Category = attribute.Category ?? "",
                    Icon = attribute.Icon ?? "",
                    Tooltip = attribute.Tooltip ?? "",
                    Priority = attribute.Priority,
                    Method = method,
                    IsSubmenu = !string.IsNullOrEmpty(attribute.Category),
                };

                var key = string.IsNullOrEmpty(toolInfo.Category)
                    ? toolInfo.Name
                    : $"{toolInfo.Category}/{toolInfo.Name}";

                if (!_toolItems.ContainsKey(key))
                {
                    _toolItems[key] = toolInfo;
                }
            }
        }

        #endregion

        #region Tool Registration

        /// <summary>
        /// Registers all discovered tools with the editor
        /// </summary>
        private void RegisterAllTools()
        {
            if (_editorPlugin == null)
                return;

            // Sort by priority (higher first)
            var sortedTools = _toolItems.Values.OrderByDescending(t => t.Priority).ToList();

            foreach (var tool in sortedTools)
            {
                RegisterToolWithEditor(tool);
            }
        }

        /// <summary>
        /// Registers a single tool with the editor
        /// </summary>
        private void RegisterToolWithEditor(ToolMenuItemInfo tool)
        {
            if (_editorPlugin == null || tool == null)
                return;

            try
            {
                // Create callable for the tool
                var callable = Callable.From(() => ExecuteTool(tool));

                // Store the callable for cleanup
                var key = string.IsNullOrEmpty(tool.Category)
                    ? tool.Name
                    : $"{tool.Category}/{tool.Name}";
                _registeredCallables[key] = callable;

                // Add to editor menu
                if (tool.IsSubmenu && !string.IsNullOrEmpty(tool.Category))
                {
                    // Get or create submenu
                    if (!_submenus.TryGetValue(tool.Category, out var submenu))
                    {
                        submenu = new PopupMenu();
                        submenu.Name = tool.Category;
                        _submenus[tool.Category] = submenu;
                        _editorPlugin.AddToolSubmenuItem(tool.Category, submenu);
                        GD.Print($"[ToolMenuManager] Created submenu: {tool.Category}");
                    }

                    // Add item to submenu
                    var itemIndex = submenu.ItemCount;
                    submenu.AddItem(tool.Name, itemIndex);
                    submenu.IdPressed += (id) =>
                    {
                        if (id == itemIndex)
                            ExecuteTool(tool);
                    };

                    GD.Print(
                        $"[ToolMenuManager] Registered submenu item: {tool.Category} > {tool.Name}"
                    );
                }
                else
                {
                    _editorPlugin.AddToolMenuItem(tool.Name, callable);
                    GD.Print($"[ToolMenuManager] Registered tool: {tool.Name}");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ToolMenuManager] Failed to register tool {tool.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Unregisters all tools from the editor
        /// </summary>
        private void UnregisterAllTools()
        {
            if (_editorPlugin == null)
                return;

            // Remove submenus
            foreach (var category in _submenus.Keys)
            {
                try
                {
                    _editorPlugin.RemoveToolMenuItem(category);
                }
                catch (Exception ex)
                {
                    GD.PrintErr(
                        $"[ToolMenuManager] Failed to unregister submenu {category}: {ex.Message}"
                    );
                }
            }

            // Remove top-level tools
            foreach (var kvp in _registeredCallables)
            {
                try
                {
                    var tool = _toolItems[kvp.Key];

                    if (!tool.IsSubmenu)
                    {
                        _editorPlugin.RemoveToolMenuItem(tool.Name);
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr(
                        $"[ToolMenuManager] Failed to unregister tool {kvp.Key}: {ex.Message}"
                    );
                }
            }

            GD.Print("[ToolMenuManager] Unregistered all tools");
        }

        /// <summary>
        /// Executes a tool
        /// </summary>
        private void ExecuteTool(ToolMenuItemInfo tool)
        {
            if (tool == null || tool.Method == null)
                return;

            try
            {
                GD.Print($"[ToolMenuManager] Executing: {tool.Name}");

                if (tool.Method.GetParameters().Length == 0)
                {
                    tool.Method.Invoke(null, null);
                }
                else
                {
                    GD.PrintErr(
                        $"[ToolMenuManager] Tool {tool.Name} has parameters - not supported"
                    );
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ToolMenuManager] Error executing {tool.Name}: {ex.Message}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets all registered tools
        /// </summary>
        public IReadOnlyDictionary<string, ToolMenuItemInfo> GetTools() => _toolItems;

        /// <summary>
        /// Gets a tool by key
        /// </summary>
        public ToolMenuItemInfo GetTool(string key)
        {
            _toolItems.TryGetValue(key, out var tool);
            return tool;
        }

        #endregion
    }
}
#endif
