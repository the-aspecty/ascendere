#if TOOLS
using System;

namespace Ascendere.Editor.CustomCommands
{
    /// <summary>
    /// Marks a class as providing editor commands.
    /// Classes with this attribute will be scanned for methods marked with [EditorCommand].
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class EditorCommandProviderAttribute : Attribute { }
}
#endif
