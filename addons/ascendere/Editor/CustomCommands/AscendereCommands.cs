#if TOOLS
using Godot;

namespace Ascendere.Editor.CustomCommands
{
    /// <summary>
    /// Example editor commands for Ascendere.
    /// Add your own commands by creating static methods with [EditorCommand] attribute.
    /// </summary>
    [EditorCommandProvider]
    public static class AscendereCommands
    {
        [EditorCommand(
            "Create New Module",
            Description = "Creates a new module from template",
            Category = "Module",
            Priority = 100
        )]
        public static void CreateNewModule()
        {
            GD.Print("[AscendereCommands] Creating new module...");
            // TODO: Implement module creation
        }

        [EditorCommand(
            "Refresh Module List",
            Description = "Refreshes the list of available modules",
            Category = "Module",
            Priority = 90
        )]
        public static void RefreshModules()
        {
            GD.Print("[AscendereCommands] Refreshing modules...");
            // TODO: Implement module refresh
        }

        [EditorCommand(
            "Open Ascendere Settings",
            Description = "Opens the Ascendere framework settings",
            Category = "Settings",
            Priority = 50
        )]
        public static void OpenSettings()
        {
            GD.Print("[AscendereCommands] Opening settings...");
            // TODO: Implement settings dialog
        }

        [EditorCommand(
            "Generate Registry",
            Description = "Generates a new registry class from template",
            Category = "Tools",
            Priority = 80
        )]
        public static void GenerateRegistry()
        {
            GD.Print("[AscendereCommands] Generating registry...");
            // TODO: Implement registry generation
        }

        [EditorCommand(
            "Clear Cache",
            Description = "Clears the Ascendere cache",
            Category = "Tools"
        )]
        public static void ClearCache()
        {
            GD.Print("[AscendereCommands] Clearing cache...");
            // TODO: Implement cache clearing
        }
    }
}
#endif
