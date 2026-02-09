#if TOOLS
using System.Reflection;

namespace Ascendere.Editor.CustomTools
{
    /// <summary>
    /// Information about a registered tool menu item.
    /// Contains metadata and execution details for a menu item.
    /// </summary>
    public class ToolMenuItemInfo
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public MethodInfo Method { get; set; }
        public int Priority { get; set; }
        public string Icon { get; set; }
        public string Tooltip { get; set; }
        public bool IsSubmenu { get; set; }
    }
}
#endif
