using System;

namespace Ascendere.Log
{
    /// <summary>
    /// Attribute to enable or disable logging for a specific class
    /// Usage: [Log(true)] or [Log(false)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class LogAttribute : Attribute
    {
        public bool Enabled { get; }

        public LogAttribute(bool enabled = true)
        {
            Enabled = enabled;
        }
    }
}
