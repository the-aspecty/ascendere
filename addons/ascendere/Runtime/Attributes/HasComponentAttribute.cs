using System;

namespace Ascendere
{
    /// <summary>
    /// Attribute to specify components that an entity should have.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class HasComponentAttribute : Attribute
    {
        /// <summary>
        /// The type of component to add to the entity.
        /// </summary>
        public Type ComponentType { get; }

        /// <summary>
        /// Whether this component is required for the entity to function.
        /// </summary>
        public bool Required { get; set; } = true;

        /// <summary>
        /// The order in which to initialize this component.
        /// </summary>
        public int InitializationOrder { get; set; } = 0;

        /// <summary>
        /// Configuration data for the component.
        /// </summary>
        public string ConfigurationData { get; set; } = "";

        public HasComponentAttribute(Type componentType)
        {
            ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
        }
    }

    /// <summary>
    /// Generic version of HasComponentAttribute for type safety.
    /// </summary>
    /// <typeparam name="T">The component type</typeparam>
    public class HasComponentAttribute<T> : HasComponentAttribute
        where T : class
    {
        public HasComponentAttribute()
            : base(typeof(T)) { }
    }
}
