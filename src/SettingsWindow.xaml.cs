using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Wpf.Ui.Controls;

namespace MicControlX
{
    /// <summary>
    /// Settings Window - WPF version with modern styling
    /// </summary>
    public partial class SettingsWindow : FluentWindow
    {
        public ApplicationConfig Configuration { get; private set; }
        public bool SettingsWereSaved { get; private set; } = false;

        public SettingsWindow(ApplicationConfig currentConfig)
        {
            try
            {
                Configuration = currentConfig ?? throw new ArgumentNullException(nameof(currentConfig));
                
                InitializeComponent();
                
                // Subscribe to language changes
                LocalizationManager.LanguageChanged += OnLanguageChanged;
                
                // Load settings with error handling
                LoadCurrentSettings();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format(Strings.ErrorSettingsInitMessage, ex.Message, ex.StackTrace), 
                    Strings.ErrorSettingsTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // Populate hotkey combo box
                PopulateHotkeyComboBox();
                
                // Select current hotkey
                SelectCurrentHotkey();
                
                // Set notification preferences without triggering events
                if (Configuration.ShowOSD)
                {
                    if (OsdNotificationRadio != null)
                        OsdNotificationRadio.IsChecked = true;
                    PopulateOsdStyleComboBox();
                    if (OsdStylePanel != null)
                        OsdStylePanel.Visibility = Visibility.Visible;
                }
                else
                {
                    if (TrayNotificationRadio != null)
                        TrayNotificationRadio.IsChecked = true;
                    if (OsdStylePanel != null)
                        OsdStylePanel.Visibility = Visibility.Collapsed;
                }
                
                // Wire up events after initialization
                if (OsdNotificationRadio != null)
                    OsdNotificationRadio.Checked += OsdNotificationRadio_Checked;
                if (TrayNotificationRadio != null)
                    TrayNotificationRadio.Checked += TrayNotificationRadio_Checked;
                
                // Populate and select app theme
                PopulateThemeComboBox();
                
                // Set toggle switch states
                if (AutoStartToggleSwitch != null)
                    AutoStartToggleSwitch.IsChecked = Configuration.AutoStart;
                if (SoundFeedbackToggleSwitch != null)
                    SoundFeedbackToggleSwitch.IsChecked = Configuration.EnableSoundFeedback;
                
                // Populate and select language
                PopulateLanguageComboBox();
                SelectCurrentLanguage();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format(Strings.ErrorLoadSettingsMessage, ex.Message), Strings.ErrorSettingsTitle, 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }
        }

        private void PopulateHotkeyComboBox()
        {
            if (HotkeyComboBox == null) return;
            
            HotkeyComboBox.Items.Clear();
            
            // Add function keys F1-F24
            for (int i = 1; i <= 24; i++)
            {
                int vkey = ConfigurationManager.VirtualKeys.VK_F1 + (i - 1);
                HotkeyComboBox.Items.Add(new ComboBoxItem($"F{i}", vkey));
            }
        }

