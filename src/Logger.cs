using System;
using System.IO;
using System.Text;

namespace MicControlX
{
    /// <summary>
    /// Simple file-based logger for user-accessible diagnostics.
    /// Logs are written to %APPDATA%\MicControlX\logs\
    /// </summary>
    public static class Logger
    {
        private static readonly string LogDirectory;
        private static readonly string LogFilePath;
        private static readonly object _lock = new object();
        private static bool _initialized = false;
        
        static Logger()
        {
            LogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MicControlX",
                "Logs"
            );
            LogFilePath = Path.Combine(LogDirectory, "miccontrolx.log");
        }
        
        /// <summary>
        /// Initialize the logger and write startup information.
        /// Called once at application startup.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // Create logs directory if it doesn't exist
                Directory.CreateDirectory(LogDirectory);
                
                // Rotate log file if it's too large (> 1MB)
                if (File.Exists(LogFilePath))
                {
                    var fileInfo = new FileInfo(LogFilePath);
                    if (fileInfo.Length > 1024 * 1024) // 1MB
                    {
                        var backupPath = Path.Combine(LogDirectory, "miccontrolx.old.log");
                        if (File.Exists(backupPath))
                            File.Delete(backupPath);
                        File.Move(LogFilePath, backupPath);
                    }
                }
                
                _initialized = true;
                
                // Write startup header
                Info("=== MicControlX Starting ===");
                Info($"Version: {GetAppVersion()}");
                Info($"OS: {GetFriendlyWindowsVersion()}");
                Info($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logger initialization failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get a friendly Windows version string (e.g., "Windows 11 Pro 23H2")
        /// </summary>
        public static string GetFriendlyWindowsVersion()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (key != null)
                {
                    var productName = key.GetValue("ProductName")?.ToString() ?? "Windows";
                    var displayVersion = key.GetValue("DisplayVersion")?.ToString() ?? "";
                    var buildNumber = key.GetValue("CurrentBuildNumber")?.ToString() ?? "";
                    
                    // Windows 11 has build 22000+
                    if (int.TryParse(buildNumber, out int build) && build >= 22000 && productName.Contains("Windows 10"))
                    {
                        productName = productName.Replace("Windows 10", "Windows 11");
                    }
                    
                    return string.IsNullOrEmpty(displayVersion) 
                        ? productName 
                        : $"{productName} {displayVersion}";
                }
            }
            catch { }
            
            return Environment.OSVersion.ToString();
        }
        
        /// <summary>
        /// Log an informational message
        /// </summary>
        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }
        
        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void Warn(string message)
        {
            WriteLog("WARN", message);
        }
        
        /// <summary>
        /// Log an error message
        /// </summary>
        public static void Error(string message)
        {
            WriteLog("ERROR", message);
        }
        
        /// <summary>
        /// Log an error with exception details
        /// </summary>
        public static void Error(string message, Exception ex)
        {
            WriteLog("ERROR", $"{message}: {ex.Message}");
        }
        
        /// <summary>
        /// Log a debug message (only in debug builds)
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Debug(string message)
        {
            WriteLog("DEBUG", message);
        }
        
        /// <summary>
        /// Get the path to the log file (for users to share)
        /// </summary>
        public static string GetLogFilePath()
        {
            return LogFilePath;
        }
        
        /// <summary>
        /// Get the path to the logs directory
        /// </summary>
        public static string GetLogDirectory()
        {
            return LogDirectory;
        }
        
        private static void WriteLog(string level, string message)
        {
            if (!_initialized) return;
            
            try
            {
                lock (_lock)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logLine = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";
                    
                    File.AppendAllText(LogFilePath, logLine, Encoding.UTF8);
                    
                    // Also write to debug output for development
                    System.Diagnostics.Debug.Write(logLine);
                }
            }
            catch
            {
                // Silently fail - logging should never crash the app
            }
        }
        
        private static string GetAppVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString(3) ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
