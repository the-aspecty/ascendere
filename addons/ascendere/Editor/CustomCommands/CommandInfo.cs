#if TOOLS
using System.Reflection;

namespace Ascendere.Editor.CustomCommands
{
    /// <summary>
    /// Information about a registered editor command.
    /// Contains metadata and execution details for a command.
    /// </summary>
    public class CommandInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Shortcut { get; set; }
        public int Priority { get; set; }
        public MethodInfo Method { get; set; }
    }
}
#endif