        private void SelectCurrentHotkey()
        {
            if (HotkeyComboBox == null) return;
            
            for (int i = 0; i < HotkeyComboBox.Items.Count; i++)
            {
                if (HotkeyComboBox.Items[i] is ComboBoxItem item && item.VirtualKey == Configuration.HotKeyVirtualKey)
                {
                    HotkeyComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private void PopulateOsdStyleComboBox()
        {
            if (OsdStyleComboBox == null) return;
            
            OsdStyleComboBox.Items.Clear();
            
            // Add OSD style options
            foreach (OSDStyles style in Enum.GetValues<OSDStyles>())
            {
                OsdStyleComboBox.Items.Add(new OsdStyleItem(GetOsdStyleDisplayName(style), style));
            }
            
            // Select current style
            for (int i = 0; i < OsdStyleComboBox.Items.Count; i++)
            {
                if (OsdStyleComboBox.Items[i] is OsdStyleItem item && item.Style == Configuration.OSDStyle)
                {
                    OsdStyleComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private void PopulateThemeComboBox()
        {
            if (ThemeComboBox == null) return;
            
            ThemeComboBox.Items.Clear();
            
            // Add theme options
            foreach (AppTheme theme in Enum.GetValues<AppTheme>())
            {
                ThemeComboBox.Items.Add(new ThemeItem(GetThemeDisplayName(theme), theme));
            }
            
            // Select current theme
            for (int i = 0; i < ThemeComboBox.Items.Count; i++)
            {
                if (ThemeComboBox.Items[i] is ThemeItem item && item.Theme == Configuration.Theme)
                {
                    ThemeComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private string GetOsdStyleDisplayName(OSDStyles style)
        {
            return style switch
            {
                OSDStyles.DefaultStyle => Strings.OSDDefaultStyle,
                OSDStyles.VantageStyle => Strings.OSDVantageStyle,
                OSDStyles.LLTStyle => Strings.OSDLLTStyle,
                _ => style.ToString()
            };
        }

        private string GetThemeDisplayName(AppTheme theme)
        {
            return theme switch
            {
                AppTheme.Light => Strings.ThemeLight,
                AppTheme.Dark => Strings.ThemeDark,
                AppTheme.System => Strings.ThemeSystem,
                _ => theme.ToString()
            };
        }

        private void PopulateLanguageComboBox()
        {
            if (LanguageComboBox == null) return;
            
            LanguageComboBox.Items.Clear();
            
            // Add language options using localized strings
            LanguageComboBox.Items.Add(new LanguageItem(GetLanguageDisplayName("auto"), "auto"));
            LanguageComboBox.Items.Add(new LanguageItem(GetLanguageDisplayName("en"), "en"));
            LanguageComboBox.Items.Add(new LanguageItem(GetLanguageDisplayName("tr"), "tr"));
        }

        private string GetLanguageDisplayName(string languageCode)
        {
            return languageCode switch
            {
                "auto" => Strings.LanguageAuto,
                "en" => Strings.LanguageEnglish,
                "tr" => Strings.LanguageTurkish,
                _ => languageCode
            };
        }

        private void SelectCurrentLanguage()
        {
            if (LanguageComboBox == null) return;
            
            // Select current language
            for (int i = 0; i < LanguageComboBox.Items.Count; i++)
            {
                if (LanguageComboBox.Items[i] is LanguageItem item && item.Code == Configuration.Language)
                {
                    LanguageComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private void OsdNotificationRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (OsdNotificationRadio?.IsChecked == true)
            {
                PopulateOsdStyleComboBox();
                if (OsdStylePanel != null)
                    OsdStylePanel.Visibility = Visibility.Visible;
            }
        }

        private void TrayNotificationRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (TrayNotificationRadio?.IsChecked == true)
            {
                if (OsdStylePanel != null)
                    OsdStylePanel.Visibility = Visibility.Collapsed;
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Language change will be applied when user clicks OK button
            // For immediate effect, you could apply it here, but it's better to apply on OK
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool hasChanges = false;

                // Apply hotkey changes
                if (HotkeyComboBox.SelectedItem is ComboBoxItem hotkeyItem)
                {
                    if (Configuration.HotKeyDisplayName != hotkeyItem.DisplayName)
                    {
                        Configuration.HotKeyVirtualKey = hotkeyItem.VirtualKey;
                        Configuration.HotKeyDisplayName = hotkeyItem.DisplayName;
                        hasChanges = true;
                    }
                }

                // Apply notification settings
                bool newShowOSD = OsdNotificationRadio.IsChecked == true;
                bool newShowNotifications = TrayNotificationRadio.IsChecked == true;
                
                if (Configuration.ShowOSD != newShowOSD)
                {
                    Configuration.ShowOSD = newShowOSD;
                    hasChanges = true;
                }
                
                if (Configuration.ShowNotifications != newShowNotifications)
                {
                    Configuration.ShowNotifications = newShowNotifications;
                    hasChanges = true;
                }

                // Apply OSD style (always save it, regardless of notification preference)
                if (OsdStyleComboBox.SelectedItem is OsdStyleItem osdStyleItem)
                {
                    if (Configuration.OSDStyle != osdStyleItem.Style)
                    {
                        Configuration.OSDStyle = osdStyleItem.Style;
                        hasChanges = true;
                    }
                }

                // Apply app theme
                if (ThemeComboBox.SelectedItem is ThemeItem themeItem)
                {
                    if (Configuration.Theme != themeItem.Theme)
                    {
                        Configuration.Theme = themeItem.Theme;
                        // Apply theme immediately
                        ThemeManager.ApplyTheme(themeItem.Theme);
                        hasChanges = true;
                    }
                }

                // Apply language setting
                if (LanguageComboBox.SelectedItem is LanguageItem languageItem)
                {
                    if (Configuration.Language != languageItem.Code)
                    {
                        Configuration.Language = languageItem.Code;
                        // Apply language immediately
                        LocalizationManager.SetLanguage(languageItem.Code);
                        hasChanges = true;
                    }
                }

                // Apply other settings
                bool newAutoStart = AutoStartToggleSwitch.IsChecked == true;
                bool newSoundFeedback = SoundFeedbackToggleSwitch.IsChecked == true;
                
                if (Configuration.AutoStart != newAutoStart)
                {
                    Configuration.AutoStart = newAutoStart;
                    hasChanges = true;
                }
                
                if (Configuration.EnableSoundFeedback != newSoundFeedback)
                {
                    Configuration.EnableSoundFeedback = newSoundFeedback;
                    hasChanges = true;
                }

                // Save configuration only if there are changes
                if (hasChanges)
                {
                    ConfigurationManager.SaveConfiguration(Configuration);
                }
                
                // Apply Windows startup setting (always check this as it might be out of sync)
                try
                {
                    StartupManager.SetAutoStart(Configuration.AutoStart);
                }
                catch (Exception startupEx)
                {
                    System.Diagnostics.Debug.WriteLine($"SettingsWindow: StartupManager failed - {startupEx.Message}");
                    
                    string startupAction = Configuration.AutoStart ? Strings.ActionEnable : Strings.ActionDisable;
                    System.Windows.MessageBox.Show(
                        string.Format(Strings.ErrorStartupSettingMessage, startupAction, startupEx.Message),
                        Strings.ErrorStartupSettingTitle,
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }

                // Set flag to indicate settings were saved successfully
                SettingsWereSaved = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format(Strings.ErrorSettingsException, ex.Message, ex.StackTrace), Strings.ErrorSettingsTitle, System.Windows.MessageBoxButton.OK);
                // Don't set SettingsWereSaved to true if there was an error
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            // The XAML bindings will automatically update when language changes
            // We need to update ComboBox items that are populated from code
            Dispatcher.Invoke(() =>
            {
                // Refresh all ComboBox items with new translations
                PopulateLanguageComboBox();
                PopulateOsdStyleComboBox();
                PopulateThemeComboBox();
                
                // Restore selections
                SelectCurrentLanguage();
                SelectCurrentOsdStyle();
                SelectCurrentTheme();
            });
        }

        private void SelectCurrentOsdStyle()
        {
            if (OsdStyleComboBox == null) return;
            
            for (int i = 0; i < OsdStyleComboBox.Items.Count; i++)
            {
                if (OsdStyleComboBox.Items[i] is OsdStyleItem item && item.Style == Configuration.OSDStyle)
                {
                    OsdStyleComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private void SelectCurrentTheme()
        {
            if (ThemeComboBox == null) return;
            
            for (int i = 0; i < ThemeComboBox.Items.Count; i++)
            {
                if (ThemeComboBox.Items[i] is ThemeItem item && item.Theme == Configuration.Theme)
                {
                    ThemeComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from language changes to prevent memory leaks
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
            base.OnClosed(e);
        }
    }

    // Helper classes for ComboBox items
    public class ComboBoxItem
    {
        public string DisplayName { get; }
        public int VirtualKey { get; }

        public ComboBoxItem(string displayName, int virtualKey)
        {
            DisplayName = displayName;
            VirtualKey = virtualKey;
        }

        public override string ToString() => DisplayName;
    }

    public class OsdStyleItem
    {
        public string DisplayName { get; }
        public OSDStyles Style { get; }

        public OsdStyleItem(string displayName, OSDStyles style)
        {
            DisplayName = displayName;
            Style = style;
        }

        public override string ToString() => DisplayName;
    }

    public class ThemeItem
    {
        public string DisplayName { get; }
        public AppTheme Theme { get; }

        public ThemeItem(string displayName, AppTheme theme)
        {
            DisplayName = displayName;
            Theme = theme;
        }

        public override string ToString() => DisplayName;
    }

    public class LanguageItem
    {
        public string DisplayName { get; }
        public string Code { get; }

        public LanguageItem(string displayName, string code)
        {
            DisplayName = displayName;
            Code = code;
        }

        public override string ToString() => DisplayName;
    }
}
