#if TOOLS
using Godot;

namespace Ascendere.Editor.CustomTools
{
    /// <summary>
    /// Example tool menu items for Ascendere.
    /// Add your own tools by creating static methods with [ToolMenuItem] attribute.
    /// </summary>
    [ToolMenuProvider]
    public static class AscendereTools
    {
        [ToolMenuItem("Create Scene Template", Category = "Ascendere", Priority = 100)]
        public static void CreateSceneTemplate()
        {
            GD.Print("[AscendereTools] Creating scene template...");
            // TODO: Implement scene template creation
        }

        [ToolMenuItem("Generate Component", Category = "Ascendere", Priority = 90)]
        public static void GenerateComponent()
        {
            GD.Print("[AscendereTools] Generating component...");
            // TODO: Implement component generation
        }

        [ToolMenuItem("Create Module Package", Category = "Ascendere", Priority = 80)]
        public static void CreateModulePackage()
        {
            GD.Print("[AscendereTools] Creating module package...");
            // TODO: Implement module package creation
        }

        [ToolMenuItem("Validate Project Structure", Category = "Ascendere", Priority = 70)]
        public static void ValidateProjectStructure()
        {
            GD.Print("[AscendereTools] Validating project structure...");
            // TODO: Implement project validation
        }

        [ToolMenuItem("Export Module Documentation", Category = "Ascendere", Priority = 60)]
        public static void ExportModuleDocs()
        {
            GD.Print("[AscendereTools] Exporting module documentation...");
            // TODO: Implement documentation export
        }

        [ToolMenuItem("Clean Build Cache", Category = "Ascendere")]
        public static void CleanBuildCache()
        {
            GD.Print("[AscendereTools] Cleaning build cache...");
            // TODO: Implement cache cleaning
        }

        // Example of a top-level tool (no category/submenu)
        [ToolMenuItem("Quick Ascendere Setup")]
        public static void QuickSetup()
        {
            GD.Print("[AscendereTools] Running quick setup...");
            // TODO: Implement quick setup wizard
        }
    }
}
#endif
