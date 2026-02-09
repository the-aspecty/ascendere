using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Ascendere.Utils;

/// <summary>
/// Provides utility methods for scanning assemblies and registering methods decorated with a given attribute.
/// This allows automatic discovery of methods that can be processed or invoked at runtime.
/// </summary>
public static class AttributeProcessorUtils
{
    /// <summary>
    /// Delegate definition for handling discovered attributes on a MethodInfo.
    /// The handler is invoked with the method, the instance (or null for static), and the attribute instance.
    /// </summary>
    public delegate void AttributeHandler<TAttribute>(
        MethodInfo method,
        object instance,
        TAttribute attribute
    )
        where TAttribute : Attribute;

    public delegate void FieldAttributeHandler<TAttribute>(
        FieldInfo field,
        object instance,
        TAttribute attribute
    )
        where TAttribute : Attribute;

    public delegate void PropertyAttributeHandler<TAttribute>(
        PropertyInfo property,
        object instance,
        TAttribute attribute
    )
        where TAttribute : Attribute;

    public delegate Task FieldAttributeHandlerAsync<TAttribute>(
        FieldInfo field,
        object instance,
        TAttribute attribute
    )
        where TAttribute : Attribute;

    /// <summary>
    /// Scans the specified assembly (or the current executing assembly if none is provided)
    /// to find all types containing methods decorated with <typeparamref name="TAttribute"/>.
    /// It automatically instantiates classes (with parameterless constructors) containing these methods,
    /// and calls the specified handler for each method found.
    /// </summary>
    public static void ScanAndRegisterAttributes<TAttribute>(
        Assembly assembly,
        AttributeHandler<TAttribute> handler
    )
        where TAttribute : Attribute
    {
        assembly ??= Assembly.GetExecutingAssembly();

        var types = assembly
            .GetTypes()
            .Where(t =>
                t.GetMethods(
                        BindingFlags.Public
                            | BindingFlags.NonPublic
                            | BindingFlags.Instance
                            | BindingFlags.Static
                    )
                    .Any(m => m.GetCustomAttribute<TAttribute>() != null)
            );

        foreach (var type in types)
        {
            // Handle static methods
            if (
                type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Any(m => m.GetCustomAttribute<TAttribute>() != null)
            )
            {
                RegisterAttributes(null, type, handler);
            }

            // Handle instance methods
            if (type.GetConstructor(Type.EmptyTypes) != null)
            {
                var instance = Activator.CreateInstance(type);
                RegisterAttributes(instance, type, handler);
            }
        }
    }

    /// <summary>
    /// Registers methods decorated with <typeparamref name="TAttribute"/> found in the given
    /// <paramref name="type"/> or <paramref name="instance"/>, and calls the specified handler for each.
    /// </summary>
    public static void RegisterAttributes<TAttribute>(
        object instance,
        Type type,
        AttributeHandler<TAttribute> handler
    )
        where TAttribute : Attribute
    {
        var methods = (type ?? instance.GetType())
            .GetMethods(
                BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
                    | BindingFlags.Static
            )
            .Where(m => m.GetCustomAttribute<TAttribute>() != null);

        foreach (var method in methods)
        {
            if (method.IsStatic || instance != null)
            {
                var attr = method.GetCustomAttribute<TAttribute>();
                handler(method, instance, attr);
            }
        }
    }

    /// <summary>
    /// Scans an assembly for fields marked with a specific attribute and registers them with a handler.
    /// </summary>
    /// <typeparam name="TAttribute">The type of attribute to scan for. Must inherit from Attribute.</typeparam>
    /// <param name="assembly">The assembly to scan. If null, uses the executing assembly.</param>
    /// <param name="handler">The handler delegate that will process fields marked with the specified attribute.</param>
    /// <remarks>
    /// This method performs the following:
    /// 1. Scans the specified assembly for types containing fields marked with TAttribute
    /// 2. For each type found:
    ///    - Processes static fields marked with TAttribute
    ///    - If the type has a parameterless constructor, instantiates it and processes instance fields
    /// 3. Calls the handler for each field found, passing the field, instance (or null for static), and the attribute instance.
    public static void ScanAndRegisterFieldAttributes<TAttribute>(
        Assembly assembly,
        FieldAttributeHandler<TAttribute> handler
    )
        where TAttribute : Attribute
    {
        assembly ??= Assembly.GetExecutingAssembly();
        var types = assembly
            .GetTypes()
            .Where(t =>
                t.GetFields(
                        BindingFlags.Public
                            | BindingFlags.NonPublic
                            | BindingFlags.Instance
                            | BindingFlags.Static
                    )
                    .Any(f => f.GetCustomAttribute<TAttribute>() != null)
            );

        foreach (var type in types)
        {
            // Handle static fields
            if (
                type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Any(f => f.GetCustomAttribute<TAttribute>() != null)
            )
            {
                RegisterFieldAttributes(null, type, handler);
            }

            // Handle instance fields
            if (type.GetConstructor(Type.EmptyTypes) != null)
            {
                var instance = Activator.CreateInstance(type);
                RegisterFieldAttributes(instance, type, handler);
            }
        }
    }

