using System;

namespace Ascendere.Log
{
    /// <summary>
    /// Service implementation for logging that wraps the Logger singleton.
    /// Use this for integration with service locator or dependency injection patterns.
    /// </summary>
    public class LogService : ILogService
    {
        private readonly Logger _logger;

        /// <summary>
        /// Creates a new LogService using the Logger singleton
        /// </summary>
        public LogService()
        {
            _logger = Logger.Instance;
        }

        /// <summary>
        /// Creates a LogService with a specific Logger instance (for testing)
        /// </summary>
        internal LogService(Logger logger)
        {
            _logger = logger;
        }

        public void Debug(object source, string message)
        {
            _logger.Debug(source, message);
        }

        public void Info(object source, string message)
        {
            _logger.Info(source, message);
        }

        public void Warning(object source, string message)
        {
            _logger.Warning(source, message);
        }

        public void Error(object source, string message)
        {
            _logger.Error(source, message);
        }

        public void Log(object source, LogLevel level, string message)
        {
            _logger.Log(source, level, message);
        }

        public bool IsLoggingEnabled(object source)
        {
            return _logger.IsLoggingEnabled(source);
        }

        public void SetLoggingOverride(Type type, bool enabled)
        {
            _logger.SetLoggingOverride(type, enabled);
        }

        public void RemoveLoggingOverride(Type type)
        {
            _logger.RemoveLoggingOverride(type);
        }

        public void ClearCache()
        {
            _logger.ClearCache();
        }

        public LogLevel GlobalMinimumLevel
        {
            get => _logger.GlobalMinimumLevel;
            set => _logger.GlobalMinimumLevel = value;
        }

        public bool EnableTimestamps
        {
            get => _logger.EnableTimestamps;
            set => _logger.EnableTimestamps = value;
        }

        public bool EnableTypeNames
        {
            get => _logger.EnableTypeNames;
            set => _logger.EnableTypeNames = value;
        }

        public bool EnableLogLevel
        {
            get => _logger.EnableLogLevel;
            set => _logger.EnableLogLevel = value;
        }
    }
}
