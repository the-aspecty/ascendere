namespace Ascendere.Log
{
    /// <summary>
    /// Service interface for logging functionality.
    /// Use this for dependency injection and service locator patterns.
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// Log a debug message
        /// </summary>
        void Debug(object source, string message);

        /// <summary>
        /// Log an info message
        /// </summary>
        void Info(object source, string message);

        /// <summary>
        /// Log a warning message
        /// </summary>
        void Warning(object source, string message);

        /// <summary>
        /// Log an error message
        /// </summary>
        void Error(object source, string message);

        /// <summary>
        /// Log a message with a specific level
        /// </summary>
        void Log(object source, LogLevel level, string message);

        /// <summary>
        /// Check if logging is enabled for a source
        /// </summary>
        bool IsLoggingEnabled(object source);

        /// <summary>
        /// Override logging behavior for a specific type at runtime
        /// </summary>
        void SetLoggingOverride(System.Type type, bool enabled);

        /// <summary>
        /// Remove a logging override for a specific type
        /// </summary>
        void RemoveLoggingOverride(System.Type type);

        /// <summary>
        /// Clear all logging caches
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Get or set the global minimum log level
        /// </summary>
        LogLevel GlobalMinimumLevel { get; set; }

        /// <summary>
        /// Enable or disable timestamps in log output
        /// </summary>
        bool EnableTimestamps { get; set; }

        /// <summary>
        /// Enable or disable type names in log output
        /// </summary>
        bool EnableTypeNames { get; set; }

        /// <summary>
        /// Enable or disable log levels in log output
        /// </summary>
        bool EnableLogLevel { get; set; }
    }
}
