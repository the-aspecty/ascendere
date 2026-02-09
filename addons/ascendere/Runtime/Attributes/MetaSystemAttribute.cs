using System;

namespace Ascendere
{
    /// <summary>
    /// Attribute to mark a class as a Meta System with automatic registration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MetaSystemAttribute : Attribute
    {
        /// <summary>
        /// The name of the system for identification.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The execution priority of this system.
        /// </summary>
        public int Priority { get; set; } = (int)SystemPriority.Normal;

        /// <summary>
        /// Whether this system processes entities every frame.
        /// </summary>
        public bool ProcessEveryFrame { get; set; } = true;

        /// <summary>
        /// Whether this system needs physics process updates.
        /// </summary>
        public bool NeedsPhysicsProcess { get; set; } = false;

        /// <summary>
        /// Whether this system should be automatically registered.
        /// </summary>
        public bool AutoRegister { get; set; } = true;

        /// <summary>
        /// The category this system belongs to.
        /// </summary>
        public string Category { get; set; } = "Default";

        /// <summary>
        /// A brief description of what this system does.
        /// </summary>
        public string Description { get; set; } = "";

        public MetaSystemAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
