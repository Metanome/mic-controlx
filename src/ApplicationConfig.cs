using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using System.Windows;
using System.Media;

namespace MicControlX
{
    /// <summary>
    /// OSD visual styles with different aesthetic inspirations
    /// </summary>
    public enum OSDStyles
    {
        /// <summary>Default Style - MicControlX standard notification style</summary>
        DefaultStyle,
        /// <summary>Vantage Style - Lenovo Vantage inspired design</summary>
        VantageStyle,
        /// <summary>LLT Style - Lenovo Legion Toolkit inspired design</summary>
        LLTStyle,
        /// <summary>Translucent Style - Semi-transparent design with blur effects</summary>
        TranslucentStyle
    }

    /// <summary>
    /// OSD overlay position options for screen placement
    /// </summary>
    public enum OSDPosition
    {
        /// <summary>Top Left corner of screen</summary>
        TopLeft,
        /// <summary>Top Center of screen</summary>
        TopCenter,
        /// <summary>Top Right corner of screen</summary>
        TopRight,
        /// <summary>Middle Center of screen</summary>
        MiddleCenter,
        /// <summary>Bottom Left corner of screen</summary>
        BottomLeft,
        /// <summary>Bottom Center of screen (default)</summary>
        BottomCenter,
        /// <summary>Bottom Right corner of screen</summary>
        BottomRight
    }

    /// <summary>
    /// Theme modes for application UI
    /// </summary>
    public enum AppTheme
    {
        /// <summary>Follow system theme</summary>
        System,
        /// <summary>Always use dark theme</summary>
        Dark,
        /// <summary>Always use light theme</summary>
        Light
    }

    /// <summary>
    /// Enhanced configuration settings for the MicControlX application
    /// Universal Windows microphone control utility with multiple visual styles
    /// </summary>
    public class ApplicationConfig
    {
        public int HotKeyVirtualKey { get; set; } = 0x7A; // F11 by default
        public string HotKeyDisplayName { get; set; } = "F11";
        public bool ShowOSD { get; set; } = true;
        public bool ShowNotifications { get; set; } = false;
        [JsonIgnore]
        public int OSDDisplayTime { get; set; } = 2000; // milliseconds
        
        /// <summary>OSD visual style preference</summary>
        public OSDStyles OSDStyle { get; set; } = OSDStyles.DefaultStyle;
        
        /// <summary>OSD overlay position on screen</summary>
        public OSDPosition OSDPosition { get; set; } = OSDPosition.BottomCenter;
        
        /// <summary>OSD display duration in seconds (1-10)</summary>
        public double OSDDurationSeconds { get; set; } = 2.0;
        
        /// <summary>Application UI theme (Dark/Light/System)</summary>
        public AppTheme Theme { get; set; } = AppTheme.System;
        
        /// <summary>Auto-start with Windows</summary>
        public bool AutoStart { get; set; } = false;
        
        /// <summary>Enable sound feedback on mute/unmute</summary>
        public bool EnableSoundFeedback { get; set; } = false;
        
        /// <summary>Respect Windows Focus Assist (Do Not Disturb) to suppress OSD when active</summary>
        public bool RespectFocusAssist { get; set; } = false;
        
        /// <summary>Application language (auto, en, tr)</summary>
        public string Language { get; set; } = "auto";
        
        /// <summary>Detected system brand (Dell, HP, Acer, Lenovo, etc.)</summary>
        [JsonIgnore]
        public string DetectedBrand { get; set; } = Strings.Unknown;
        
        /// <summary>Detected system model</summary>
        [JsonIgnore]
        public string DetectedModel { get; set; } = Strings.Unknown;
        
        /// <summary>
        /// Get a user-friendly description of the detected system
        /// </summary>
        public string GetSystemDescription()
        {
            if (DetectedBrand == Strings.Unknown || DetectedModel == Strings.Unknown)
            {
                return Strings.UnknownSystemDescription;
            }
            
            return $"{DetectedBrand} {DetectedModel}";
        }

        /// <summary>
        /// Get recommended settings description based on detected system
        /// </summary>
        public string GetRecommendedSettingsDescription()
        {
            return Strings.RecommendedSettingsDescription;
        }
    }

    public static class ConfigurationManager
    {
        private static readonly string ConfigFilePath = GetConfigFilePath();

        /// <summary>
        /// Get configuration file path
        /// </summary>
        private static string GetConfigFilePath()
        {
            // Use AppData for reliable, user-specific configuration storage
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MicControlX",
                "config.json"
            );
            return appDataPath;
        }

