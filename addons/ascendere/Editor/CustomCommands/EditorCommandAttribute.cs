#if TOOLS
using System;

namespace Ascendere.Editor.CustomCommands
{
    /// <summary>
    /// Marks a static method as an editor command.
    /// The method will be automatically discovered and registered with the command palette.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class EditorCommandAttribute : Attribute
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Shortcut { get; set; }
        public int Priority { get; set; }

        public EditorCommandAttribute(string name)
        {
            Name = name;
        }
    }
}
#endif
