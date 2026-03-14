using System;
using System.Collections.Generic;

public class ServiceNode
{
    public Type ServiceType { get; set; }
    public Type ImplementationType { get; set; }
    public string ServiceName { get; set; }
    public ServiceLifetime Lifetime { get; set; }
    public List<ServiceDependency> Dependencies { get; } = new();
}
