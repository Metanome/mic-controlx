using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MicControlX
{
    /// <summary>
    /// UI implementation for MainWindow - Contains all visual components and layout
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Initialize all UI components and layout
        /// </summary>
        private void InitializeComponent()
        {
            this.Text = "MicControlX";
            this.Size = new Size(500, 450); // Reduced height since removing buttons
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true; // Enable minimize to tray
            this.BackColor = Color.FromArgb(248, 249, 250);
            
            CreateTitleSection();
            CreateHotkeyCard();
            CreateFeaturesCard();
            CreateControlsCard();
            CreateStatusCard();
            CreateSystemInfoPanel();
        }

        /// <summary>
        /// Create the main title section with Settings and Info buttons
        /// </summary>
        private void CreateTitleSection()
        {
            Label titleLabel = new Label();
            titleLabel.Text = "MicControlX";
            titleLabel.Location = new Point(20, 15);
            titleLabel.Size = new Size(380, 35);
            titleLabel.TextAlign = ContentAlignment.TopCenter;
            titleLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(33, 37, 41);
            titleLabel.Name = "TitleLabel";
            this.Controls.Add(titleLabel);

            // Settings button (PNG-based)
            Button settingsButton = CreatePngButton("settings.png", "Settings", new Point(406, 12));
            settingsButton.Click += (sender, e) => ShowConfigDialog();
            settingsButton.Name = "SettingsButton";
            this.Controls.Add(settingsButton);

            // Info button (PNG-based)
            Button infoButton = CreatePngButton("info.png", "Info", new Point(446, 12));
            infoButton.Click += (sender, e) => ShowInfoDialog();
            infoButton.Name = "InfoButton";
            this.Controls.Add(infoButton);
        }

        /// <summary>
        /// Create the hotkey information card
        /// </summary>
        private void CreateHotkeyCard()
        {
            Panel hotkeyCard = new Panel();
            hotkeyCard.Location = new Point(20, 60);
            hotkeyCard.Size = new Size(450, 70);
            hotkeyCard.BackColor = Color.FromArgb(13, 110, 253);
            hotkeyCard.BorderStyle = BorderStyle.FixedSingle;
            
            Label hotkeyTitle = new Label();
            hotkeyTitle.Text = "Global Hotkey";
            hotkeyTitle.Location = new Point(15, 10);
            hotkeyTitle.Size = new Size(200, 20);
            hotkeyTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            hotkeyTitle.ForeColor = Color.White;
            hotkeyCard.Controls.Add(hotkeyTitle);
            
            Label hotkeyValue = new Label();
            hotkeyValue.Text = $"Press {config.HotKeyDisplayName} to toggle, hold to mute/unmute temporarily";
            hotkeyValue.Location = new Point(15, 35);
            hotkeyValue.Size = new Size(420, 25);
            hotkeyValue.Font = new Font("Segoe UI", 12, FontStyle.Regular);
            hotkeyValue.ForeColor = Color.White;
            hotkeyValue.Name = "HotkeyLabel";
            hotkeyCard.Controls.Add(hotkeyValue);
            this.Controls.Add(hotkeyCard);
        }

        /// <summary>
        /// Create the features and compatibility card
        /// </summary>
        private void CreateFeaturesCard()
        {
            Panel featuresCard = new Panel();
            featuresCard.Location = new Point(20, 140);
            featuresCard.Size = new Size(450, 80);
            featuresCard.BackColor = Color.White;
            featuresCard.BorderStyle = BorderStyle.FixedSingle;
            
            Label featuresTitle = new Label();
            featuresTitle.Text = "✅ Features & Compatibility";
            featuresTitle.Location = new Point(15, 10);
            featuresTitle.Size = new Size(420, 20);
            featuresTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            featuresTitle.ForeColor = Color.FromArgb(33, 37, 41);
            featuresCard.Controls.Add(featuresTitle);
            
            Label featuresText = new Label();
            featuresText.Text = "• Optimized for Lenovo gaming laptops (Legion, LOQ, IdeaPad)\n" +
                               "• Works seamlessly with: Vantage, LLT, Fn+F4, Windows settings";
            featuresText.Location = new Point(15, 35);
            featuresText.Size = new Size(420, 40);
            featuresText.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            featuresText.ForeColor = Color.FromArgb(73, 80, 87);
            featuresCard.Controls.Add(featuresText);
            this.Controls.Add(featuresCard);
        }

        /// <summary>
        /// Create the alternative controls information card
        /// </summary>
        private void CreateControlsCard()
        {
            Panel altControlsCard = new Panel();
            altControlsCard.Location = new Point(20, 230);
            altControlsCard.Size = new Size(450, 70);
            altControlsCard.BackColor = Color.White;
            altControlsCard.BorderStyle = BorderStyle.FixedSingle;
            
            Label altTitle = new Label();
            altTitle.Text = "🖱️ Alternative Controls";
            altTitle.Location = new Point(15, 10);
            altTitle.Size = new Size(420, 20);
            altTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            altTitle.ForeColor = Color.FromArgb(33, 37, 41);
            altControlsCard.Controls.Add(altTitle);
            
            Label altText = new Label();
            altText.Text = "• Single-click system tray icon to toggle  • Double-click tray to open app";
            altText.Location = new Point(15, 35);
            altText.Size = new Size(420, 25);
            altText.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            altText.ForeColor = Color.FromArgb(73, 80, 87);
            altControlsCard.Controls.Add(altText);
            this.Controls.Add(altControlsCard);
        }

        /// <summary>
        /// Create the current status display card
        /// </summary>
        private void CreateStatusCard()
        {
            Panel statusCard = new Panel();
            statusCard.Location = new Point(20, 310);
            statusCard.Size = new Size(450, 40);
            statusCard.BackColor = Color.FromArgb(25, 135, 84);
            statusCard.BorderStyle = BorderStyle.FixedSingle;
            statusCard.Name = "StatusCard";
            
            Label currentStatus = new Label();
            currentStatus.Text = "Current Status: Unknown";
            currentStatus.Location = new Point(15, 10);
            currentStatus.Size = new Size(420, 20);
            currentStatus.TextAlign = ContentAlignment.MiddleLeft;
            currentStatus.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            currentStatus.ForeColor = Color.White;
            currentStatus.Name = "StatusLabel";
            statusCard.Controls.Add(currentStatus);
            this.Controls.Add(statusCard);
        }

        /// <summary>
        /// Create the system information panel
        /// </summary>
        private void CreateSystemInfoPanel()
        {
            Panel systemInfoPanel = new Panel();
            systemInfoPanel.Location = new Point(20, 360);
            systemInfoPanel.Size = new Size(450, 30);
            systemInfoPanel.BorderStyle = BorderStyle.FixedSingle;
            
            // Set color based on Lenovo detection
            bool isLenovo = config.DetectedBrand.ToUpper().Contains("LENOVO");
            systemInfoPanel.BackColor = isLenovo ? Color.FromArgb(217, 237, 247) : Color.FromArgb(255, 243, 243); // Light blue for Lenovo, light red for others
            
            Label systemInfoLabel = new Label();
            systemInfoLabel.Text = $"Detected: {config.DetectedBrand} {config.DetectedModel}, {WindowsVersionInfo}";
            systemInfoLabel.Location = new Point(10, 6);
            systemInfoLabel.Size = new Size(430, 18);
            systemInfoLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            systemInfoLabel.ForeColor = isLenovo ? Color.FromArgb(13, 110, 253) : Color.FromArgb(220, 53, 69); // Blue for Lenovo, red for others
            systemInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
            systemInfoPanel.Controls.Add(systemInfoLabel);
            this.Controls.Add(systemInfoPanel);
        }

        /// <summary>
        /// Create a button with PNG icon from embedded resources (theme-aware)
        /// </summary>
        private Button CreatePngButton(string baseResourceName, string tooltipText, Point location)
        {
            var button = new Button();
            button.Size = new Size(36, 36); // Larger button to accommodate 32px icons
            button.Location = location;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, Color.White);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(50, Color.White);
            button.BackColor = Color.Transparent;
            button.Cursor = Cursors.Hand;

            // Determine theme-appropriate resource name
            bool isDarkTheme = GetEffectiveTheme();
            string themeVariant = isDarkTheme ? "_dark" : "_light";
            string resourceName = baseResourceName.Replace(".png", $"{themeVariant}.png");

            // Load PNG icon from embedded resources - unscaled for pixel-perfect rendering
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    // Use image directly without any scaling for your 32px PNGs
                    button.Image = Image.FromStream(stream);
                    button.ImageAlign = ContentAlignment.MiddleCenter;
                }
                else
                {
                    // Try fallback to base resource name without theme variant
                    using var fallbackStream = assembly.GetManifestResourceStream(baseResourceName);
                    if (fallbackStream != null)
                    {
                        // Use image directly without scaling
                        button.Image = Image.FromStream(fallbackStream);
                        button.ImageAlign = ContentAlignment.MiddleCenter;
                    }
                    else
                    {
                        // Fallback to text if PNG not found
                        SetFallbackButtonAppearance(button, tooltipText, isDarkTheme);
                    }
                }
            }
            catch
            {
                // Fallback to text if PNG loading fails
                SetFallbackButtonAppearance(button, tooltipText, isDarkTheme);
            }

            // Add tooltip
            var toolTip = new ToolTip();
            toolTip.SetToolTip(button, tooltipText);

            return button;
        }

        /// <summary>
        /// Set fallback button appearance when PNG icons are not available
        /// </summary>
        private void SetFallbackButtonAppearance(Button button, string tooltipText, bool isDarkTheme)
        {
            button.Text = tooltipText == "Settings" ? "SET" : "INFO";
            button.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            
            if (tooltipText == "Settings")
            {
                button.ForeColor = isDarkTheme ? Color.FromArgb(173, 181, 189) : Color.FromArgb(108, 117, 125);
            }
            else // Info button
            {
                button.ForeColor = isDarkTheme ? Color.FromArgb(86, 156, 255) : Color.FromArgb(13, 110, 253);
            }
        }

        /// <summary>
        /// Get the effective theme (resolves System theme to actual Dark/Light)
        /// </summary>
        private bool GetEffectiveTheme()
        {
            if (config.AppUITheme == AppTheme.Dark)
                return true;
            if (config.AppUITheme == AppTheme.Light)
                return false;
            
            // For System theme, check Windows dark mode setting
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int value)
                {
                    return value == 0; // 0 = dark theme, 1 = light theme
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme detection error: {ex.Message}");
                // If registry check fails, default to light theme
            }
            
            return false; // Default to light theme
        }

        /// <summary>
        /// Update PNG button icons when theme changes
        /// </summary>
        public void UpdatePngButtonsForTheme()
        {
            // Update Settings button
            var settingsButton = this.Controls.Find("SettingsButton", true);
            if (settingsButton.Length > 0)
            {
                UpdateButtonIcon((Button)settingsButton[0], "settings.png");
            }
            
            // Update Info button  
            var infoButton = this.Controls.Find("InfoButton", true);
            if (infoButton.Length > 0)
            {
                UpdateButtonIcon((Button)infoButton[0], "info.png");
            }
        }
        
        /// <summary>
        /// Update a single button's icon based on current theme
        /// </summary>
        private void UpdateButtonIcon(Button button, string baseResourceName)
        {
            bool isDarkTheme = GetEffectiveTheme();
            string themeVariant = isDarkTheme ? "_dark" : "_light";
            string resourceName = baseResourceName.Replace(".png", $"{themeVariant}.png");
            
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    // Dispose old image to prevent memory leaks
                    var oldImage = button.Image;
                    oldImage?.Dispose();
                    
                    // Load new image without scaling for pixel-perfect rendering
                    button.Image = Image.FromStream(stream);
                    button.ImageAlign = ContentAlignment.MiddleCenter;
                    button.Text = ""; // Clear any fallback text
                }
            }
            catch
            {
                // Keep existing fallback if loading fails
            }
        }

        /// <summary>
        /// Update the status label and status card visual state
        /// </summary>
        public void UpdateStatusLabel(string status)
        {
            var statusLabel = this.Controls.Find("StatusLabel", true);
            if (statusLabel.Length > 0)
            {
                statusLabel[0].Text = $"Current Status: {status}";
                
                // Update status card color based on mute state
                var statusCard = this.Controls.Find("StatusCard", false);
                if (statusCard.Length > 0)
                {
                    if (status.Contains("Muted"))
                    {
                        statusCard[0].BackColor = Color.FromArgb(220, 53, 69); // Red for muted
                    }
                    else if (status.Contains("Active"))
                    {
                        statusCard[0].BackColor = Color.FromArgb(25, 135, 84); // Green for active
                    }
                    else
                    {
                        statusCard[0].BackColor = Color.FromArgb(108, 117, 125); // Gray for unknown
                    }
                }
            }
        }
    }
}
