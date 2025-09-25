using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Linq;

namespace MicControlX
{
    /// <summary>
    /// Provides bindable access to localized strings that update when language changes
    /// </summary>
    public class LocalizedStrings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // Expose all the strings as properties for binding
        public string AppTitle => Strings.AppTitle;
        public string Settings => Strings.Settings;
        public string About => Strings.About;
        public string GlobalHotkey => Strings.GlobalHotkey;
        public string HotkeyInstruction => Strings.HotkeyInstruction;
        public string FeaturesCompatibility => Strings.FeaturesCompatibility;
        public string FeatureUniversal => Strings.FeatureUniversal;
        public string FeatureAnyDevice => Strings.FeatureAnyDevice;
        public string AlternativeControls => Strings.AlternativeControls;
        public string ControlSingleClick => Strings.ControlSingleClick;
        public string ControlDoubleClick => Strings.ControlDoubleClick;
        public string ControlRightClick => Strings.ControlRightClick;
        public string MicrophoneStatus => Strings.MicrophoneStatus;
        public string Active => Strings.Active;
        public string SystemInformation => Strings.SystemInformation;
        public string Brand => Strings.Brand;
        public string UnknownSystem => Strings.UnknownSystem;
        public string Platform => Strings.Platform;
        public string MicrophoneDevice => Strings.MicrophoneDevice;
        public string Unknown => Strings.Unknown;
        
        // OSD Overlay strings
        public string Microphone => Strings.Microphone;
        
        // Settings window strings
        public string SettingsTitle => Strings.SettingsTitle;
        public string HotkeySettings => Strings.HotkeySettings;
        public string GlobalHotkeyLabel => Strings.GlobalHotkeyLabel;
        public string NotificationType => Strings.NotificationType;
        public string ShowOSDOverlay => Strings.ShowOSDOverlay;
        public string ShowTrayNotifications => Strings.ShowTrayNotifications;
        public string OSDStyleLabel => Strings.OSDStyleLabel;
        public string OSDPositionLabel => Strings.OSDPositionLabel;
        public string OSDDurationLabel => Strings.OSDDurationLabel;
        public string ApplicationSettings => Strings.ApplicationSettings;
        public string LanguageLabel => Strings.LanguageLabel;
        public string LanguageAuto => Strings.LanguageAuto;
        public string LanguageEnglish => Strings.LanguageEnglish;
        public string LanguageTurkish => Strings.LanguageTurkish;
        public string LanguageGerman => Strings.LanguageGerman;
        public string ThemeLabel => Strings.ThemeLabel;
        public string StartWithWindows => Strings.StartWithWindows;
        public string EnableSoundFeedback => Strings.EnableSoundFeedback;
        public string RespectFocusAssist => Strings.RespectFocusAssist;
        public string OK => Strings.OK;
        public string Cancel => Strings.Cancel;
        
        // About window strings
        public string AboutTitle => Strings.AboutTitle;
        public string Version => Strings.Version;
        public string AppDescription => Strings.AppDescription;
        public string Features => Strings.Features;
        public string FeatureGlobalHotkey => Strings.FeatureGlobalHotkey;
        public string FeatureSystemTray => Strings.FeatureSystemTray;
        public string FeatureMultipleOSD => Strings.FeatureMultipleOSD;
        public string FeatureAudioFeedback => Strings.FeatureAudioFeedback;
        public string FeatureHoldHotkey => Strings.FeatureHoldHotkey;
        public string FeatureAutoStartup => Strings.FeatureAutoStartup;
        public string AboutDescription => Strings.AboutDescription;
        public string Copyright => Strings.Copyright;
        public string CheckForUpdates => Strings.CheckForUpdates;

        /// <summary>
        /// Called when language changes to notify all properties have changed
        /// </summary>
        public void OnLanguageChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }

    /// <summary>
    /// Manages application localization and culture settings
    /// </summary>
    public static class LocalizationManager
    {
        private static LocalizedStrings? _instance;
        private static CultureInfo? _originalSystemCulture;
        
        /// <summary>
        /// Gets the singleton instance for binding to localized strings
        /// </summary>
        public static LocalizedStrings Instance => _instance ??= new LocalizedStrings();

        /// <summary>
        /// Event fired when language changes
        /// </summary>
        public static event EventHandler? LanguageChanged;

        /// <summary>
        /// Stores the original system culture when the app starts
        /// </summary>
        static LocalizationManager()
        {
            // Capture the original system culture before any changes
            _originalSystemCulture = CultureInfo.CurrentUICulture;
        }

        /// <summary>
        /// Initializes the application culture based on the configuration
        /// </summary>
        /// <param name="languageCode">Language code (auto, en, tr, de)</param>
        public static void Initialize(string languageCode)
        {
            CultureInfo culture;

            switch (languageCode?.ToLower())
            {
                case "en":
                    culture = new CultureInfo("en-US");
                    break;
                case "tr":
                    culture = new CultureInfo("tr-TR");
                    break;
                case "de":
                    culture = new CultureInfo("de-DE");
                    break;
                case "auto":
                default:
                    // Use the original system culture that was captured when the app started
                    var systemCulture = _originalSystemCulture ?? CultureInfo.InstalledUICulture;
                    var systemLanguage = systemCulture.TwoLetterISOLanguageName;
                    
                    // Check if the system language is supported
                    switch (systemLanguage)
                    {
                        case "tr":
                            culture = new CultureInfo("tr-TR");
                            break;
                        case "de":
                            culture = new CultureInfo("de-DE");
                            break;
                        case "en":
                            culture = new CultureInfo("en-US");
                            break;
                        default:
                            // For unsupported languages (fr, es, etc.), default to English
                            // This provides the best user experience since English is widely understood
                            culture = new CultureInfo("en-US");
                            break;
                    }
                    break;
            }

            // Set both UI and regular culture
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;

            // Set for the application domain
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
        }

        /// <summary>
        /// Sets the application language
        /// </summary>
        /// <param name="languageCode">Language code (auto, en, tr, de)</param>
        public static void SetLanguage(string languageCode)
        {
            Initialize(languageCode);
            
            // Update the instance to notify bindings
            Instance.OnLanguageChanged();
            
            // Notify that language has changed
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Gets the current culture display name for UI
        /// </summary>
        public static string GetCurrentLanguageDisplayName()
        {
            var culture = Thread.CurrentThread.CurrentUICulture;
            return culture.TwoLetterISOLanguageName switch
            {
                "tr" => "Türkçe",
                "de" => "Deutsch",
                "en" => "English",
                _ => "English"
            };
        }

        /// <summary>
        /// Gets available language codes
        /// </summary>
        public static string[] GetAvailableLanguages()
        {
            return new[] { "auto", "en", "tr", "de" };
        }

        /// <summary>
        /// Gets display names for available languages
        /// </summary>
        public static string[] GetLanguageDisplayNames()
        {
            return new[] { 
                Strings.LanguageAuto, 
                Strings.LanguageEnglish, 
                Strings.LanguageTurkish,
                Strings.LanguageGerman
            };
        }

        /// <summary>
        /// Gets information about what language "Auto" would select
        /// </summary>
        public static string GetAutoLanguageInfo()
        {
            var systemCulture = _originalSystemCulture ?? CultureInfo.InstalledUICulture;
            var systemLanguage = systemCulture.TwoLetterISOLanguageName;
            
            return systemLanguage switch
            {
                "tr" => "Auto (Turkish - System)",
                "de" => "Auto (German - System)",
                "en" => "Auto (English - System)", 
                _ => $"Auto (English - Default for {systemCulture.DisplayName})"
            };
        }
    }
}
