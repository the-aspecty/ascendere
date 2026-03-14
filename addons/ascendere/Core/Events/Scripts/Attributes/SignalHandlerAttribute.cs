// Attribute to connect to Godot signals automatically
using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class SignalHandlerAttribute : Attribute
{
    public string SignalName { get; }
    public string NodePath { get; }

    public SignalHandlerAttribute(string signalName)
    {
        SignalName = signalName;
        NodePath = null;
    }

    public SignalHandlerAttribute(string signalName, string nodePath)
    {
        SignalName = signalName;
        NodePath = nodePath;
    }
}
