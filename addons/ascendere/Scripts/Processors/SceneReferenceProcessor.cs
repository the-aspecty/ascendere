using System;
using System.Reflection;
using Godot;

public class SceneReferenceProcessor
{
    private static SceneReferenceProcessor _instance;
    public static SceneReferenceProcessor Instance => _instance ??= new SceneReferenceProcessor();

    public void ProcessSceneReferences(Node node)
    {
        var type = node.GetType();
        var fields = type.GetFields(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );

        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<SceneReferenceAttribute>();
            if (attribute == null)
                continue;

            if (field.FieldType != typeof(PackedScene))
            {
                GD.PrintErr($"Field {field.Name} must be of type PackedScene");
                continue;
            }

            try
            {
                var scene = GD.Load<PackedScene>(attribute.Path);
                if (scene != null)
                {
                    field.SetValue(node, scene);
                    GD.Print($"Loaded scene {attribute.Path} into {field.Name}");
                }
                else
                {
                    GD.PrintErr($"Failed to load scene at path: {attribute.Path}");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error loading scene for {field.Name}: {ex.Message}");
            }
        }
    }
}
