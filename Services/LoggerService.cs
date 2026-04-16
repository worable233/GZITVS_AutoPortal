using System;
using System.IO;
using System.Threading.Tasks;

namespace AutoPortal.Services
{
    /// <summary>
    /// 应用程序日志服务
    /// </summary>
    public class LoggerService
    {
        private static LoggerService? _instance;
        private readonly string _logFilePath;
        private static readonly object _lockObject = new object();

        public static LoggerService Instance => _instance ??= new LoggerService();

        public LoggerService()
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoPortal",
                "Logs"
            );

            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            _logFilePath = Path.Combine(logDir, $"log_{DateTime.Now:yyyy-MM-dd}.txt");
        }

        public void Info(string message, string? category = null)
        {
            LogMessage(LogLevel.Info, message, category);
        }

        public void Warning(string message, string? category = null)
        {
            LogMessage(LogLevel.Warning, message, category);
        }

        public void Error(string message, Exception? exception = null, string? category = null)
        {
            var fullMessage = exception == null
                ? message
                : $"{message}\n{exception}";
            LogMessage(LogLevel.Error, fullMessage, category);
        }

        public void Debug(string message, string? category = null)
        {
#if DEBUG
            LogMessage(LogLevel.Debug, message, category);
#endif
        }

        private void LogMessage(LogLevel level, string message, string? category)
        {
            try
            {
                lock (_lockObject)
                {
                    var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{category ?? "General"}] {message}";
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // 日志写入失败时不抛出异常
            }
        }

        private enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }
    }
}
