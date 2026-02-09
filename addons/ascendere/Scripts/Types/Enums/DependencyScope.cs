using System;

namespace Ascendere
{
    /// <summary>
    /// Represents the scope for dependency resolution.
    /// </summary>
    public enum DependencyScope
    {
        /// <summary>
        /// Scoped to the current entity.
        /// </summary>
        Entity,

        /// <summary>
        /// Scoped to the current scene.
        /// </summary>
        Scene,

        /// <summary>
        /// Globally scoped across the entire application.
        /// </summary>
        Global,

        /// <summary>
        /// Scoped to the current system.
        /// </summary>
        System,
    }
}
