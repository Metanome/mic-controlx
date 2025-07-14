using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MicControlX
{
    /// <summary>
    /// Settings Window - Logic and Configuration Management
    /// UI components are defined in SettingsWindow_UI.cs
    /// </summary>
    public partial class SettingsWindow : Form
    {
        public ApplicationConfig Configuration { get; private set; }

        public SettingsWindow(ApplicationConfig currentConfig)
        {
            Configuration = currentConfig; // Work on the provided config object
            
            InitializeComponent();
            LoadCurrentSettings();
            
            // Apply initial theme using ThemeManager
            ThemeManager.ApplyTheme(this, Configuration.AppUITheme);
        }

        private void LoadCurrentSettings()
        {
            // Add event handlers after loading to prevent triggering during initialization
            hotkeyComboBox.SelectedIndexChanged += HotkeyComboBox_SelectedIndexChanged;
            osdNotificationRadio.CheckedChanged += OsdNotificationRadio_CheckedChanged;
            trayNotificationRadio.CheckedChanged += TrayNotificationRadio_CheckedChanged;
            
            // Select current hotkey
            for (int i = 0; i < hotkeyComboBox.Items.Count; i++)
            {
                if (hotkeyComboBox.Items[i] is ComboBoxItem item && item.VirtualKey == Configuration.HotKeyVirtualKey)
                {
                    hotkeyComboBox.SelectedIndex = i;
                    break;
                }
            }
            
            // Select current notification preference and OSD style
            if (Configuration.ShowOSD)
            {
                osdNotificationRadio.Checked = true;
                LoadOsdStyleSettings();
            }
            else
            {
                trayNotificationRadio.Checked = true;
            }
            
            // Select current app theme
            appThemeComboBox.SelectedIndex = Configuration.AppUITheme switch
            {
                AppTheme.Dark => 1,
                AppTheme.Light => 2,
                _ => 0 // System
            };
            
            // Load new settings
            autoStartCheckBox.Checked = StartupManager.IsAutoStartEnabled(); // Check actual registry state
            soundFeedbackCheckBox.Checked = Configuration.EnableSoundFeedback;
            
            // Update visibility based on current settings
            UpdateOSDControlsVisibility();
        }

        private void LoadOsdStyleSettings()
        {
            // Map current OSD style to combo box index based on available options
            if (Configuration.EnableLenovoFeatures && Configuration.DetectedBrand.ToUpper().Contains("LENOVO"))
            {
                // Find the index based on what's actually available in the dropdown
                var targetStyle = Configuration.OSDStyle;
                
                for (int i = 0; i < osdStyleComboBox.Items.Count; i++)
                {
                    var itemText = osdStyleComboBox.Items[i]?.ToString() ?? "";
                    bool isMatch = (targetStyle, itemText) switch
                    {
                        (LenovoOSDStyle.WindowsDefault, "Default") => true,
                        (LenovoOSDStyle.LenovoStyle, "Lenovo Vantage") => true,
                        (LenovoOSDStyle.LegionStyle, "Legion Toolkit") => true,
                        _ => false
                    };
                    
                    if (isMatch)
                    {
                        osdStyleComboBox.SelectedIndex = i;
                        return;
                    }
                }
                
                // Fallback to Default if current style is not available
                osdStyleComboBox.SelectedIndex = 0;
            }
            else
            {
                // Non-Lenovo system - only Default available
                osdStyleComboBox.SelectedIndex = 0;
            }
        }

        // Event Handlers
        private void HotkeyComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (hotkeyComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                Configuration.HotKeyVirtualKey = selectedItem.VirtualKey;
                Configuration.HotKeyDisplayName = selectedItem.DisplayName;
                // Changes are no longer saved in real-time.
            }
        }

        private void OsdStyleComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Map combo box selection to enum based on the actual text
            if (osdStyleComboBox.SelectedItem != null)
            {
                var selectedText = osdStyleComboBox.SelectedItem.ToString() ?? "";
                
                Configuration.OSDStyle = selectedText switch
                {
                    "Lenovo Vantage" => LenovoOSDStyle.LenovoStyle,
                    "Legion Toolkit" => LenovoOSDStyle.LegionStyle,
                    _ => LenovoOSDStyle.WindowsDefault // Default or any other case
                };
                
                // Changes are no longer saved in real-time.
            }
        }

        private void TrayNotificationRadio_CheckedChanged(object? sender, EventArgs e)
        {
            if (trayNotificationRadio.Checked)
            {
                Configuration.ShowOSD = false;
                Configuration.ShowNotifications = true;
                UpdateOSDControlsVisibility();
                // Changes are no longer saved in real-time.
            }
        }

        private void AppThemeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Apply theme immediately when changed using ThemeManager
            var selectedTheme = appThemeComboBox.SelectedIndex switch
            {
                1 => AppTheme.Dark,   // Dark
                2 => AppTheme.Light,  // Light
                _ => AppTheme.System  // System
            };
            
            Configuration.AppUITheme = selectedTheme;
            ThemeManager.ApplyTheme(this, selectedTheme);
            // Changes are no longer saved in real-time.
        }
        
        private void OsdNotificationRadio_CheckedChanged(object? sender, EventArgs e)
        {
            if (osdNotificationRadio.Checked)
            {
                Configuration.ShowOSD = true;
                Configuration.ShowNotifications = false;
                UpdateOSDControlsVisibility();
                // Changes are no longer saved in real-time.
            }
        }

        private void UpdateOSDControlsVisibility()
        {
            // Show/hide OSD-related controls based on radio button selection
            bool showOSDControls = osdNotificationRadio.Checked;
            
            UpdateOsdStyleVisibility(showOSDControls);
            
            // Adjust display group height to prevent gap
            var displayGroup = this.Controls.OfType<GroupBox>().FirstOrDefault(g => g.Text == "Notification Type");
            if (displayGroup != null)
            {
                if (showOSDControls)
                {
                    displayGroup.Height = 90; // Show all controls (reduced since no preview button)
                }
                else
                {
                    displayGroup.Height = 55;  // Hide OSD controls, no gap
                }
            }
            
            // Adjust Application Settings group position
            var appGroup = this.Controls.OfType<GroupBox>().FirstOrDefault(g => g.Text == "Application Settings");
            if (appGroup != null)
            {
                appGroup.Location = new Point(20, showOSDControls ? 225 : 190);
            }
        }

        private void okButton_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void cancelButton_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void AutoStartCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            Configuration.AutoStart = autoStartCheckBox.Checked;
            
            // Immediately apply startup setting
            try
            {
                bool success = StartupManager.SetAutoStart(Configuration.AutoStart);
                if (!success)
                {
                    MessageBox.Show("Failed to update Windows startup setting. Please check permissions.", 
                                   "Startup Setting", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // Revert checkbox if failed
                    autoStartCheckBox.Checked = !autoStartCheckBox.Checked;
                    Configuration.AutoStart = autoStartCheckBox.Checked;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating startup setting: {ex.Message}", 
                               "Startup Setting Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Revert checkbox if failed
                autoStartCheckBox.Checked = !autoStartCheckBox.Checked;
                Configuration.AutoStart = autoStartCheckBox.Checked;
            }
            
            // Changes are no longer saved in real-time.
        }

        private void SoundFeedbackCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            Configuration.EnableSoundFeedback = soundFeedbackCheckBox.Checked;
            // Changes are no longer saved in real-time.
        }

        private class ComboBoxItem
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
    }
}
