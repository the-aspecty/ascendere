using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;

namespace Ascendere.Log
{
    /// <summary>
    /// Main logger implementation that respects [Log] attributes on classes
    /// </summary>
    public partial class Logger : Node, ILogger
    {
        private static Logger _instance;
        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Logger();
                }
                return _instance;
            }
        }

        private readonly Dictionary<Type, bool> _loggingCache = new();
        private readonly Dictionary<Type, bool> _loggingOverrides = new();
        private LogLevel _globalMinimumLevel = LogLevel.Debug;

        [Export]
        public LogLevel GlobalMinimumLevel
        {
            get => _globalMinimumLevel;
            set => _globalMinimumLevel = value;
        }

        [Export]
        public bool EnableTimestamps { get; set; } = true;

        [Export]
        public bool EnableTypeNames { get; set; } = true;

        [Export]
        public bool EnableLogLevel { get; set; } = true;

        public override void _Ready()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                QueueFree();
            }
        }

        /// <summary>
        /// Override logging behavior for a specific type at runtime
        /// </summary>
        /// <param name="type">The type to override</param>
        /// <param name="enabled">Whether logging should be enabled</param>
        public void SetLoggingOverride(Type type, bool enabled)
        {
            _loggingOverrides[type] = enabled;
            // Update cache if it exists
            if (_loggingCache.ContainsKey(type))
            {
                _loggingCache[type] = enabled;
            }
        }

        /// <summary>
        /// Override logging behavior for a specific object's type at runtime
        /// </summary>
        /// <param name="source">The source object</param>
        /// <param name="enabled">Whether logging should be enabled</param>
        public void SetLoggingOverride(object source, bool enabled)
        {
            if (source != null)
            {
                SetLoggingOverride(source.GetType(), enabled);
            }
        }

        /// <summary>
        /// Remove a logging override for a specific type
        /// </summary>
        /// <param name="type">The type to remove override for</param>
        public void RemoveLoggingOverride(Type type)
        {
            _loggingOverrides.Remove(type);
            // Clear cache to re-evaluate from attribute
            _loggingCache.Remove(type);
        }

        /// <summary>
        /// Remove a logging override for a specific object's type
        /// </summary>
        /// <param name="source">The source object</param>
        public void RemoveLoggingOverride(object source)
        {
            if (source != null)
            {
                RemoveLoggingOverride(source.GetType());
            }
        }

        /// <summary>
        /// Checks if logging is enabled for the given source object
        /// </summary>
        public bool IsLoggingEnabled(object source)
        {
            if (source == null)
                return true; // Allow null sources to log

            var type = source.GetType();

            // Check for runtime override first
            if (_loggingOverrides.TryGetValue(type, out bool overrideValue))
            {
                return overrideValue;
            }

            // Check cache second
            if (_loggingCache.TryGetValue(type, out bool cached))
            {
                return cached;
            }

            // Check for [Log] attribute last
            var logAttr = type.GetCustomAttribute<LogAttribute>(true);
            bool enabled = logAttr?.Enabled ?? true; // Default to true if no attribute

            _loggingCache[type] = enabled;
            return enabled;
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        public void Debug(object source, string message)
        {
            Log(source, LogLevel.Debug, message);
        }

        /// <summary>
        /// Log an info message
        /// </summary>
        public void Info(object source, string message)
        {
            Log(source, LogLevel.Info, message);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public void Warning(object source, string message)
        {
            Log(source, LogLevel.Warning, message);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public void Error(object source, string message)
        {
            Log(source, LogLevel.Error, message);
        }

        /// <summary>
        /// Main logging method
        /// </summary>
        public void Log(object source, LogLevel level, string message)
        {
            // Check if logging is enabled for this source
            if (!IsLoggingEnabled(source))
            {
                return;
            }

            // Check minimum level
            if (level < _globalMinimumLevel)
            {
                return;
            }

            // Build the log message
            string formattedMessage = FormatMessage(source, level, message);

            // Output using appropriate Godot method
            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    GD.Print(formattedMessage);
                    break;
                case LogLevel.Warning:
                    GD.PushWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                    GD.PushError(formattedMessage);
                    break;
            }
        }

        private string FormatMessage(object source, LogLevel level, string message)
        {
            var parts = new List<string>();

            if (EnableTimestamps)
            {
                parts.Add($"[{DateTime.Now:HH:mm:ss.fff}]");
            }

            if (EnableLogLevel)
            {
                parts.Add($"[{level}]");
            }

            if (EnableTypeNames && source != null)
            {
                parts.Add($"[{source.GetType().Name}]");
            }

            parts.Add(message);

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Clear the logging cache (useful if attributes are changed at runtime)
        /// </summary>
        public void ClearCache()
        {
            _loggingCache.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _instance == this)
            {
                _loggingCache.Clear();
                _loggingOverrides.Clear();
                _instance = null;
            }
            base.Dispose(disposing);
        }
    }
}
