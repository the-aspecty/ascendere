using System;

namespace Ascendere
{
    /// <summary>
    /// Attribute to mark properties for automatic dependency injection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class AutoWireAttribute : Attribute
    {
        /// <summary>
        /// The name to use for dependency resolution. If null, uses the property/field name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether this dependency is optional.
        /// </summary>
        public bool Optional { get; set; } = false;

        /// <summary>
        /// Whether to create the dependency if it doesn't exist.
        /// </summary>
        public bool CreateIfMissing { get; set; } = true;

        /// <summary>
        /// The scope for dependency resolution.
        /// </summary>
        public DependencyScope Scope { get; set; } = DependencyScope.Entity;

        public AutoWireAttribute() { }
    }
}
