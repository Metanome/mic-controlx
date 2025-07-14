using System;
using System.Drawing;
using System.Windows.Forms;

namespace MicControlX
{
    /// <summary>
    /// UI components and layout for the Settings Window
    /// </summary>
    public partial class SettingsWindow
    {
        private ComboBox hotkeyComboBox = null!;
        private RadioButton osdNotificationRadio = null!;
        private RadioButton trayNotificationRadio = null!;
        private ComboBox osdStyleComboBox = null!;
        private ComboBox appThemeComboBox = null!;
        private Label osdStyleLabel = null!;
        private CheckBox autoStartCheckBox = null!;
        private CheckBox soundFeedbackCheckBox = null!;

        private void InitializeComponent()
        {
            this.Text = "Settings";
            this.Size = new Size(500, 400); // Increased height for OK/Cancel buttons
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            
            CreateMainTitle();
            CreateHotkeyGroup();
            CreateDisplayGroup();
            CreateApplicationGroup();
            CreateActionButtons();
        }

        private void CreateMainTitle()
        {
            Label titleLabel = new Label();
            titleLabel.Text = "MicControlX Settings";
            titleLabel.Location = new Point(20, 15);
            titleLabel.Size = new Size(450, 25);
            titleLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(33, 37, 41);
            this.Controls.Add(titleLabel);
        }

        private void CreateHotkeyGroup()
        {
            // Hotkey settings group
            GroupBox hotkeyGroup = new GroupBox();
            hotkeyGroup.Text = "Hotkey Settings";
            hotkeyGroup.Location = new Point(20, 55);
            hotkeyGroup.Size = new Size(450, 60);
            hotkeyGroup.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            hotkeyGroup.ForeColor = Color.FromArgb(73, 80, 87);
            this.Controls.Add(hotkeyGroup);
            
            Label hotkeyLabel = new Label();
            hotkeyLabel.Text = "Global Hotkey:";
            hotkeyLabel.Location = new Point(15, 25);
            hotkeyLabel.Size = new Size(100, 20);
            hotkeyLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            hotkeyGroup.Controls.Add(hotkeyLabel);
            
            hotkeyComboBox = new ComboBox();
            hotkeyComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            hotkeyComboBox.Location = new Point(120, 23);
            hotkeyComboBox.Size = new Size(100, 25);
            
            // Add function keys to combo box
            for (int i = 1; i <= 24; i++)
            {
                int vkey = ConfigurationManager.VirtualKeys.VK_F1 + (i - 1);
                hotkeyComboBox.Items.Add(new ComboBoxItem($"F{i}", vkey));
            }
            hotkeyGroup.Controls.Add(hotkeyComboBox);
        }

        private void CreateDisplayGroup()
        {
            // Notification Type settings group (reorganized with conditional OSD style)
            GroupBox displayGroup = new GroupBox();
            displayGroup.Text = "Notification Type";
            displayGroup.Location = new Point(20, 125);
            displayGroup.Size = new Size(450, 90); // Reduced height since no preview button
            displayGroup.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            displayGroup.ForeColor = Color.FromArgb(73, 80, 87);
            this.Controls.Add(displayGroup);
            
            // Notification preference radio buttons (mutually exclusive)
            osdNotificationRadio = new RadioButton();
            osdNotificationRadio.Text = "Show OSD overlay";
            osdNotificationRadio.Location = new Point(15, 25);
            osdNotificationRadio.Size = new Size(200, 20);
            osdNotificationRadio.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            osdNotificationRadio.Checked = true; // Default to OSD
            osdNotificationRadio.CheckedChanged += OsdNotificationRadio_CheckedChanged;
            displayGroup.Controls.Add(osdNotificationRadio);
            
            trayNotificationRadio = new RadioButton();
            trayNotificationRadio.Text = "Show system notifications";
            trayNotificationRadio.Location = new Point(220, 25);
            trayNotificationRadio.Size = new Size(220, 20);
            trayNotificationRadio.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            displayGroup.Controls.Add(trayNotificationRadio);
            
            // OSD Style section (only visible when OSD is selected)
            osdStyleLabel = new Label();
            osdStyleLabel.Text = "OSD Style:";
            osdStyleLabel.Location = new Point(15, 60);
            osdStyleLabel.Size = new Size(100, 20);
            osdStyleLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            displayGroup.Controls.Add(osdStyleLabel);
            
            osdStyleComboBox = new ComboBox();
            osdStyleComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            osdStyleComboBox.Location = new Point(120, 58);
            osdStyleComboBox.Size = new Size(140, 25);
            osdStyleComboBox.SelectedIndexChanged += OsdStyleComboBox_SelectedIndexChanged;
            
            // Add style options based on system detection and installed software
            PopulateOsdStyleOptions();
            displayGroup.Controls.Add(osdStyleComboBox);
        }

