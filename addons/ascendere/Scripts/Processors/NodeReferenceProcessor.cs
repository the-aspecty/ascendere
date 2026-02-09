using System;
using System.Collections.Generic;
using System.Reflection;
using Ascendere.Log;
using Godot;

[Log(false)]
public class NodeReferenceProcessor
{
    private static NodeReferenceProcessor _instance;
    public static NodeReferenceProcessor Instance => _instance ??= new NodeReferenceProcessor();

    // Track which nodes have been processed for cleanup
    private readonly Dictionary<Node, List<FieldInfo>> _trackedReferences =
        new Dictionary<Node, List<FieldInfo>>();

    public void ProcessNodeReferences(Node node)
    {
        var type = node.GetType();
        var fields = type.GetFields(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );

        var processedFields = new List<FieldInfo>();

        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<NodeReferenceAttribute>();
            if (attribute == null)
                continue;

            if (!typeof(Node).IsAssignableFrom(field.FieldType))
            {
                this.LogError($"Field {field.Name} must be of type Node or derived from Node");
                continue;
            }

            try
            {
                var referencedNode = node.GetNode(attribute.Path);
                if (referencedNode != null)
                {
                    if (field.FieldType.IsAssignableFrom(referencedNode.GetType()))
                    {
                        field.SetValue(node, referencedNode);
                        processedFields.Add(field);
                        this.LogInfo($"Linked node at {attribute.Path} to {field.Name}");
                    }
                    else
                    {
                        this.LogError(
                            $"Node at {attribute.Path} is not of type {field.FieldType.Name}"
                        );
                    }
                }
                else if (attribute.Required)
                {
                    this.LogError($"Required node not found at path: {attribute.Path}");
                }
            }
            catch (Exception ex)
            {
                this.LogError($"Error linking node for {field.Name}: {ex.Message}");
            }
        }

        // Track the node and its references for cleanup
        if (processedFields.Count > 0)
        {
            _trackedReferences[node] = processedFields;

            // Subscribe to TreeExiting for automatic cleanup using a lambda
            // that captures the node weakly to avoid circular references
            if (
                !node.IsConnected(
                    Node.SignalName.TreeExiting,
                    Callable.From(() => CleanupNodeReferences(node))
                )
            )
            {
                node.Connect(
                    Node.SignalName.TreeExiting,
                    Callable.From(() => CleanupNodeReferences(node))
                );
            }
        }
    }

    /// <summary>
    /// Manually clear all node references for a specific node
    /// Call this in _ExitTree() if you want explicit control
    /// </summary>
    public void CleanupNodeReferences(Node node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
        {
            return;
        }

        if (_trackedReferences.TryGetValue(node, out var fields))
        {
            foreach (var field in fields)
            {
                try
                {
                    // Set the field to null to release the reference
                    field.SetValue(node, null);
                }
                catch (Exception ex)
                {
                    this.LogError($"Error clearing node reference {field.Name}: {ex.Message}");
                }
            }

            _trackedReferences.Remove(node);
            this.LogInfo($"Cleaned up {fields.Count} references for {node.GetType().Name}");
        }
    }

    /// <summary>
    /// Validate that a node reference is still valid before using it
    /// Use this when accessing node references that might have been freed
    /// </summary>
    public bool ValidateNodeReference(Node node)
    {
        return node != null && GodotObject.IsInstanceValid(node) && node.IsInsideTree();
    }

    /// <summary>
    /// Clear all tracked references (useful for complete cleanup)
    /// </summary>
    public void ClearAllTrackedReferences()
    {
        var nodes = new List<Node>(_trackedReferences.Keys);
        foreach (var node in nodes)
        {
            CleanupNodeReferences(node);
        }
        _trackedReferences.Clear();
        this.LogInfo("Cleared all tracked node references");
    }
}
