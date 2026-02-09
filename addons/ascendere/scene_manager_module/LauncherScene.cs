using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

#nullable enable

namespace Ascendere.SceneManagement
{
    /// <summary>
    /// Launcher scene that validates game requirements before proceeding.
    /// Performs checks such as system requirements, saved data availability, etc.
    /// </summary>
    public partial class LauncherScene : GameScene
    {
        [Signal]
        public delegate void RequirementCheckedEventHandler(string requirementName, bool passed);

        [Signal]
        public delegate void AllRequirementsMetEventHandler();

        [Signal]
        public delegate void RequirementsFailedEventHandler(string[] failedRequirements);

        /// <summary>
        /// List of requirements that must pass before proceeding.
        /// </summary>
        private readonly List<ILaunchRequirement> _requirements = new();

        /// <summary>
        /// Type of the next scene to load after successful validation.
        /// </summary>
        [Export]
        public string NextSceneTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Whether to auto-proceed after all requirements pass.
        /// </summary>
        [Export]
        public bool AutoProceed { get; set; } = true;

        /// <summary>
        /// Delay in seconds before auto-proceeding (if enabled).
        /// </summary>
        [Export]
        public float ProceedDelay { get; set; } = 0.5f;

        private bool _requirementsMet;

        public override void _Ready()
        {
            base._Ready();
            RegisterRequirements();
            CallDeferred(nameof(CheckRequirements));
        }

        /// <summary>
        /// Override to register custom launch requirements.
        /// </summary>
        protected virtual void RegisterRequirements()
        {
            // Default requirements - override to add custom checks
            AddRequirement(new SystemRequirement());
            AddRequirement(new GraphicsDriverRequirement());
        }

        /// <summary>
        /// Adds a requirement to the validation list.
        /// </summary>
        protected void AddRequirement(ILaunchRequirement requirement)
        {
            _requirements.Add(requirement);
        }

        /// <summary>
        /// Performs all requirement checks asynchronously.
        /// </summary>
        private async void CheckRequirements()
        {
            GD.Print($"[{GetType().Name}] Checking {_requirements.Count} launch requirements...");

            var failedRequirements = new List<string>();

            foreach (var requirement in _requirements)
            {
                var result = await requirement.CheckAsync();
                EmitSignal(SignalName.RequirementChecked, requirement.Name, result.Passed);

                if (!result.Passed)
                {
                    failedRequirements.Add(requirement.Name);
                    GD.PrintErr(
                        $"[{GetType().Name}] Requirement failed: {requirement.Name} - {result.Message}"
                    );
                }
                else
                {
                    GD.Print($"[{GetType().Name}] Requirement passed: {requirement.Name}");
                }
            }

            if (failedRequirements.Count > 0)
            {
                _requirementsMet = false;
                EmitSignal(SignalName.RequirementsFailed, failedRequirements.ToArray());
                OnRequirementsFailed(failedRequirements);
            }
            else
            {
                _requirementsMet = true;
                EmitSignal(SignalName.AllRequirementsMet);
                OnAllRequirementsMet();

                if (AutoProceed)
                {
                    await Godot
                        .Engine.GetMainLoop()
                        .ToSignal(
                            GetTree().CreateTimer(ProceedDelay),
                            SceneTreeTimer.SignalName.Timeout
                        );
                    ProceedToNext();
                }
            }
        }

        /// <summary>
        /// Called when all requirements are met.
        /// Override to add custom behavior before proceeding.
        /// </summary>
        protected virtual void OnAllRequirementsMet()
        {
            GD.Print($"[{GetType().Name}] All requirements met - ready to proceed!");
        }

        /// <summary>
        /// Called when one or more requirements fail.
        /// Override to handle failures (e.g., show error dialog).
        /// </summary>
        protected virtual void OnRequirementsFailed(List<string> failed)
        {
            GD.PrintErr($"[{GetType().Name}] Requirements failed: {string.Join(", ", failed)}");
        }

        protected override Type? GetNextSceneType()
        {
            if (string.IsNullOrEmpty(NextSceneTypeName))
                return null;

            var type = Type.GetType(NextSceneTypeName);
            if (type == null || !type.IsSubclassOf(typeof(GameScene)))
            {
                GD.PrintErr($"[{GetType().Name}] Invalid NextSceneTypeName: {NextSceneTypeName}");
                return null;
            }

            return type;
        }

        protected override bool CanProceedToNext()
        {
            return _requirementsMet;
        }
    }

    // ========== REQUIREMENT INTERFACES AND DEFAULT IMPLEMENTATIONS ==========

    /// <summary>
    /// Interface for launch requirements.
    /// </summary>
    public interface ILaunchRequirement
    {
        string Name { get; }
        System.Threading.Tasks.Task<RequirementResult> CheckAsync();
    }

    /// <summary>
    /// Result of a requirement check.
    /// </summary>
    public struct RequirementResult
    {
        public bool Passed { get; set; }
        public string Message { get; set; }

        public static RequirementResult Success(string message = "OK") =>
            new() { Passed = true, Message = message };

        public static RequirementResult Failure(string message) =>
            new() { Passed = false, Message = message };
    }

    /// <summary>
    /// Default requirement: checks basic system compatibility.
    /// </summary>
    public class SystemRequirement : ILaunchRequirement
    {
        public string Name => "System Compatibility";

        public System.Threading.Tasks.Task<RequirementResult> CheckAsync()
        {
            // Check OS, memory, etc.
            var osName = OS.GetName();
            var memory = OS.GetStaticMemoryUsage();

            if (string.IsNullOrEmpty(osName))
                return System.Threading.Tasks.Task.FromResult(
                    RequirementResult.Failure("Unable to detect OS")
                );

            return System.Threading.Tasks.Task.FromResult(
                RequirementResult.Success($"System OK ({osName})")
            );
        }
    }

    /// <summary>
    /// Default requirement: checks graphics driver availability.
    /// </summary>
    public class GraphicsDriverRequirement : ILaunchRequirement
    {
        public string Name => "Graphics Driver";

        public System.Threading.Tasks.Task<RequirementResult> CheckAsync()
        {
            var adapterName = RenderingServer.GetVideoAdapterName();
            var apiVersion = RenderingServer.GetVideoAdapterApiVersion();

            if (string.IsNullOrEmpty(adapterName))
                return System.Threading.Tasks.Task.FromResult(
                    RequirementResult.Failure("No graphics adapter detected")
                );

            return System.Threading.Tasks.Task.FromResult(
                RequirementResult.Success($"Driver OK ({adapterName})")
            );
        }
    }
}
