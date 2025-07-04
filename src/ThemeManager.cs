using System;
using System.Drawing;
using System.Windows.Forms;

namespace MicControlX
{
    /// <summary>
    /// Centralized theme management for consistent UI across the entire application
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>
        /// Apply theme to any form in the application
        /// </summary>
        public static void ApplyTheme(Form form, AppTheme theme)
        {
            bool isDark = theme switch
            {
                AppTheme.Dark => true,
                AppTheme.Light => false,
                AppTheme.System => IsSystemDarkTheme(),
                _ => IsSystemDarkTheme()
            };
            
            if (isDark)
            {
                ApplyDarkTheme(form);
            }
            else
            {
                ApplyLightTheme(form);
            }
        }
        
        /// <summary>
        /// Determine if dark mode should be used based on system theme
        /// </summary>
        public static bool ShouldUseDarkMode()
        {
            return IsSystemDarkTheme();
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
        
        private static void ApplyDarkTheme(Form form)
        {
            // Lenovo Legion Toolkit inspired dark theme colors
            form.BackColor = Color.FromArgb(24, 24, 24);  // Very dark background
            form.ForeColor = Color.FromArgb(240, 240, 240);  // Light text
            
            foreach (Control control in form.Controls)
            {
                ApplyDarkThemeToControl(control);
            }
        }
        
        private static void ApplyLightTheme(Form form)
        {
            form.BackColor = Color.FromArgb(248, 249, 250);
            form.ForeColor = Color.FromArgb(33, 37, 41);
            
            foreach (Control control in form.Controls)
            {
                ApplyLightThemeToControl(control);
            }
        }
        
        private static void ApplyDarkThemeToControl(Control control)
        {
            switch (control)
            {
                case GroupBox groupBox:
                    groupBox.ForeColor = Color.FromArgb(220, 220, 220);
                    break;
                case Label label:
                    if (label.Parent is Panel panel)
                    {
                        // Handle different panel types
                        if (panel.BackColor == Color.FromArgb(217, 237, 247) || panel.BackColor == Color.FromArgb(30, 50, 70))
                        {
                            // Info panel - Lenovo Legion style
                            panel.BackColor = Color.FromArgb(30, 50, 70);
                            label.ForeColor = Color.FromArgb(130, 180, 255);
                        }
                        else if (panel.BackColor == Color.FromArgb(233, 236, 239) || panel.BackColor == Color.FromArgb(35, 35, 35))
                        {
                            // System panel
                            panel.BackColor = Color.FromArgb(35, 35, 35);
                            label.ForeColor = Color.FromArgb(190, 190, 190);
                        }
                        else
                        {
                            // Regular panel
                            panel.BackColor = Color.FromArgb(28, 28, 28);
                            label.ForeColor = Color.FromArgb(220, 220, 220);
                        }
                    }
                    else
                    {
                        label.ForeColor = Color.FromArgb(220, 220, 220);
                    }
                    break;
                case ComboBox comboBox:
                    comboBox.BackColor = Color.FromArgb(40, 40, 40);
                    comboBox.ForeColor = Color.FromArgb(240, 240, 240);
                    break;
                case CheckBox checkBox:
                    checkBox.ForeColor = Color.FromArgb(220, 220, 220);
                    break;
                case Button button:
                    if (button.BackColor == Color.FromArgb(25, 135, 84) || button.Text == "Save Settings") // OK/Save button
                    {
                        button.BackColor = Color.FromArgb(35, 155, 94);
                        button.ForeColor = Color.White;
                    }
                    else if (button.BackColor == Color.FromArgb(108, 117, 125) || button.Text == "Cancel") // Cancel button
                    {
                        button.BackColor = Color.FromArgb(55, 55, 55);
                        button.ForeColor = Color.FromArgb(220, 220, 220);
                    }
                    else if (button.BackColor == Color.FromArgb(13, 110, 253) || button.Text == "Preview") // Preview button
                    {
                        button.BackColor = Color.FromArgb(25, 135, 255);
                        button.ForeColor = Color.White;
                    }
                    else
                    {
                        button.BackColor = Color.FromArgb(55, 55, 55);
                        button.ForeColor = Color.FromArgb(220, 220, 220);
                    }
                    break;
                case TextBox textBox:
                    textBox.BackColor = Color.FromArgb(40, 40, 40);
                    textBox.ForeColor = Color.FromArgb(240, 240, 240);
                    break;
                case Panel panelControl:
                    // Apply to panels that haven't been handled above
                    if (panelControl.BackColor == Color.FromArgb(248, 249, 250))
                    {
                        panelControl.BackColor = Color.FromArgb(28, 28, 28);
                    }
                    break;
            }
            
            // Recursively apply to child controls
            foreach (Control child in control.Controls)
            {
                ApplyDarkThemeToControl(child);
            }
        }
        
        private static void ApplyLightThemeToControl(Control control)
        {
            switch (control)
            {
                case GroupBox groupBox:
                    groupBox.ForeColor = Color.FromArgb(73, 80, 87);
                    break;
                case Label label:
                    if (label.Parent is Panel panel)
                    {
                        if (panel.BackColor == Color.FromArgb(30, 50, 70) || panel.BackColor == Color.FromArgb(217, 237, 247))
                        {
                            // Info panel - restore light theme
                            panel.BackColor = Color.FromArgb(217, 237, 247);
                            label.ForeColor = Color.FromArgb(13, 110, 253);
                        }
                        else if (panel.BackColor == Color.FromArgb(35, 35, 35) || panel.BackColor == Color.FromArgb(233, 236, 239))
                        {
                            // System panel
                            panel.BackColor = Color.FromArgb(233, 236, 239);
                            label.ForeColor = Color.FromArgb(108, 117, 125);
                        }
                        else
                        {
                            // Regular panel
                            panel.BackColor = Color.FromArgb(248, 249, 250);
                            label.ForeColor = Color.FromArgb(33, 37, 41);
                        }
                    }
                    else if (label.Font?.Italic == true)
                    {
                        label.ForeColor = Color.FromArgb(108, 117, 125);
                    }
                    else
                    {
                        label.ForeColor = Color.FromArgb(33, 37, 41);
                    }
                    break;
                case ComboBox comboBox:
                    comboBox.BackColor = Color.White;
                    comboBox.ForeColor = Color.Black;
                    break;
                case CheckBox checkBox:
                    checkBox.ForeColor = Color.FromArgb(33, 37, 41);
                    break;
                case Button button:
                    if (button.Text == "Save Settings")
                    {
                        button.BackColor = Color.FromArgb(25, 135, 84);
                        button.ForeColor = Color.White;
                    }
                    else if (button.Text == "Cancel")
                    {
                        button.BackColor = Color.FromArgb(108, 117, 125);
                        button.ForeColor = Color.White;
                    }
                    else if (button.Text == "Preview")
                    {
                        button.BackColor = Color.FromArgb(13, 110, 253);
                        button.ForeColor = Color.White;
                    }
                    else
                    {
                        button.BackColor = Color.FromArgb(225, 225, 225);
                        button.ForeColor = Color.FromArgb(33, 37, 41);
                    }
                    break;
                case TextBox textBox:
                    textBox.BackColor = Color.White;
                    textBox.ForeColor = Color.Black;
                    break;
                case Panel panelControl:
                    // Apply to panels that haven't been handled above
                    if (panelControl.BackColor == Color.FromArgb(28, 28, 28))
                    {
                        panelControl.BackColor = Color.FromArgb(248, 249, 250);
                    }
                    break;
            }
            
            // Recursively apply to child controls
            foreach (Control child in control.Controls)
            {
                ApplyLightThemeToControl(child);
            }
        }
    }
}
