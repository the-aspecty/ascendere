using System;

/// <summary>
/// Marks a method for async initialization
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class InitializeAttribute : Attribute { }