    public static void RegisterFieldAttributes<TAttribute>(
        object instance,
        Type type,
        FieldAttributeHandler<TAttribute> handler
    )
        where TAttribute : Attribute
    {
        var fields = (type ?? instance.GetType())
            .GetFields(
                BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
                    | BindingFlags.Static
            )
            .Where(f => f.GetCustomAttribute<TAttribute>() != null);

        foreach (var field in fields)
        {
            if (field.IsStatic || instance != null)
            {
                var attr = field.GetCustomAttribute<TAttribute>();
                handler(field, instance, attr);
            }
        }
    }

    public static async Task ScanAndRegisterFieldAttributesAsync<TAttribute>(
        Assembly assembly,
        FieldAttributeHandlerAsync<TAttribute> handler
    )
        where TAttribute : Attribute
    {
        assembly ??= Assembly.GetExecutingAssembly();
        var types = assembly
            .GetTypes()
            .Where(t =>
                t.GetFields(
                        BindingFlags.Public
                            | BindingFlags.NonPublic
                            | BindingFlags.Instance
                            | BindingFlags.Static
                    )
                    .Any(f => f.GetCustomAttribute<TAttribute>() != null)
            );

        foreach (var type in types)
        {
            if (
                type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Any(f => f.GetCustomAttribute<TAttribute>() != null)
            )
            {
                await RegisterFieldAttributesAsync(null, type, handler);
            }

            if (type.GetConstructor(Type.EmptyTypes) != null)
            {
                var instance = Activator.CreateInstance(type);
                await RegisterFieldAttributesAsync(instance, type, handler);
            }
        }
    }

    public static async Task RegisterFieldAttributesAsync<TAttribute>(
        object instance,
        Type type,
        FieldAttributeHandlerAsync<TAttribute> handler
    )
        where TAttribute : Attribute
    {
        var fields = (type ?? instance.GetType())
            .GetFields(
                BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
                    | BindingFlags.Static
            )
            .Where(f => f.GetCustomAttribute<TAttribute>() != null);

        foreach (var field in fields)
        {
            if (field.IsStatic || instance != null)
            {
                var attr = field.GetCustomAttribute<TAttribute>();
                await handler(field, instance, attr);
            }
        }
    }

    public static void ScanAndRegisterPropertyAttributes<TAttribute>(
        Assembly assembly,
        PropertyAttributeHandler<TAttribute> handler
    )
        where TAttribute : Attribute
    {
        assembly ??= Assembly.GetExecutingAssembly();
        var types = assembly
            .GetTypes()
            .Where(t =>
                t.GetProperties(
                        BindingFlags.Public
                            | BindingFlags.NonPublic
                            | BindingFlags.Instance
                            | BindingFlags.Static
                    )
                    .Any(p => p.GetCustomAttribute<TAttribute>() != null)
            );

        foreach (var type in types)
        {
            // Handle static properties
            if (
                type.GetProperties(
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                    )
                    .Any(p => p.GetCustomAttribute<TAttribute>() != null)
            )
            {
                RegisterPropertyAttributes(null, type, handler);
            }

            // Handle instance properties
            if (type.GetConstructor(Type.EmptyTypes) != null)
            {
                var instance = Activator.CreateInstance(type);
                RegisterPropertyAttributes(instance, type, handler);
            }
        }
    }

    public static void RegisterPropertyAttributes<TAttribute>(
        object instance,
        Type type,
        PropertyAttributeHandler<TAttribute> handler
    )
        where TAttribute : Attribute
    {
        var props = (type ?? instance.GetType())
            .GetProperties(
                BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
                    | BindingFlags.Static
            )
            .Where(p => p.GetCustomAttribute<TAttribute>() != null);

        foreach (var prop in props)
        {
            if ((prop.GetGetMethod(true)?.IsStatic ?? false) || instance != null)
            {
                var attr = prop.GetCustomAttribute<TAttribute>();
                handler(prop, instance, attr);
            }
        }
    }

    /// <summary>
    /// Ensures that a method does not use out or retval parameters. Throws an exception if found.
    /// </summary>
    public static void ValidateMethodParameters(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Any(p => p.IsOut || p.IsRetval))
            throw new ArgumentException($"Method {method.Name} cannot have out parameters");
    }
}
