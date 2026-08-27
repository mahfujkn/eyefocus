using System;
using System.IO;

namespace EyeFocus.Storage
{
    public static class LogService
    {
        private static readonly string LogDir;
        private static readonly string LogFile;
        private static readonly object LockObj = new();
        public static bool IsDebugEnabled { get; set; } = false;

        static LogService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            LogDir = Path.Combine(appData, "EyeFocus", "Logs");
            LogFile = Path.Combine(LogDir, $"EyeFocus_{DateTime.Now:yyyyMMdd}.log");
        }

        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public static void Debug(string message)
        {
            if (IsDebugEnabled)
            {
                WriteLog("DEBUG", message);
            }
        }

        public static void Warn(string message)
        {
            WriteLog("WARN", message);
        }

        public static void Error(string message, Exception? ex = null)
        {
            var msg = ex != null ? $"{message} | Exception: {ex.Message} | StackTrace: {ex.StackTrace}" : message;
            WriteLog("ERROR", msg);
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                lock (LockObj)
                {
                    if (!Directory.Exists(LogDir))
                    {
                        Directory.CreateDirectory(LogDir);
                    }

                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logLine = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";
                    File.AppendAllText(LogFile, logLine);
                }
            }
            catch
            {
                // Silently ignore logging failures to never crash the main application
            }
        }

        public static void ClearLogs()
        {
            try
            {
                lock (LockObj)
                {
                    if (Directory.Exists(LogDir))
                    {
                        var files = Directory.GetFiles(LogDir, "*.log");
                        foreach (var file in files)
                        {
                            File.Delete(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error("Failed to clear logs", ex);
            }
        }

        public static string GetLogDirectory() => LogDir;
    }
}
