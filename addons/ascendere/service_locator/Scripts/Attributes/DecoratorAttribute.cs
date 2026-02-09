using System;

/// <summary>
/// Marks a class as a service decorator
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DecoratorAttribute : Attribute
{
    public Type ServiceType { get; }
    public int Order { get; }

    public DecoratorAttribute(Type serviceType, int order = 0)
    {
        ServiceType = serviceType;
        Order = order;
    }
}