        /// <summary>
        /// Load configuration with automatic system detection
        /// </summary>
        public static ApplicationConfig LoadConfiguration()
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var config = JsonSerializer.Deserialize<ApplicationConfig>(json);
                        if (config != null)
                        {
                            // Successfully loaded existing config, just detect dynamic properties
                            DetectSystemProperties(config);
                            return config;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log the error and proceed to create a new config
                    System.Diagnostics.Debug.WriteLine($"ConfigManager: Failed to load config - {ex.Message}");
                }
            }
            
            // If file doesn't exist, is empty, or is corrupt, create a new one.
            return CreateNewConfiguration();
        }

        /// <summary>
        /// Creates a new configuration, applies default settings, and saves it.
        /// </summary>
        private static ApplicationConfig CreateNewConfiguration()
        {
            var newConfig = new ApplicationConfig();
            DetectSystemProperties(newConfig); // Detect properties first
            ApplyDefaultSettings(newConfig);   // Then apply defaults based on detection
            SaveConfiguration(newConfig);      // Save the new config
            return newConfig;
        }

        /// <summary>
        /// Detects basic system information for display purposes only.
        /// Also syncs AutoStart setting with registry to prevent mismatches.
        /// This should be run every time the application starts.
        /// </summary>
        private static void DetectSystemProperties(ApplicationConfig config)
        {
            try
            {
                // Detect system info
                config.DetectedBrand = GetSystemInfo("SystemManufacturer") ?? Strings.Unknown;
                config.DetectedModel = GetSystemInfo("SystemProductName") ?? Strings.Unknown;
                
                // Sync AutoStart setting with registry to prevent mismatches
                bool registryAutoStart = StartupManager.IsAutoStartEnabled();
                if (config.AutoStart != registryAutoStart)
                {
                    System.Diagnostics.Debug.WriteLine($"ConfigManager: AutoStart mismatch - Config: {config.AutoStart}, Registry: {registryAutoStart}. Syncing to registry value.");
                    config.AutoStart = registryAutoStart;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ConfigManager: Error detecting system info - {ex.Message}");
                // Fallback to safe defaults
                config.DetectedBrand = Strings.Unknown;
                config.DetectedModel = Strings.Unknown;
            }
        }

        /// <summary>
        /// Applies default settings to a new configuration.
        /// This should only be run when creating a new config file.
        /// </summary>
        private static void ApplyDefaultSettings(ApplicationConfig config)
        {
            // Always use Default Style as the universal choice
            config.OSDStyle = OSDStyles.DefaultStyle;
            config.OSDPosition = OSDPosition.BottomCenter;
            config.OSDDurationSeconds = 2.0;
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
                System.Diagnostics.Debug.WriteLine($"ConfigManager: Failed to save configuration - {ex.Message}");
                MessageBox.Show(
                    string.Format(Strings.ErrorConfigSaveMessage, ex.Message),
                    Strings.ErrorConfigSaveTitle, 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
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

            /// <summary>
            /// Get information about common conflicts for specific function keys
            /// </summary>
            public static string GetKeyConflictInfo(int virtualKey)
            {
                return virtualKey switch
                {
                    VK_F1 => "F1 is commonly used by applications for Help dialogs",
                    VK_F2 => "F2 is often used for renaming files in Windows Explorer",
                    VK_F3 => "F3 is commonly used for search functions",
                    VK_F4 => "F4 is used by Windows (Alt+F4 to close windows)",
                    VK_F5 => "F5 is commonly used for refresh/reload in browsers and applications",
                    VK_F6 => "F6 is used for navigation between UI elements",
                    VK_F7 => "F7 is used by some applications for spell checking",
                    VK_F8 => "F8 is reserved by Windows for Safe Mode boot and may conflict with system functions",
                    VK_F9 => "F9 is relatively safe for global hotkeys",
                    VK_F10 => "F10 is relatively safe for global hotkeys",
                    VK_F11 => "F11 is commonly used for fullscreen mode but generally works for global hotkeys",
                    VK_F12 => "F12 is heavily used by developer tools and debuggers - high conflict potential",
                    _ when virtualKey >= VK_F13 && virtualKey <= VK_F24 => "Higher function keys (F13-F24) are usually safe but may not exist on all keyboards",
                    _ => "Unknown key conflict information"
                };
            }

            /// <summary>
            /// Get suggested alternative keys when a hotkey fails to register
            /// </summary>
            public static string[] GetSuggestedAlternatives(int failedKey)
            {
                // Recommend generally safer function keys
                return failedKey switch
                {
                    VK_F1 or VK_F2 or VK_F3 or VK_F4 or VK_F5 => new[] { "F9", "F10", "F11" },
                    VK_F8 or VK_F12 => new[] { "F9", "F10", "F11", "F7" },
                    _ => new[] { "F9", "F10", "F11" }
                };
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
                            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName 
                                         ?? Path.Combine(AppContext.BaseDirectory, "MicControlX.exe");
                            var registryValue = $"\"{exePath}\" --minimized";
                            key.SetValue(AppName, registryValue);
                        }
                        else
                        {
                            key.DeleteValue(AppName, false);
                        }
                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"StartupManager: Could not open registry key");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StartupManager: Registry operation failed - {ex.Message}");
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
                        bool isEnabled = !string.IsNullOrEmpty(value);
                        return isEnabled;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"StartupManager: Could not open registry key for reading");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StartupManager: Registry check failed - {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Sync config AutoStart setting with registry state and save if needed
        /// </summary>
        public static void SyncAutoStartSetting(ApplicationConfig config)
        {
            try
            {
                bool registryEnabled = IsAutoStartEnabled();
                if (config.AutoStart != registryEnabled)
                {
                    System.Diagnostics.Debug.WriteLine($"AutoStart mismatch detected. Config: {config.AutoStart}, Registry: {registryEnabled}");
                    config.AutoStart = registryEnabled;
                    ConfigurationManager.SaveConfiguration(config);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutoStart sync error: {ex.Message}");
            }
        }
    }
}
