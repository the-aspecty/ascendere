#if TOOLS
using System;

namespace Ascendere.Editor.CustomTools
{
    /// <summary>
    /// Marks a class as providing tool menu items.
    /// Classes with this attribute will be scanned for methods marked with [ToolMenuItem].
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ToolMenuProviderAttribute : Attribute { }
}
#endif
