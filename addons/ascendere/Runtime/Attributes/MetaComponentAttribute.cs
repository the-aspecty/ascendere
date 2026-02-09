using System;

namespace Ascendere
{
    /// <summary>
    /// Attribute to mark a class as a Meta Component.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class MetaComponentAttribute : Attribute
    {
        /// <summary>
        /// The name of the component for identification.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The type of component this represents.
        /// </summary>
        public ComponentType ComponentType { get; set; } = ComponentType.Data;

        /// <summary>
        /// Whether this component can be added multiple times to the same entity.
        /// </summary>
        public bool AllowMultiple { get; set; } = false;

        /// <summary>
        /// The category this component belongs to.
        /// </summary>
        public string Category { get; set; } = "Default";

        /// <summary>
        /// A brief description of what this component does.
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Whether this component should be automatically registered.
        /// </summary>
        public bool AutoRegister { get; set; } = true;

        /// <summary>
        /// Dependencies that this component requires.
        /// </summary>
        public Type[] Dependencies { get; set; } = Array.Empty<Type>();

        public MetaComponentAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
