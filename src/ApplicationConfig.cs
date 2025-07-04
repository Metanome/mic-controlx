using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace MicControlX
{
    /// <summary>
    /// Enhanced configuration settings for the MicControlX application
    /// Optimized for Lenovo gaming laptops (Legion, LOQ, IdeaPad) with universal Windows compatibility
    /// Works seamlessly with Lenovo Vantage, Legion Toolkit, Windows settings, and hardware Fn keys
    /// </summary>
    public class ApplicationConfig
    {
        public int HotKeyVirtualKey { get; set; } = 0x7A; // F11 by default
        public string HotKeyDisplayName { get; set; } = "F11";
        public bool ShowOSD { get; set; } = true;
        public bool ShowNotifications { get; set; } = false;
        public int OSDDisplayTime { get; set; } = 2000; // milliseconds
        
        /// <summary>OSD style specifically for Lenovo gaming laptops</summary>
        public LenovoOSDStyle OSDStyle { get; set; } = LenovoOSDStyle.WindowsDefault;
        
        /// <summary>Application UI theme (Dark/Light/System) - does not affect OSD overlay styling</summary>
        public AppTheme AppUITheme { get; set; } = AppTheme.System;
        
        /// <summary>Auto-start with Windows</summary>
        public bool AutoStart { get; set; } = false;
        
        /// <summary>Start minimized to system tray</summary>
        public bool StartMinimized { get; set; } = true;
        
        /// <summary>Enable sound feedback on mute/unmute</summary>
        public bool EnableSoundFeedback { get; set; } = false;
        
        /// <summary>Detected laptop brand (Lenovo, HP, Acer, etc.)</summary>
        public string DetectedBrand { get; set; } = "Unknown";
        
        /// <summary>Detected laptop model for Lenovo systems</summary>
        public string DetectedModel { get; set; } = "Unknown";
        
        /// <summary>Enable Lenovo-specific features (OSD styles, Legion integration)</summary>
        public bool EnableLenovoFeatures { get; set; } = true;
        
        /// <summary>Detected Lenovo Vantage installation status</summary>
        public bool HasLenovoVantage { get; set; } = false;
        
        /// <summary>Detected Legion Toolkit installation status</summary>
        public bool HasLegionToolkit { get; set; } = false;
        
        /// <summary>Application version</summary>
        public string AppVersion { get; set; } = "3.1.1-gamma";
        
        /// <summary>
        /// Get a user-friendly description of the detected system
        /// </summary>
        public string GetSystemDescription()
        {
            if (DetectedBrand == "Unknown" || DetectedModel == "Unknown")
            {
                return "Unknown System - Universal Compatibility Mode";
            }
            
            return $"{DetectedBrand} {DetectedModel}";
        }

        /// <summary>Get recommended settings description based on detected system</summary>
        public string GetRecommendedSettingsDescription()
        {
            if (!EnableLenovoFeatures)
            {
                return "Windows Default OSD recommended for universal compatibility";
            }
            
            var modelUpper = DetectedModel.ToUpper();
            if (modelUpper.Contains("LEGION"))
            {
                return "Legion-style OSD recommended for Legion gaming laptops";
            }
            else if (modelUpper.Contains("LOQ") || modelUpper.Contains("IDEAPAD"))
            {
                return "Lenovo-style OSD recommended for LOQ/IdeaPad laptops";
            }
            else if (modelUpper.Contains("THINKPAD"))
            {
                return "Lenovo-style OSD recommended for ThinkPad laptops";
            }
            else
            {
                return "Lenovo-style OSD recommended for Lenovo laptops";
            }
        }
    }

    public static class ConfigurationManager
    {
        private static readonly string ConfigFilePath = GetConfigFilePath();

        /// <summary>
        /// Get configuration file path - prefer next to executable for portability
        /// </summary>
        private static string GetConfigFilePath()
        {
            try
            {
                // Try to save config next to executable for portability
                var exeDirectory = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
                var portableConfigPath = Path.Combine(exeDirectory, "config.json");
                
                // Test if we can write to the executable directory
                try
                {
                    File.WriteAllText(portableConfigPath, "test");
                    File.Delete(portableConfigPath);
                    return portableConfigPath; // Use portable config
                }
                catch
                {
                    // Fall back to AppData if executable directory is read-only
                    var appDataPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "MicControlX",
                        "config.json"
                    );
                    return appDataPath;
                }
            }
            catch
            {
                // Final fallback to AppData
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MicControlX",
                    "config.json"
                );
            }
        }

        /// <summary>
        /// Load configuration with automatic system detection
        /// </summary>
        public static ApplicationConfig LoadConfiguration()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<ApplicationConfig>(json) ?? new ApplicationConfig();
                    DetectAndConfigureSystem(config);
                    return config;
                }
            }
            catch (Exception ex)
            {
                // If config is corrupted, create new one
                System.Diagnostics.Debug.WriteLine($"Config load error: {ex.Message}");
            }
            
            var newConfig = new ApplicationConfig();
            DetectAndConfigureSystem(newConfig);
            SaveConfiguration(newConfig);
            return newConfig;
        }

        /// <summary>
        /// Detect system information and configure Lenovo-specific features
        /// </summary>
        private static void DetectAndConfigureSystem(ApplicationConfig config)
        {
            try
            {
                // Detect system manufacturer and model using registry
                config.DetectedBrand = GetSystemInfo("SystemManufacturer") ?? "Unknown";
                config.DetectedModel = GetSystemInfo("SystemProductName") ?? "Unknown";
                
                // Enable Lenovo-specific features for Lenovo systems
                if (config.DetectedBrand.ToUpper().Contains("LENOVO"))
                {
                    config.EnableLenovoFeatures = true;
                    
                    // Detect Lenovo software installations
                    config.HasLenovoVantage = IsLenovoVantageInstalled();
                    config.HasLegionToolkit = IsLegionToolkitInstalled();
                    
                    // Configure OSD style based on detected model and available software
                    var modelUpper = config.DetectedModel.ToUpper();
                    if (modelUpper.Contains("LEGION") && config.HasLegionToolkit)
                    {
                        config.OSDStyle = LenovoOSDStyle.LegionStyle;
                    }
                    else if (config.HasLenovoVantage)
                    {
                        config.OSDStyle = LenovoOSDStyle.LenovoStyle;
                    }
                    else
                    {
                        config.OSDStyle = LenovoOSDStyle.WindowsDefault;
                    }
                }
                else
                {
                    // Non-Lenovo systems
                    config.EnableLenovoFeatures = false;
                    config.HasLenovoVantage = false;
                    config.HasLegionToolkit = false;
                    config.OSDStyle = LenovoOSDStyle.WindowsDefault;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error detecting system info: {ex.Message}");
                // Fallback to safe defaults
                config.EnableLenovoFeatures = false;
                config.OSDStyle = LenovoOSDStyle.WindowsDefault;
                config.DetectedBrand = "Unknown";
                config.DetectedModel = "Unknown";
            }
        }

        private static string? GetSystemInfo(string keyName)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
                return key?.GetValue(keyName)?.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static void SaveConfiguration(ApplicationConfig config)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath)!);
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save configuration: {ex.Message}", 
                               "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public static class VirtualKeys
        {
            public const int VK_F1 = 0x70;
            public const int VK_F2 = 0x71;
            public const int VK_F3 = 0x72;
            public const int VK_F4 = 0x73;
            public const int VK_F5 = 0x74;
            public const int VK_F6 = 0x75;
            public const int VK_F7 = 0x76;
            public const int VK_F8 = 0x77;
            public const int VK_F9 = 0x78;
            public const int VK_F10 = 0x79;
            public const int VK_F11 = 0x7A;
            public const int VK_F12 = 0x7B;
            public const int VK_F13 = 0x7C;
            public const int VK_F14 = 0x7D;
            public const int VK_F15 = 0x7E;
            public const int VK_F16 = 0x7F;
            public const int VK_F17 = 0x80;
            public const int VK_F18 = 0x81;
            public const int VK_F19 = 0x82;
            public const int VK_F20 = 0x83;
            public const int VK_F21 = 0x84;
            public const int VK_F22 = 0x85;
            public const int VK_F23 = 0x86;
            public const int VK_F24 = 0x87;

            public static string GetKeyName(int virtualKey)
            {
                return virtualKey switch
                {
                    VK_F1 => "F1", VK_F2 => "F2", VK_F3 => "F3", VK_F4 => "F4",
                    VK_F5 => "F5", VK_F6 => "F6", VK_F7 => "F7", VK_F8 => "F8",
                    VK_F9 => "F9", VK_F10 => "F10", VK_F11 => "F11", VK_F12 => "F12",
                    VK_F13 => "F13", VK_F14 => "F14", VK_F15 => "F15", VK_F16 => "F16",
                    VK_F17 => "F17", VK_F18 => "F18", VK_F19 => "F19", VK_F20 => "F20",
                    VK_F21 => "F21", VK_F22 => "F22", VK_F23 => "F23", VK_F24 => "F24",
                    _ => $"Key{virtualKey:X}"
                };
            }
        }

        /// <summary>
        /// Detect if Lenovo Vantage is installed on the system
        /// </summary>
        private static bool IsLenovoVantageInstalled()
        {
            try
            {
                // Check for Lenovo Vantage process
                var vantageProcesses = System.Diagnostics.Process.GetProcessesByName("LenovoVantage");
                if (vantageProcesses.Length > 0)
                {
                    return true;
                }
                
                // Check for Lenovo Vantage service
                var vantageService = System.Diagnostics.Process.GetProcessesByName("LenovoVantageService");
                if (vantageService.Length > 0)
                {
                    return true;
                }
                
                // Check registry for Lenovo Vantage installation
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                var displayName = subKey?.GetValue("DisplayName")?.ToString();
                                if (displayName != null && displayName.Contains("Lenovo Vantage"))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error detecting Lenovo Vantage: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Detect if Lenovo Legion Toolkit (LLT) is installed on the system
        /// </summary>
        private static bool IsLegionToolkitInstalled()
        {
            try
            {
                // Check for Legion Toolkit process
                var lltProcesses = System.Diagnostics.Process.GetProcessesByName("Lenovo.Legion.Toolkit");
                if (lltProcesses.Length > 0)
                {
                    return true;
                }
                
                // Check for LLT service
                var lltService = System.Diagnostics.Process.GetProcessesByName("LegionService");
                if (lltService.Length > 0)
                {
                    return true;
                }
                
                // Check registry for Legion Toolkit installation
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                var displayName = subKey?.GetValue("DisplayName")?.ToString();
                                if (displayName != null && (displayName.Contains("Legion Toolkit") || displayName.Contains("Lenovo Legion Toolkit")))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error detecting Legion Toolkit: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Manages Windows startup registry entries for the application
    /// </summary>
    public static class StartupManager
    {
        private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "MicControlX";

        /// <summary>
        /// Enable or disable auto-start with Windows
        /// </summary>
        public static bool SetAutoStart(bool enable)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            // Get current executable path dynamically
                            var exePath = Application.ExecutablePath;
                            key.SetValue(AppName, $"\"{exePath}\"");
                        }
                        else
                        {
                            key.DeleteValue(AppName, false);
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Startup registry error: {ex.Message}");
                return false;
            }
            return false;
        }

        /// <summary>
        /// Check if auto-start is currently enabled
        /// </summary>
        public static bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegistryKey, false))
                {
                    if (key != null)
                    {
                        var value = key.GetValue(AppName)?.ToString();
                        return !string.IsNullOrEmpty(value);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Startup check error: {ex.Message}");
            }
            return false;
        }
    }

    /// <summary>
    /// Manages sound feedback for microphone state changes
    /// </summary>
    public static class SoundFeedback
    {
        /// <summary>
        /// Play embedded sound resource
        /// </summary>
        public static void PlayEmbeddedSound(string resourceName)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        // Use SoundPlayer for simple WAV playback from embedded resource
                        using (var player = new System.Media.SoundPlayer(stream))
                        {
                            player.Play(); // Async playback to avoid blocking
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Sound resource not found: {resourceName}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sound playback error: {ex.Message}");
                // Silent failure - don't interrupt app functionality
            }
        }

        /// <summary>
        /// Play mute sound from embedded resource
        /// </summary>
        public static void PlayMuteSound()
        {
            PlayEmbeddedSound("mic_mute.wav");
        }

        /// <summary>
        /// Play unmute sound from embedded resource
        /// </summary>
        public static void PlayUnmuteSound()
        {
            PlayEmbeddedSound("mic_unmute.wav");
        }
    }
}
