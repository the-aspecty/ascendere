using System;
using System.Threading.Tasks;
using Ascendere.SceneManagement;
using Godot;

#nullable enable

namespace Examples.SceneManagement
{
    /// <summary>
    /// Example launcher implementation with custom requirements.
    /// </summary>
    public partial class ExampleLauncher : LauncherScene
    {
        protected override void RegisterRequirements()
        {
            // Register default requirements
            base.RegisterRequirements();

            // Add custom requirements
            AddRequirement(new SaveDataRequirement());
            AddRequirement(new ConfigRequirement());
        }

        protected override void OnAllRequirementsMet()
        {
            base.OnAllRequirementsMet();
            GD.Print("[ExampleLauncher] All checks passed - game will start!");
        }

        protected override void OnRequirementsFailed(System.Collections.Generic.List<string> failed)
        {
            base.OnRequirementsFailed(failed);

            // Show error dialog or create default config
            GD.PrintErr($"[ExampleLauncher] Cannot start - missing: {string.Join(", ", failed)}");

            // Optionally quit
            // GetTree().Quit();
        }

        // Define flow: after launcher, go to the Menu scene in the examples
        protected override Type? GetNextSceneType()
        {
            return typeof(Menu);
        }

        // Resolve example scene paths under Examples/SceneManagement/scenes
        protected override string? GetScenePathForType(Type sceneType)
        {
            if (sceneType.Namespace?.StartsWith("Examples.SceneManagement") == true)
                return $"res://Examples/SceneManagement/scenes/{sceneType.Name.ToLowerInvariant()}.tscn";

            return base.GetScenePathForType(sceneType);
        }
    }

    // ========== CUSTOM REQUIREMENTS ==========

    /// <summary>
    /// Checks if save data directory exists.
    /// </summary>
    public class SaveDataRequirement : ILaunchRequirement
    {
        public string Name => "Save Data";

        public Task<RequirementResult> CheckAsync()
        {
            return Task.FromResult(RequirementResult.Success("Save directory exists"));
        }
    }

    /// <summary>
    /// Checks if configuration file exists.
    /// </summary>
    public class ConfigRequirement : ILaunchRequirement
    {
        public string Name => "Configuration";

        public Task<RequirementResult> CheckAsync()
        {
            return Task.FromResult(RequirementResult.Success("Config file exists"));
        }
    }
}
