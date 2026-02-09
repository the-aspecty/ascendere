#if TOOLS
using Godot;

namespace Ascendere.Editor
{
    /// <summary>
    /// Contains information about a registered tab in the Ascendere main panel.
    /// </summary>
    public class TabInfo
    {
        public string TabId { get; set; }
        public string TabName { get; set; }
        public Control TabContent { get; set; }
        public int TabIndex { get; set; }
    }
}
#endif
