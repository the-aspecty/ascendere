#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace Ascendere.Utils
{
    /// <summary>
    /// Utility class for discovering Meta Framework attributes across assemblies.
    /// </summary>
    public static class AttributeDiscovery
    {
        /// <summary>
        /// Discovers all types decorated with Meta Framework attributes in the current app domain.
        /// </summary>
        /// <returns>A collection of discovered meta types.</returns>
        public static MetaDiscoveryResult DiscoverMetaTypes()
        {
            var result = new MetaDiscoveryResult();

            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        // Skip system assemblies and Godot assemblies for performance
                        if (IsSystemAssembly(assembly))
                            continue;

                        var types = assembly.GetTypes();

                        foreach (var type in types)
                        {
                            try
                            {
                                DiscoverTypeAttributes(type, result);
                            }
                            catch (Exception ex)
                            {
                                GD.PrintErr(
                                    $"[AttributeDiscovery] Error processing type {type.Name}: {ex.Message}"
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr(
                            $"[AttributeDiscovery] Error processing assembly {assembly.FullName}: {ex.Message}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[AttributeDiscovery] Critical error during discovery: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Discovers only components with Meta Framework attributes.
        /// </summary>
        /// <returns>A list of discovered components.</returns>
        public static List<MetaComponentInfo> DiscoverComponents()
        {
            var result = DiscoverMetaTypes();
            return result.Components;
        }

        /// <summary>
        /// Discovers only entities with Meta Framework attributes.
        /// </summary>
        /// <returns>A list of discovered entities.</returns>
        public static List<MetaEntityInfo> DiscoverEntities()
        {
            var result = DiscoverMetaTypes();
            return result.Entities;
        }

        /// <summary>
        /// Discovers only systems with Meta Framework attributes.
        /// </summary>
        /// <returns>A list of discovered systems.</returns>
        public static List<MetaSystemInfo> DiscoverSystems()
        {
            var result = DiscoverMetaTypes();
            return result.Systems;
        }

        /// <summary>
        /// Discovers attributes for a specific type.
        /// </summary>
        /// <param name="type">The type to examine.</param>
        /// <param name="result">The result collection to populate.</param>
        private static void DiscoverTypeAttributes(Type type, MetaDiscoveryResult result)
        {
            // Check for MetaComponent attribute
            var componentAttr = type.GetCustomAttribute<MetaComponentAttribute>();
            if (componentAttr != null)
            {
                result.Components.Add(
                    new MetaComponentInfo
                    {
                        Type = type,
                        Attribute = componentAttr,
                        Name = componentAttr.Name,
                        Category = componentAttr.Category,
                        ComponentType = componentAttr.ComponentType,
                        AllowMultiple = componentAttr.AllowMultiple,
                    }
                );
            }

            // Check for GameEntity attribute
            var entityAttr = type.GetCustomAttribute<GameEntityAttribute>();
            if (entityAttr != null)
            {
                result.Entities.Add(
                    new MetaEntityInfo
                    {
                        Type = type,
                        Attribute = entityAttr,
                        Name = entityAttr.Name,
                        Category = entityAttr.Category,
                        Description = entityAttr.Description,
                        AutoRegister = entityAttr.AutoRegister,
                    }
                );
            }

            // Check for MetaSystem attribute
            var systemAttr = type.GetCustomAttribute<MetaSystemAttribute>();
            if (systemAttr != null)
            {
                result.Systems.Add(
                    new MetaSystemInfo
                    {
                        Type = type,
                        Attribute = systemAttr,
                        Name = systemAttr.Name,
                        Priority = systemAttr.Priority,
                        ProcessEveryFrame = systemAttr.ProcessEveryFrame,
                        NeedsPhysicsProcess = systemAttr.NeedsPhysicsProcess,
                    }
                );
            }
        }

        /// <summary>
        /// Checks if an assembly is a system assembly that should be skipped.
        /// </summary>
        /// <param name="assembly">The assembly to check.</param>
        /// <returns>True if the assembly should be skipped.</returns>
        private static bool IsSystemAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name;
            return name.StartsWith("System")
                || name.StartsWith("Microsoft")
                || name.StartsWith("mscorlib")
                || name.StartsWith("netstandard")
                || name.Equals("Godot")
                || name.Equals("GodotSharp")
                || name.Equals("GodotSharpEditor")
                || name.Equals("Mono.Cecil")
                || name.Equals("Mono.Security");
        }
    }
}
#endif
