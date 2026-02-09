using Godot;

namespace Ascendere
{
    /// <summary>
    /// Represents the lifecycle state of a Meta Entity.
    /// </summary>
    public enum MetaEntityState
    {
        /// <summary>
        /// Entity has been created but not yet initialized.
        /// </summary>
        Created,

        /// <summary>
        /// Entity is being initialized.
        /// </summary>
        Initializing,

        /// <summary>
        /// Entity is fully initialized and ready to use.
        /// </summary>
        Ready,

        /// <summary>
        /// Entity is being configured or reconfigured.
        /// </summary>
        Configuring,

        /// <summary>
        /// Entity is active and processing.
        /// </summary>
        Active,

        /// <summary>
        /// Entity is paused and not processing.
        /// </summary>
        Paused,

        /// <summary>
        /// Entity is being destroyed.
        /// </summary>
        Destroying,

        /// <summary>
        /// Entity has been destroyed and should not be used.
        /// </summary>
        Destroyed,
    }

    /// <summary>
    /// Represents the execution priority levels for systems.
    /// </summary>
    public enum SystemPriority
    {
        /// <summary>
        /// Lowest priority - executes last.
        /// </summary>
        Lowest = 0,

        /// <summary>
        /// Low priority.
        /// </summary>
        Low = 100,

        /// <summary>
        /// Normal priority - default value.
        /// </summary>
        Normal = 500,

        /// <summary>
        /// High priority.
        /// </summary>
        High = 800,

        /// <summary>
        /// Highest priority - executes first.
        /// </summary>
        Highest = 1000,
    }

    /// <summary>
    /// Represents different types of components in the Meta Framework.
    /// </summary>
    public enum ComponentType
    {
        /// <summary>
        /// Data-only component with no behavior.
        /// </summary>
        Data,

        /// <summary>
        /// Behavior component that processes logic.
        /// </summary>
        Behavior,

        /// <summary>
        /// Rendering component for visual representation.
        /// </summary>
        Rendering,

        /// <summary>
        /// Physics component for physical simulation.
        /// </summary>
        Physics,

        /// <summary>
        /// Audio component for sound processing.
        /// </summary>
        Audio,

        /// <summary>
        /// Input component for handling user input.
        /// </summary>
        Input,

        /// <summary>
        /// Network component for multiplayer functionality.
        /// </summary>
        Network,

        /// <summary>
        /// Custom component type.
        /// </summary>
        Custom,
    }
}
