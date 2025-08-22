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
                
                // Load settings with error handling
                LoadCurrentSettings();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"SettingsWindow initialization error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "Settings Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                PopulateAppThemeComboBox();
                
                // Set checkbox states
                if (AutoStartCheckBox != null)
                    AutoStartCheckBox.IsChecked = Configuration.AutoStart;
                if (SoundFeedbackCheckBox != null)
                    SoundFeedbackCheckBox.IsChecked = Configuration.EnableSoundFeedback;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"LoadCurrentSettings error: {ex.Message}", "Settings Error", 
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

        private void PopulateAppThemeComboBox()
        {
            if (AppThemeComboBox == null) return;
            
            AppThemeComboBox.Items.Clear();
            
            // Add theme options
            foreach (AppTheme theme in Enum.GetValues<AppTheme>())
            {
                AppThemeComboBox.Items.Add(new ThemeItem(GetThemeDisplayName(theme), theme));
            }
            
            // Select current theme
            for (int i = 0; i < AppThemeComboBox.Items.Count; i++)
            {
                if (AppThemeComboBox.Items[i] is ThemeItem item && item.Theme == Configuration.Theme)
                {
                    AppThemeComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private string GetOsdStyleDisplayName(OSDStyles style)
        {
            return style switch
            {
                OSDStyles.WindowsDefault => "Windows Default",
                OSDStyles.VantageStyle => "Lenovo Vantage Style",
                OSDStyles.LLTStyle => "Lenovo Legion Toolkit Style",
                _ => style.ToString()
            };
        }

        private string GetThemeDisplayName(AppTheme theme)
        {
            return theme switch
            {
                AppTheme.Light => "Light",
                AppTheme.Dark => "Dark",
                AppTheme.System => "System Default",
                _ => theme.ToString()
            };
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
                if (AppThemeComboBox.SelectedItem is ThemeItem themeItem)
                {
                    if (Configuration.Theme != themeItem.Theme)
                    {
                        Configuration.Theme = themeItem.Theme;
                        // Apply theme immediately
                        ThemeManager.ApplyTheme(themeItem.Theme);
                        hasChanges = true;
                    }
                }

                // Apply other settings
                bool newAutoStart = AutoStartCheckBox.IsChecked == true;
                bool newSoundFeedback = SoundFeedbackCheckBox.IsChecked == true;
                
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
                    
                    string startupAction = Configuration.AutoStart ? "enable" : "disable";
                    System.Windows.MessageBox.Show(
                        $"Warning: Failed to {startupAction} Windows startup setting.\n\n" +
                        $"Error: {startupEx.Message}\n\n" +
                        $"This may be due to:\n" +
                        $"• Insufficient registry permissions\n" +
                        $"• Antivirus software blocking registry changes\n" +
                        $"• Group policy restrictions\n\n" +
                        $"Your other settings have been saved successfully.",
                        "Startup Setting Failed",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }

                // Set flag to indicate settings were saved successfully
                SettingsWereSaved = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Exception in OkButton_Click: {ex.Message}\n\nStack trace: {ex.StackTrace}", "Settings Error", System.Windows.MessageBoxButton.OK);
                // Don't set SettingsWereSaved to true if there was an error
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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
}
