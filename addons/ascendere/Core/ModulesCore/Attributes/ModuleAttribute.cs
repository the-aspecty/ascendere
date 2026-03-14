using System;

[AttributeUsage(AttributeTargets.Class)]
public class ModuleAttribute : Attribute
{
    public string Name { get; }
    public bool AutoLoad { get; }
    public int LoadOrder { get; set; }

    public ModuleAttribute(string name = null, bool autoLoad = true, int loadOrder = 0)
    {
        Name = name;
        AutoLoad = autoLoad;
        LoadOrder = loadOrder;
    }
}
