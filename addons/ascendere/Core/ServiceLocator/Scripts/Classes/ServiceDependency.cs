using System;

public class ServiceDependency
{
    public Type DependencyType { get; set; }
    public string DependencyName { get; set; }
    public bool IsRequired { get; set; }
    public string InjectionPoint { get; set; }
}
