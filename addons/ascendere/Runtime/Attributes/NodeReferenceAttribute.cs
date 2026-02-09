using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class NodeAttribute : Attribute
{
    public string NodePath { get; }

    public NodeAttribute(string nodePath)
    {
        NodePath = nodePath;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class NodeReferenceAttribute : Attribute
{
    public string Path { get; }
    public bool Required { get; }
    public NodePathHint Hint { get; }

    public NodeReferenceAttribute(
        string path,
        bool required = true,
        NodePathHint hint = NodePathHint.All
    )
    {
        Path = path;
        Required = required;
        Hint = hint;
    }
}

public enum NodePathHint
{
    All,
    Children,
    Parent,
    Siblings,
    Scene,
}
