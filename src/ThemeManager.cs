using System;
using System.Windows;
using Wpf.Ui.Appearance;

namespace MicControlX
{
    /// <summary>
    /// Centralized theme management for WPF UI with modern styling
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>
        /// Apply theme to the application
        /// </summary>
        public static void ApplyTheme(AppTheme theme)
        {
            bool isDark = theme switch
            {
                AppTheme.Dark => true,
                AppTheme.Light => false,
                AppTheme.System => IsSystemDarkTheme(),
                _ => IsSystemDarkTheme()
            };
            
            // Apply WPF-UI theme
            ApplicationThemeManager.Apply(isDark ? ApplicationTheme.Dark : ApplicationTheme.Light);
        }
        
        /// <summary>
        /// Determine if dark mode should be used based on system theme
        /// </summary>
        public static bool ShouldUseDarkMode()
        {
            return IsSystemDarkTheme();
        }
        
        /// <summary>
        /// Get the current theme that should be applied
        /// </summary>
        public static bool GetEffectiveTheme(AppTheme configuredTheme)
        {
            return configuredTheme switch
            {
                AppTheme.Dark => true,
                AppTheme.Light => false,
                AppTheme.System => IsSystemDarkTheme(),
                _ => IsSystemDarkTheme()
            };
        }
        
        private static bool IsSystemDarkTheme()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int intValue && intValue == 0;
            }
            catch
            {
                return false; // Default to light if detection fails
            }
        }
    }
}
