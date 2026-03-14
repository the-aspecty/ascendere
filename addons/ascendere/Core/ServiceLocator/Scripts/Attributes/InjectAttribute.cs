using System;

/// <summary>
/// Marks properties/fields for automatic dependency injection
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class InjectAttribute : Attribute
{
    public bool Optional { get; }
    public string Name { get; }

    public InjectAttribute(bool optional = false, string name = null)
    {
        Optional = optional;
        Name = name;
    }
}
