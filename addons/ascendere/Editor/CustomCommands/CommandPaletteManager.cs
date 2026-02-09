#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace Ascendere.Editor.CustomCommands
{
    /// <summary>
    /// Manages command registration for the native Godot editor command palette.
    /// Provides attribute-based command discovery and registration.
    /// </summary>
    public class CommandPaletteManager
    {
        private EditorCommandPalette _editorPalette;
        private readonly Dictionary<string, CommandInfo> _commands =
            new Dictionary<string, CommandInfo>();
        private readonly HashSet<string> _registeredKeys = new HashSet<string>();

        #region Lifecycle

        /// <summary>
        /// Initializes the command palette manager and discovers commands
        /// </summary>
        public void Initialize(EditorCommandPalette editorPalette)
        {
            if (editorPalette == null)
            {
                GD.PrintErr("[CommandPaletteManager] EditorCommandPalette is null");
                return;
            }

            _editorPalette = editorPalette;
            DiscoverCommands();
            RegisterAllCommands();
            GD.Print($"[CommandPaletteManager] Initialized with {_commands.Count} commands");
        }

        /// <summary>
        /// Cleans up all registered commands
        /// </summary>
        public void Cleanup()
        {
            UnregisterAllCommands();
            _commands.Clear();
            _editorPalette = null;
            GD.Print("[CommandPaletteManager] Cleaned up");
        }

        #endregion

        #region Command Registration

        /// <summary>
        /// Discovers and registers all commands marked with [EditorCommand] attribute
        /// </summary>
        public void DiscoverCommands()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly
                        .GetTypes()
                        .Where(t => t.GetCustomAttribute<EditorCommandProviderAttribute>() != null);

                    foreach (var type in types)
                    {
                        RegisterCommandsFromType(type);
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    GD.PrintErr(
                        $"[CommandPaletteManager] Failed to load types from assembly {assembly.FullName}: {ex.Message}"
                    );
                }
            }

            GD.Print($"[CommandPaletteManager] Discovered {_commands.Count} commands");
        }

        /// <summary>
        /// Registers commands from a specific type
        /// </summary>
        private void RegisterCommandsFromType(Type type)
        {
            var methods = type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            );

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<EditorCommandAttribute>();
                if (attribute == null)
                    continue;

                var commandInfo = new CommandInfo
                {
                    Id = attribute.Id ?? $"{type.Name}.{method.Name}",
                    Name = attribute.Name ?? method.Name,
                    Description = attribute.Description ?? "",
                    Category = attribute.Category ?? "General",
                    Shortcut = attribute.Shortcut ?? "",
                    Method = method,
                    Priority = attribute.Priority,
                };

                RegisterCommand(commandInfo);
            }
        }

        /// <summary>
        /// Registers a command manually
        /// </summary>
        public bool RegisterCommand(CommandInfo command)
        {
            if (command == null || string.IsNullOrEmpty(command.Id))
            {
                GD.PrintErr("[CommandPaletteManager] Cannot register null or empty command");
                return false;
            }

            if (_commands.ContainsKey(command.Id))
            {
                GD.PrintErr($"[CommandPaletteManager] Command '{command.Id}' already registered");
                return false;
            }

            _commands[command.Id] = command;
            GD.Print($"[CommandPaletteManager] Registered command: {command.Name}");
            return true;
        }

        /// <summary>
        /// Unregisters a command by ID
        /// </summary>
        public bool UnregisterCommand(string id)
        {
            if (_commands.Remove(id))
            {
                GD.Print($"[CommandPaletteManager] Unregistered command: {id}");
                return true;
            }
            return false;
        }

        #endregion

        #region Command Registration with Native Palette

        /// <summary>
        /// Registers all discovered commands with the native editor palette
        /// </summary>
        private void RegisterAllCommands()
        {
            if (_editorPalette == null)
                return;

            foreach (var command in _commands.Values)
            {
                RegisterCommandWithPalette(command);
            }
        }

        /// <summary>
        /// Registers a single command with the native editor palette
        /// </summary>
        private void RegisterCommandWithPalette(CommandInfo command)
        {
            if (_editorPalette == null || command == null)
                return;

            try
            {
                // Create the command key (use category as prefix for organization)
                var commandKey = string.IsNullOrEmpty(command.Category)
                    ? $"ascendere/{command.Id}"
                    : $"ascendere/{command.Category.ToLower()}/{command.Id}";

                // Create callable for the command
                var callable = Callable.From(() => ExecuteCommand(command));

                // Add to editor palette
                _editorPalette.AddCommand(command.Name, commandKey, callable);
                _registeredKeys.Add(commandKey);

                GD.Print($"[CommandPaletteManager] Registered: {command.Name} ({commandKey})");
            }
            catch (Exception ex)
            {
                GD.PrintErr(
                    $"[CommandPaletteManager] Failed to register command {command.Name}: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Unregisters all commands from the native editor palette
        /// </summary>
        private void UnregisterAllCommands()
        {
            if (_editorPalette == null)
                return;

            foreach (var key in _registeredKeys)
            {
                try
                {
                    _editorPalette.RemoveCommand(key);
                }
                catch (Exception ex)
                {
                    GD.PrintErr(
                        $"[CommandPaletteManager] Failed to unregister command {key}: {ex.Message}"
                    );
                }
            }

            _registeredKeys.Clear();
            GD.Print("[CommandPaletteManager] Unregistered all commands");
        }

        /// <summary>
        /// Executes a command
        /// </summary>
        private void ExecuteCommand(CommandInfo command)
        {
            if (command == null || command.Method == null)
                return;

            try
            {
                GD.Print($"[CommandPaletteManager] Executing: {command.Name}");

                if (command.Method.GetParameters().Length == 0)
                {
                    command.Method.Invoke(null, null);
                }
                else
                {
                    GD.PrintErr(
                        $"[CommandPaletteManager] Command {command.Name} has parameters - not supported"
                    );
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr(
                    $"[CommandPaletteManager] Error executing {command.Name}: {ex.Message}"
                );
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets all registered commands
        /// </summary>
        public IReadOnlyDictionary<string, CommandInfo> GetCommands() => _commands;

        /// <summary>
        /// Gets a command by ID
        /// </summary>
        public CommandInfo GetCommand(string id)
        {
            _commands.TryGetValue(id, out var command);
            return command;
        }

        #endregion
    }
}
#endif
