namespace Ascendere.Log
{
    /// <summary>
    /// Extension methods for convenient logging
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Log a debug message from this object
        /// </summary>
        public static void LogDebug(this object source, string message)
        {
            Logger.Instance.Debug(source, message);
        }

        /// <summary>
        /// Log an info message from this object
        /// </summary>
        public static void LogInfo(this object source, string message)
        {
            Logger.Instance.Info(source, message);
        }

        /// <summary>
        /// Log a warning message from this object
        /// </summary>
        public static void LogWarning(this object source, string message)
        {
            Logger.Instance.Warning(source, message);
        }

        /// <summary>
        /// Log an error message from this object
        /// </summary>
        public static void LogError(this object source, string message)
        {
            Logger.Instance.Error(source, message);
        }

        /// <summary>
        /// Check if logging is enabled for this object
        /// </summary>
        public static bool IsLoggingEnabled(this object source)
        {
            return Logger.Instance.IsLoggingEnabled(source);
        }

        /// <summary>
        /// Override logging for this object's type at runtime
        /// </summary>
        public static void SetLoggingOverride(this object source, bool enabled)
        {
            Logger.Instance.SetLoggingOverride(source, enabled);
        }

        /// <summary>
        /// Remove logging override for this object's type
        /// </summary>
        public static void RemoveLoggingOverride(this object source)
        {
            Logger.Instance.RemoveLoggingOverride(source);
        }
    }
}
