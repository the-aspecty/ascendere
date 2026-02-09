using System;
using System.Collections.Generic;

public class ServiceContext
{
    public Type ServiceType { get; }
    public string ServiceName { get; }
    public DateTime RequestTime { get; }
    public Dictionary<string, object> Data { get; }

    public ServiceContext(Type serviceType, string serviceName)
    {
        ServiceType = serviceType;
        ServiceName = serviceName;
        RequestTime = DateTime.UtcNow;
        Data = new Dictionary<string, object>();
    }
}
