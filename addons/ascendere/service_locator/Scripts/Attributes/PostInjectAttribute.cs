using System;

/// <summary>
/// Marks a method to be called after all dependencies are injected
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class PostInjectAttribute : Attribute { }
