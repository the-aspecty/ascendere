#if TOOLS
using System;

namespace Ascendere.Editor.CustomTools
{
    /// <summary>
    /// Marks a static method as a tool menu item.
    /// The method will be automatically discovered and added to the editor's Tools menu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ToolMenuItemAttribute : Attribute
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Icon { get; set; }
        public string Tooltip { get; set; }
        public int Priority { get; set; }

        public ToolMenuItemAttribute(string name)
        {
            Name = name;
        }
    }
}
#endif