        private void PopulateOsdStyleOptions()
        {
            osdStyleComboBox.Items.Clear();
            
            if (Configuration.EnableLenovoFeatures && Configuration.DetectedBrand.ToUpper().Contains("LENOVO"))
            {
                // Always add Default for Lenovo systems
                osdStyleComboBox.Items.Add("Default");
                
                // If either Vantage or LLT is installed, show both options
                // (LLT uses Vantage under the hood, so both styles work with either software)
                if (Configuration.HasLenovoVantage || Configuration.HasLegionToolkit)
                {
                    osdStyleComboBox.Items.Add("Lenovo Vantage");
                    osdStyleComboBox.Items.Add("Legion Toolkit");
                }
            }
            else
            {
                // Non-Lenovo systems: only Default (uses Windows Sound Settings)
                osdStyleComboBox.Items.Add("Default");
            }
        }

        private void CreateApplicationGroup()
        {
            // Application Settings group
            GroupBox appGroup = new GroupBox();
            appGroup.Text = "Application Settings";
            appGroup.Location = new Point(20, 225);
            appGroup.Size = new Size(450, 110); // Reduced height after removing PTT option
            appGroup.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            appGroup.ForeColor = Color.FromArgb(73, 80, 87);
            this.Controls.Add(appGroup);
            
            Label appThemeLabel = new Label();
            appThemeLabel.Text = "App Theme:";
            appThemeLabel.Location = new Point(15, 25);
            appThemeLabel.Size = new Size(80, 20);
            appThemeLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            appGroup.Controls.Add(appThemeLabel);
            
            appThemeComboBox = new ComboBox();
            appThemeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            appThemeComboBox.Location = new Point(100, 23);
            appThemeComboBox.Size = new Size(100, 25);
            appThemeComboBox.Items.AddRange(new object[] { "System", "Dark", "Light" });
            appThemeComboBox.SelectedIndexChanged += AppThemeComboBox_SelectedIndexChanged;
            appGroup.Controls.Add(appThemeComboBox);
            
            // Auto-start checkbox
            autoStartCheckBox = new CheckBox();
            autoStartCheckBox.Text = "Start with Windows";
            autoStartCheckBox.Location = new Point(15, 55);
            autoStartCheckBox.Size = new Size(200, 20);
            autoStartCheckBox.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            autoStartCheckBox.CheckedChanged += AutoStartCheckBox_CheckedChanged;
            appGroup.Controls.Add(autoStartCheckBox);
            
            // Sound feedback checkbox
            soundFeedbackCheckBox = new CheckBox();
            soundFeedbackCheckBox.Text = "Enable sound feedback";
            soundFeedbackCheckBox.Location = new Point(15, 80);
            soundFeedbackCheckBox.Size = new Size(400, 20);
            soundFeedbackCheckBox.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            soundFeedbackCheckBox.CheckedChanged += SoundFeedbackCheckBox_CheckedChanged;
            appGroup.Controls.Add(soundFeedbackCheckBox);
            
        }

        private void CreateActionButtons()
        {
            // OK Button
            Button okButton = new Button();
            okButton.Text = "OK";
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(300, 340);
            okButton.Size = new Size(80, 25);
            okButton.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            okButton.Click += okButton_Click;
            this.Controls.Add(okButton);

            // Cancel Button
            Button cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(390, 340);
            cancelButton.Size = new Size(80, 25);
            cancelButton.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            cancelButton.Click += cancelButton_Click;
            this.Controls.Add(cancelButton);

            // Set form's AcceptButton and CancelButton
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void UpdateOsdStyleVisibility(bool showOsdOptions)
        {
            osdStyleLabel.Visible = showOsdOptions;
            osdStyleComboBox.Visible = showOsdOptions;
        }
    }
}
