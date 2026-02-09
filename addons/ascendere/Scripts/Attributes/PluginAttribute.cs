using System;

namespace Ascendere;

[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public string Name { get; }
    public string Description { get; set; }
    public bool Core { get; }
    public int LoadOrder { get; set; }

    public PluginAttribute(
        string name = null,
        string description = null,
        bool core = false,
        int loadOrder = 0
    )
    {
        Name = name;
        Description = description;
        Core = core;
        LoadOrder = loadOrder;
    }
}
