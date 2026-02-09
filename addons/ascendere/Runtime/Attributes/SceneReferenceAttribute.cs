using System;

[AttributeUsage(AttributeTargets.Field)]
public class SceneReferenceAttribute : Attribute
{
    public string Path { get; }

    public SceneReferenceAttribute(string path)
    {
        Path = path;
    }
}
