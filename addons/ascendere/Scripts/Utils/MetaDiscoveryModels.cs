#if TOOLS
using System;
using System.Collections.Generic;

namespace Ascendere.Utils
{
    /// <summary>
    /// Represents the result of Meta Framework attribute discovery.
    /// </summary>
    public class MetaDiscoveryResult
    {
        /// <summary>
        /// List of discovered components.
        /// </summary>
        public List<MetaComponentInfo> Components { get; set; } = new();

        /// <summary>
        /// List of discovered entities.
        /// </summary>
        public List<MetaEntityInfo> Entities { get; set; } = new();

        /// <summary>
        /// List of discovered systems.
        /// </summary>
        public List<MetaSystemInfo> Systems { get; set; } = new();

        /// <summary>
        /// Gets the total count of discovered items.
        /// </summary>
        public int TotalCount => Components.Count + Entities.Count + Systems.Count;
    }

    /// <summary>
    /// Information about a discovered Meta Component.
    /// </summary>
    public class MetaComponentInfo
    {
        /// <summary>
        /// The type of the component.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// The MetaComponent attribute instance.
        /// </summary>
        public MetaComponentAttribute Attribute { get; set; }

        /// <summary>
        /// The name of the component.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The category of the component.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// The type of component.
        /// </summary>
        public ComponentType ComponentType { get; set; }

        /// <summary>
        /// Whether this component allows multiple instances.
        /// </summary>
        public bool AllowMultiple { get; set; }

        /// <summary>
        /// The full type name for display.
        /// </summary>
        public string TypeName => Type?.FullName ?? "Unknown";

        /// <summary>
        /// The namespace of the component.
        /// </summary>
        public string Namespace => Type?.Namespace ?? "Unknown";
    }

    /// <summary>
    /// Information about a discovered Meta Entity.
    /// </summary>
    public class MetaEntityInfo
    {
        /// <summary>
        /// The type of the entity.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// The GameEntity attribute instance.
        /// </summary>
        public GameEntityAttribute Attribute { get; set; }

        /// <summary>
        /// The name of the entity.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The category of the entity.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// The description of the entity.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether this entity is auto-registered.
        /// </summary>
        public bool AutoRegister { get; set; }

        /// <summary>
        /// The full type name for display.
        /// </summary>
        public string TypeName => Type?.FullName ?? "Unknown";

        /// <summary>
        /// The namespace of the entity.
        /// </summary>
        public string Namespace => Type?.Namespace ?? "Unknown";
    }

    /// <summary>
    /// Information about a discovered Meta System.
    /// </summary>
    public class MetaSystemInfo
    {
        /// <summary>
        /// The type of the system.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// The MetaSystem attribute instance.
        /// </summary>
        public MetaSystemAttribute Attribute { get; set; }

        /// <summary>
        /// The name of the system.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The execution priority of the system.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Whether this system processes every frame.
        /// </summary>
        public bool ProcessEveryFrame { get; set; }

        /// <summary>
        /// Whether this system needs physics process.
        /// </summary>
        public bool NeedsPhysicsProcess { get; set; }

        /// <summary>
        /// The full type name for display.
        /// </summary>
        public string TypeName => Type?.FullName ?? "Unknown";

        /// <summary>
        /// The namespace of the system.
        /// </summary>
        public string Namespace => Type?.Namespace ?? "Unknown";

        /// <summary>
        /// Gets the priority description.
        /// </summary>
        public string PriorityDescription
        {
            get
            {
                return Priority switch
                {
                    <= 100 => "Low",
                    <= 500 => "Normal",
                    <= 800 => "High",
                    _ => "Highest",
                };
            }
        }
    }
}
#endif
