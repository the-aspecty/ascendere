// === DEPENDENCY GRAPH TYPES ===

using System;
using System.Collections.Generic;
using System.Linq;

public enum GraphFormat
{
    Mermaid,
    Dot,
}

public class DependencyGraph
{
    public List<ServiceNode> Nodes { get; } = new();
    private readonly Dictionary<Type, ServiceNode> _nodeMap = new();

    public void AddNode(ServiceNode node)
    {
        Nodes.Add(node);
        _nodeMap[node.ServiceType] = node;
    }

    public List<List<Type>> DetectCircularDependencies()
    {
        var cycles = new List<List<Type>>();
        var visited = new HashSet<Type>();
        var recursionStack = new HashSet<Type>();

        foreach (var node in Nodes)
        {
            if (!visited.Contains(node.ServiceType))
            {
                var path = new List<Type>();
                DetectCyclesRecursive(node.ServiceType, visited, recursionStack, path, cycles);
            }
        }

        return cycles;
    }

    private void DetectCyclesRecursive(
        Type current,
        HashSet<Type> visited,
        HashSet<Type> recursionStack,
        List<Type> path,
        List<List<Type>> cycles
    )
    {
        visited.Add(current);
        recursionStack.Add(current);
        path.Add(current);

        if (_nodeMap.TryGetValue(current, out var node))
        {
            foreach (var dep in node.Dependencies)
            {
                if (!visited.Contains(dep.DependencyType))
                {
                    DetectCyclesRecursive(
                        dep.DependencyType,
                        visited,
                        recursionStack,
                        path,
                        cycles
                    );
                }
                else if (recursionStack.Contains(dep.DependencyType))
                {
                    // Found a cycle
                    var cycleStart = path.IndexOf(dep.DependencyType);
                    var cycle = path.Skip(cycleStart).Append(dep.DependencyType).ToList();
                    cycles.Add(cycle);
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        recursionStack.Remove(current);
    }

    public List<ServiceNode> GetInitializationOrder()
    {
        var result = new List<ServiceNode>();
        var visited = new HashSet<Type>();
        var temp = new HashSet<Type>();

        foreach (var node in Nodes)
        {
            if (!visited.Contains(node.ServiceType))
            {
                TopologicalSort(node.ServiceType, visited, temp, result);
            }
        }

        result.Reverse();
        return result;
    }

    private void TopologicalSort(
        Type current,
        HashSet<Type> visited,
        HashSet<Type> temp,
        List<ServiceNode> result
    )
    {
        if (temp.Contains(current))
            return; // Circular dependency
        if (visited.Contains(current))
            return;

        temp.Add(current);

        if (_nodeMap.TryGetValue(current, out var node))
        {
            foreach (var dep in node.Dependencies.Where(d => d.IsRequired))
            {
                TopologicalSort(dep.DependencyType, visited, temp, result);
            }
        }

        temp.Remove(current);
        visited.Add(current);

        if (_nodeMap.TryGetValue(current, out var currentNode))
        {
            result.Add(currentNode);
        }
    }

    public string ToMermaid()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("graph TD");

        foreach (var node in Nodes)
        {
            var nodeName = GetNodeName(node);
            var label = string.IsNullOrEmpty(node.ServiceName)
                ? node.ServiceType.Name
                : $"{node.ServiceType.Name}<br/>({node.ServiceName})";

            sb.AppendLine($"    {nodeName}[\"{label}\n{node.Lifetime}\"]");

            foreach (var dep in node.Dependencies)
            {
                var depNode = Nodes.FirstOrDefault(n => n.ServiceType == dep.DependencyType);
                if (depNode != null)
                {
                    var depNodeName = GetNodeName(depNode);
                    var arrow = dep.IsRequired ? "-->" : "-.->";
                    var label2 = string.IsNullOrEmpty(dep.DependencyName)
                        ? ""
                        : $"|{dep.DependencyName}|";
                    sb.AppendLine($"    {nodeName} {arrow}{label2} {depNodeName}");
                }
            }
        }

        return sb.ToString();
    }

    public string ToDot()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("digraph ServiceDependencies {");
        sb.AppendLine("    rankdir=LR;");
        sb.AppendLine("    node [shape=box, style=rounded];");

        foreach (var node in Nodes)
        {
            var nodeName = GetNodeName(node);
            var label = string.IsNullOrEmpty(node.ServiceName)
                ? $"{node.ServiceType.Name}\\n{node.Lifetime}"
                : $"{node.ServiceType.Name}\\n({node.ServiceName})\\n{node.Lifetime}";

            sb.AppendLine($"    {nodeName} [label=\"{label}\"];");

            foreach (var dep in node.Dependencies)
            {
                var depNode = Nodes.FirstOrDefault(n => n.ServiceType == dep.DependencyType);
                if (depNode != null)
                {
                    var depNodeName = GetNodeName(depNode);
                    var style = dep.IsRequired ? "solid" : "dashed";
                    var labelText = string.IsNullOrEmpty(dep.DependencyName)
                        ? ""
                        : $"label=\"{dep.DependencyName}\"";
                    sb.AppendLine($"    {nodeName} -> {depNodeName} [style={style} {labelText}];");
                }
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private string GetNodeName(ServiceNode node)
    {
        var name = node.ServiceType.Name.Replace("<", "_").Replace(">", "_");
        if (!string.IsNullOrEmpty(node.ServiceName))
        {
            name += "_" + node.ServiceName.Replace(" ", "_");
        }
        return name;
    }
}
