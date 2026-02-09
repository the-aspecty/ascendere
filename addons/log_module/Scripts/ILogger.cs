namespace Ascendere.Log
{
    /// <summary>
    /// Interface for logging functionality
    /// </summary>
    public interface ILogger
    {
        void Debug(object source, string message);
        void Info(object source, string message);
        void Warning(object source, string message);
        void Error(object source, string message);
        void Log(object source, LogLevel level, string message);
        bool IsLoggingEnabled(object source);
    }
}
