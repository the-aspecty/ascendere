using System.Reflection;
using Godot;

public static class AttributeProcessor
{
    public static void ProcessAttributes(Node target)
    {
        var type = target.GetType();
        var fields = type.GetFields(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );
        var properties = type.GetProperties(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );

        foreach (var field in fields)
        {
            ProcessFieldAttributes(target, field);
        }

        foreach (var property in properties)
        {
            ProcessPropertyAttributes(target, property);
        }
    }

    private static void ProcessFieldAttributes(Node target, FieldInfo field)
    {
        // Process AutoInitialize
        var autoInit = field.GetCustomAttribute<AutoInitializeAttribute>();
        if (autoInit != null)
        {
            var nodePath = string.IsNullOrEmpty(autoInit.NodePath)
                ? $"%{field.Name}"
                : autoInit.NodePath;
            var node = target.GetNode(nodePath);
            if (node != null)
            {
                field.SetValue(target, node);
            }
        }

        // Process other attributes...
    }

    private static void ProcessPropertyAttributes(Node target, PropertyInfo property)
    {
        // Similar to ProcessFieldAttributes
    }
}
