using System;
using Godot;

namespace Ascendere
{
    /// <summary>
    /// Attribute to mark a class as a game entity with automatic registration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class GameEntityAttribute : Attribute
    {
        /// <summary>
        /// The name of the entity for registration and identification.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The category this entity belongs to for organization.
        /// </summary>
        public string Category { get; set; } = "Default";

        /// <summary>
        /// A brief description of what this entity represents.
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Whether this entity should be automatically registered with the framework.
        /// </summary>
        public bool AutoRegister { get; set; } = true;

        /// <summary>
        /// The scene path to use as a template for this entity.
        /// </summary>
        public string SceneTemplate { get; set; } = "";

        /// <summary>
        /// Tags for categorizing and filtering entities.
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        public GameEntityAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
