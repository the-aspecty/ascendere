using System;

/// <summary>
/// Marks a class as a service for automatic registration
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ServiceAttribute : Attribute
{
    public Type InterfaceType { get; }
    public ServiceLifetime Lifetime { get; }
    public int Priority { get; }
    public bool Lazy { get; }
    public string Name { get; }

    public ServiceAttribute(
        Type interfaceType = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        int priority = 0,
        bool lazy = false,
        string name = null
    )
    {
        InterfaceType = interfaceType;
        Lifetime = lifetime;
        Priority = priority;
        Lazy = lazy;
        Name = name;
    }
}
