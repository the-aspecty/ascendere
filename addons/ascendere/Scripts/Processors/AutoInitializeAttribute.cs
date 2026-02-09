using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class AutoInitializeAttribute : Attribute
{
    public string NodePath { get; }

    public AutoInitializeAttribute(string nodePath = "")
    {
        NodePath = nodePath;
    }
}
