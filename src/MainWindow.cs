using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;

namespace MicControlX
{
    /// <summary>
    /// Main Window - Application Logic and Event Handling
    /// UI components are defined in MainWindow_UI.cs
    /// </summary>
    public partial class MainWindow : Form
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 1;
        
        private NotifyIcon? notifyIcon;
        private readonly AudioController micController = new();
        private OsdOverlay? osd; // Will be initialized with config style
        private ApplicationConfig config;
        private bool isStartingUp = true; // Flag to prevent OSD during startup
        private DateTime lastOSDUpdate = DateTime.MinValue; // Debouncing for OSD updates
        private System.Windows.Forms.Timer? singleClickTimer; // Timer to distinguish single vs double click
        private bool isDoubleClick = false; // Flag to track double-click
        private bool isPushToTalkActive = false; // Track if PTT is currently active
        private bool wasMutedBeforePTT = false; // Store mic state before PTT activation
        private System.Windows.Forms.Timer? pttHoldTimer; // Timer to detect key hold
        private System.Windows.Forms.Timer? keyReleaseMonitor; // Timer to monitor key release
        private const int PTT_HOLD_THRESHOLD = 400; // 400ms to distinguish tap vs hold
        private const int KEY_MONITOR_INTERVAL = 30; // Check key state every 30ms
        
        // Synchronization for configuration changes
        private readonly object configLock = new object();
        
        // Property to provide Windows version info to UI
        private string WindowsVersionInfo => GetDetailedWindowsVersion();
        
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public MainWindow()
        {   
            // Load configuration first
            config = ConfigurationManager.LoadConfiguration();
            
            // Initialize single-click timer
            InitializeSingleClickTimer();
            
            // Initialize PTT hold timer
            InitializePTTTimer();
            
            // Initialize OSD with configured style (no theme parameter - OSD has fixed styling)
            osd = new OsdOverlay(config.OSDStyle);
            
            // Load default embedded icons
            osd.LoadDefaultIcons();
            
            InitializeComponent();
            LoadApplicationIcon();
            InitializeSystemTray();
            RegisterGlobalHotKey();
            
            // Apply theme to main form
            ThemeManager.ApplyTheme(this, config.AppUITheme);
            
            // Wire up microphone controller events
            micController.MuteStateChanged += OnMuteStateChanged;
            micController.ErrorOccurred += OnMicrophoneError;
            micController.ExternalChangeDetected += OnExternalChangeDetected;
            
            this.Load += (s, e) => {
                this.Hide();
                CheckInitialMicrophoneStatus();
            };
            
            // Handle window state changes
            this.Resize += (s, e) => {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.Hide();
                    if (!isStartingUp)
                    {
                        ShowNotification("App minimized to system tray. Single-click to toggle microphone.");
                    }
                }
            };
        }

        private void InitializeSingleClickTimer()
        {
            singleClickTimer = new System.Windows.Forms.Timer();
            singleClickTimer.Interval = SystemInformation.DoubleClickTime;
            singleClickTimer.Tick += (sender, e) => {
                singleClickTimer.Stop();
                if (!isDoubleClick)
                {
                    ToggleMicrophone(); // Execute single-click action
                }
                isDoubleClick = false;
            };
        }

        private void InitializePTTTimer()
        {
            pttHoldTimer = new System.Windows.Forms.Timer();
            pttHoldTimer.Interval = PTT_HOLD_THRESHOLD;
            pttHoldTimer.Tick += (sender, e) => {
                pttHoldTimer.Stop();
                // Key has been held long enough - start PTT mode
                if (!isPushToTalkActive)
                {
                    StartPushToTalk();
                }
            };
            
            // Initialize key release monitor
            keyReleaseMonitor = new System.Windows.Forms.Timer();
            keyReleaseMonitor.Interval = KEY_MONITOR_INTERVAL;
            keyReleaseMonitor.Tick += MonitorKeyRelease;
        }

        private void InitializeSystemTray()
        {
            notifyIcon = new NotifyIcon();
            
            // Load default logo icon
            UpdateTrayIconState(null); // null means use default logo
            
            notifyIcon.Text = $"Hotkey changed! Press {config.HotKeyDisplayName} to toggle now";
            notifyIcon.Visible = true;
            
            // Create context menu
            CreateTrayContextMenu();
            
            // Double-click to open main window  
            notifyIcon.DoubleClick += (sender, e) => {
                isDoubleClick = true;
                singleClickTimer?.Stop();
                ShowWindow();
            };
            
            // Single click to toggle microphone (with delay to distinguish from double-click)
            notifyIcon.Click += (sender, e) => {
                if (((MouseEventArgs)e).Button == MouseButtons.Left)
                {
                    isDoubleClick = false;
                    singleClickTimer?.Stop();
                    singleClickTimer?.Start();
                }
            };
        }

        private void UpdateTrayIconState(bool? isMuted)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string iconName = isMuted switch
                {
                    true => "logo_muted.ico",
                    false => "logo_active.ico", 
                    null => "logo.ico" // Default state
                };

                using (var stream = assembly.GetManifestResourceStream(iconName))
                {
                    if (stream != null && notifyIcon != null)
                    {
                        notifyIcon.Icon = new Icon(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load tray icon: {ex.Message}");
            }
        }

        private void CreateTrayContextMenu()
        {
            if (notifyIcon != null)
            {
                // Dispose old context menu to prevent memory leaks
                notifyIcon.ContextMenuStrip?.Dispose();
                
                // Create new context menu
                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("Show Window", null, (sender, e) => ShowWindow());
                contextMenu.Items.Add($"Toggle Microphone ({config.HotKeyDisplayName})", null, (sender, e) => ToggleMicrophone());
                contextMenu.Items.Add("-");
                contextMenu.Items.Add("Settings", null, (sender, e) => ShowConfigDialog());
                contextMenu.Items.Add("-");
                contextMenu.Items.Add("Exit", null, (sender, e) => ExitApplication());
                notifyIcon.ContextMenuStrip = contextMenu;
            }
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate(); // Force window to come to front
            this.Focus(); // Give focus to the window
        }

        /// <summary>
        /// Bring this window to foreground (used by single instance enforcement)
        /// </summary>
        public void BringToForeground()
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            
            this.Show();
            this.BringToFront();
            this.Activate();
            this.Focus();
        }

        private void RegisterGlobalHotKey()
        {
            bool success = TryRegisterHotKey(config.HotKeyVirtualKey, config.HotKeyDisplayName);
            if (!success)
            {
                MessageBox.Show($"Failed to register {config.HotKeyDisplayName} hotkey. The key might be in use by another application.\n\nYou can still use the system tray icon (single-click) to toggle the microphone.", 
                               "Hotkey Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool TryRegisterHotKey(int virtualKey, string displayName)
        {
            // Validate virtual key first
            if (virtualKey < 0x70 || virtualKey > 0x87)
            {
                return false;
            }
            
            // First, ensure any existing hotkey is unregistered
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            System.Threading.Thread.Sleep(100);
            
            // Try to register the hotkey with retry logic - single key only (no modifiers)
            for (int i = 0; i < 5; i++) // Increased attempts for reliability
            {
                if (RegisterHotKey(this.Handle, HOTKEY_ID, 0, virtualKey))
                {
                    return true;
                }
                
                // Progressive delay - Windows sometimes needs more time
                if (i < 4) // Don't sleep on the last iteration
                {
                    int delay = (i + 1) * 150; // 150ms, 300ms, 450ms, 600ms
                    System.Threading.Thread.Sleep(delay);
                    
                    // Try unregistering again between attempts
                    UnregisterHotKey(this.Handle, HOTKEY_ID);
                    System.Threading.Thread.Sleep(50);
                }
            }
            
            return false;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                HandleHotkeyPress();
            }
            base.WndProc(ref m);
        }

        private void HandleHotkeyPress()
        {
            try
            {
                // Stop any existing timers
                pttHoldTimer?.Stop();
                keyReleaseMonitor?.Stop();
                
                // Start hold detection timer
                pttHoldTimer?.Start();
                
                // Start monitoring for key release
                keyReleaseMonitor?.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hotkey error: {ex.Message}");
                ResetPTTState();
            }
        }

        private void MonitorKeyRelease(object? sender, EventArgs e)
        {
            try
            {
                // Check if key is still pressed
                bool isStillPressed = (GetAsyncKeyState(config.HotKeyVirtualKey) & 0x8000) != 0;
                
                if (!isStillPressed)
                {
                    // Key released
                    keyReleaseMonitor?.Stop();
                    pttHoldTimer?.Stop();
                    
                    if (isPushToTalkActive)
                    {
                        // End PTT mode
                        EndPushToTalk();
                    }
                    else
                    {
                        // Was a quick tap - toggle microphone
                        ToggleMicrophone();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Key monitor error: {ex.Message}");
                ResetPTTState();
                // Emergency fallback - just toggle the microphone
                try
                {
                    ToggleMicrophone();
                }
                catch
                {
                    // Last resort - log and continue
                    System.Diagnostics.Debug.WriteLine("Emergency toggle failed");
                }
            }
        }

        private void StartPushToTalk()
        {
            try
            {
                if (!isPushToTalkActive)
                {
                    isPushToTalkActive = true;
                    wasMutedBeforePTT = micController.IsMuted;
                    
                    // Temporarily flip to opposite state
                    bool newState = !wasMutedBeforePTT;
                    micController.SetMuted(newState);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StartPushToTalk error: {ex.Message}");
                ResetPTTState();
            }
        }

        private void EndPushToTalk()
        {
            try
            {
                if (isPushToTalkActive)
                {
                    // Restore original state
                    micController.SetMuted(wasMutedBeforePTT);
                    isPushToTalkActive = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EndPushToTalk error: {ex.Message}");
                ResetPTTState();
            }
        }

        private void ResetPTTState()
        {
            isPushToTalkActive = false;
            pttHoldTimer?.Stop();
            keyReleaseMonitor?.Stop();
        }

        private void ToggleMicrophone()
        {
            try
            {
                // Use microphone controller for reliable microphone control
                micController.ToggleMute();
                // Note: The OSD will be shown via the OnMuteStateChanged event
            }
            catch (Exception ex)
            {
                if (config.ShowNotifications)
                {
                    ShowNotification("Error toggling microphone");
                }
                
                MessageBox.Show($"Error toggling microphone: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnMuteStateChanged(bool isMuted)
        {
            // Ensure we're on the UI thread for UI updates
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(OnMuteStateChanged), isMuted);
                return;
            }

            string status = isMuted ? "Microphone Muted" : "Microphone Active";
            
            // Play sound feedback if enabled (and not during startup)
            if (config.EnableSoundFeedback && !isStartingUp)
            {
                if (isMuted)
                {
                    SoundFeedback.PlayMuteSound();
                }
                else
                {
                    SoundFeedback.PlayUnmuteSound();
                }
            }
            
            // Only show OSD after startup is complete and with minimal debouncing
            if (config.ShowOSD && !isStartingUp && osd != null)
            {
                var now = DateTime.Now;
                if ((now - lastOSDUpdate).TotalMilliseconds > 100) // Reduced to 100ms for better responsiveness
                {
                    lastOSDUpdate = now;
                    try
                    {
                        osd.ShowMicrophoneStatus(isMuted);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"OSD Show Error: {ex.Message}");
                    }
                }
            }
            
            // Still show balloon notification if enabled in config (but less frequently)
            if (config.ShowNotifications && !isStartingUp)
            {
                ShowNotification(status);
            }
            
            UpdateStatusLabel(status);
            
            // Update tray icon to reflect current status
            UpdateTrayIconState(isMuted);
            if (notifyIcon != null)
            {
                notifyIcon.Text = $"Currently {(isMuted ? "Muted" : "Active")} - {config.HotKeyDisplayName} to toggle";
            }
        }

        private void OnExternalChangeDetected(bool isMuted)
        {
            // This event fires specifically for external changes (LLT, Fn+F4, Windows Settings, etc.)
            
            // Ensure we're on the UI thread for notifications
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(OnExternalChangeDetected), isMuted);
                return;
            }
            
            // Show a different message for external changes (but not during startup)
            if (config.ShowNotifications && !isStartingUp)
            {
                string source = "External change detected";
                ShowNotification($"{(isMuted ? "Muted" : "Active")} - {source}");
            }
        }

        private void OnMicrophoneError(string errorMessage)
        {
            ShowNotification($"Error: {errorMessage}");
        }

        private void ShowNotification(string message)
        {
            if (notifyIcon != null)
            {
                notifyIcon.BalloonTipTitle = "MicControlX";
                notifyIcon.BalloonTipText = message;
                notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                notifyIcon.ShowBalloonTip(3000);
            }
        }

        private void ExitApplication()
        {
            // Unregister the global hotkey
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            
            // Dispose of system tray icon
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
            
            // Dispose of microphone controller
            micController?.Dispose();
            
            // Force application exit
            Application.Exit();
            Environment.Exit(0);
        }

        // Override to handle minimize and close buttons
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // X button now exits the application
                ExitApplication();
            }
        }

        private void CheckInitialMicrophoneStatus()
        {
            try
            {
                // Wait a moment for the system to be fully ready
                System.Threading.Thread.Sleep(200);
                
                // Check actual microphone status using controller
                if (micController.GetCurrentMuteState())
                {
                    bool isMuted = micController.IsMuted;
                    string initialStatus = isMuted ? "Microphone Muted" : "Microphone Active";
                    UpdateStatusLabel(initialStatus);
                    
                    // Update tray icon text to reflect current status
                    if (notifyIcon != null)
                    {
                        notifyIcon.Text = $"Currently {(isMuted ? "Muted" : "Active")} - {config.HotKeyDisplayName} to toggle";
                    }
                }
                else
                {
                    UpdateStatusLabel($"Status: Ready to toggle (Press {config.HotKeyDisplayName})");
                    
                    if (notifyIcon != null)
                    {
                        notifyIcon.Text = $"Hotkey changed! Press {config.HotKeyDisplayName} to toggle now";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initial status check failed: {ex.Message}");
                UpdateStatusLabel($"Status: Ready to toggle (Press {config.HotKeyDisplayName})");
                
                if (notifyIcon != null)
                {
                    notifyIcon.Text = $"Hotkey changed! Press {config.HotKeyDisplayName} to toggle now";
                }
            }
            finally
            {
                // Startup is complete - allow OSD to show for future changes
                isStartingUp = false;
            }
        }

        private void ShowConfigDialog()
        {
            // Capture the current config state BEFORE creating SettingsWindow
            var oldConfigSnapshot = new ApplicationConfig
            {
                HotKeyVirtualKey = config.HotKeyVirtualKey,
                HotKeyDisplayName = config.HotKeyDisplayName,
                OSDStyle = config.OSDStyle,
                AppUITheme = config.AppUITheme,
                ShowOSD = config.ShowOSD,
                ShowNotifications = config.ShowNotifications,
                DetectedBrand = config.DetectedBrand,
                DetectedModel = config.DetectedModel,
                EnableLenovoFeatures = config.EnableLenovoFeatures,
                HasLenovoVantage = config.HasLenovoVantage,
                HasLegionToolkit = config.HasLegionToolkit,
                AutoStart = config.AutoStart,
                EnableSoundFeedback = config.EnableSoundFeedback
            };
            
            var configForm = new SettingsWindow(config);
            
            // Subscribe to real-time configuration changes
            configForm.ConfigurationChanged += (sender, newConfig) =>
            {
                HandleConfigurationChange(oldConfigSnapshot, newConfig);
                
                // Update the old config snapshot for the next change
                oldConfigSnapshot = new ApplicationConfig
                {
                    HotKeyVirtualKey = newConfig.HotKeyVirtualKey,
                    HotKeyDisplayName = newConfig.HotKeyDisplayName,
                    OSDStyle = newConfig.OSDStyle,
                    AppUITheme = newConfig.AppUITheme,
                    ShowOSD = newConfig.ShowOSD,
                    ShowNotifications = newConfig.ShowNotifications,
                    DetectedBrand = newConfig.DetectedBrand,
                    DetectedModel = newConfig.DetectedModel,
                    EnableLenovoFeatures = newConfig.EnableLenovoFeatures,
                    HasLenovoVantage = newConfig.HasLenovoVantage,
                    HasLegionToolkit = newConfig.HasLegionToolkit,
                    AutoStart = newConfig.AutoStart,
                    EnableSoundFeedback = newConfig.EnableSoundFeedback
                };
            };
            
            // Show as modal dialog (no DialogResult needed since changes are real-time)
            configForm.ShowDialog();
            configForm.Dispose();
        }

        private async void ShowInfoDialog()
        {
            string appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "3.1.1";
            
            // Check for updates asynchronously
            string updateStatus = "Checking for updates...";
            
            try
            {
                var updateInfo = await GitHubUpdateChecker.CheckForUpdatesAsync();
                if (updateInfo != null)
                {
                    updateStatus = $"Update Available: v{updateInfo.LatestVersion}\nA newer version is available on GitHub.";
                }
                else
                {
                    updateStatus = "You have the latest version.";
                }
            }
            catch (Exception ex)
            {
                updateStatus = "Unable to check for updates.";
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
            }
            
            string infoMessage = $@"MicControlX v{appVersion}

Developer: Dr. Skinner

Features:
• Global hotkey microphone toggle
• Push-to-talk functionality  
• Lenovo gaming laptop optimization
• System tray integration
• OSD notifications with PNG icons
• Sound feedback
• Theme-aware UI

Controls:
• Tap hotkey = Toggle microphone
• Hold hotkey = Temporary flip while held
• Single-click tray = Toggle
• Double-click tray = Show window

Compatible with:
• Lenovo Vantage & Legion Toolkit
• Windows native microphone controls
• All Windows microphone devices

Update Status:
{updateStatus}

For support and source code, visit my GitHub repository.";

            // Show info dialog - simple OK button only
            MessageBox.Show(infoMessage, "About MicControlX", 
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void HandleConfigurationChange(ApplicationConfig oldConfig, ApplicationConfig newConfig)
        {
            lock (configLock)
            {
                try
                {
                    // Compare specific properties to detect changes using the passed old config
                    bool hotkeyChanged = (oldConfig.HotKeyVirtualKey != newConfig.HotKeyVirtualKey);
                    bool osdStyleChanged = (oldConfig.OSDStyle != newConfig.OSDStyle);
                    bool themeChanged = (oldConfig.AppUITheme != newConfig.AppUITheme);
                    
                    // If no changes detected, exit early
                    if (!hotkeyChanged && !osdStyleChanged && !themeChanged)
                    {
                        return;
                    }
                    
                    // Update configuration 
                    config = newConfig;
                    ConfigurationManager.SaveConfiguration(config);
                    
                    // Handle hotkey change
                    if (hotkeyChanged)
                    {
                        // Reset PTT state to prevent conflicts
                        ResetPTTState();
                        
                        // Use the robust hotkey registration method
                        if (!TryRegisterHotKey(newConfig.HotKeyVirtualKey, newConfig.HotKeyDisplayName))
                        {
                            MessageBox.Show($"Failed to register {newConfig.HotKeyDisplayName} hotkey. The key might be in use by another application.", 
                                           "Hotkey Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        
                        // Update tray icon and UI after successful registration
                        UpdateTrayIcon();
                        
                        // Update hotkey label text (now using the updated config)
                        var hotkeyLabel = this.Controls.Find("HotkeyLabel", true);
                        if (hotkeyLabel.Length > 0 && hotkeyLabel[0] is Label label)
                        {
                            label.Text = $"Press {config.HotKeyDisplayName} to toggle, hold to mute/unmute temporarily";
                        }
                    }
                    
                    // Handle OSD style change (only reload images if OSD style actually changed)
                    if (osdStyleChanged && osd != null)
                    {
                        osd.SetOSDStyle(config.OSDStyle);
                    }
                    
                    // Handle theme change
                    if (themeChanged)
                    {
                        // Apply updated theme to main form
                        ThemeManager.ApplyTheme(this, config.AppUITheme);
                        
                        // Update PNG buttons for new theme
                        UpdatePngButtonsForTheme();
                        
                        // Force UI refresh to ensure changes are visible
                        this.Refresh();
                        
                        // Re-apply dynamic status color after theme change
                        var statusLabel = this.Controls.Find("StatusLabel", true);
                        if (statusLabel.Length > 0)
                        {
                            string currentStatus = statusLabel[0].Text;
                            UpdateStatusLabel(currentStatus.Replace("Current Status: ", ""));
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Configuration change error: {ex.Message}");
                    MessageBox.Show($"Error applying configuration changes: {ex.Message}", 
                                   "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void UpdateTrayIcon()
        {
            // Ensure we're on the UI thread
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateTrayIcon));
                return;
            }
            
            if (notifyIcon != null)
            {
                notifyIcon.Text = $"Hotkey changed! Press {config.HotKeyDisplayName} to toggle now";
                CreateTrayContextMenu(); // Use the centralized method
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    UnregisterHotKey(this.Handle, HOTKEY_ID);
                    
                    // Force close OSD first
                    osd?.ForceClose();
                    osd?.Dispose();
                    
                    notifyIcon?.Dispose();
                    micController?.Dispose();
                    singleClickTimer?.Dispose();
                    pttHoldTimer?.Dispose();
                    keyReleaseMonitor?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Dispose Error: {ex.Message}");
                }
            }
            base.Dispose(disposing);
        }

        private void LoadApplicationIcon()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("logo.ico"))
                {
                    if (stream != null)
                    {
                        this.Icon = new Icon(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load application icon: {ex.Message}");
            }
        }

        /// <summary>
        /// Get detailed Windows version information including edition
        /// </summary>
        private string GetDetailedWindowsVersion()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        var productName = key.GetValue("ProductName")?.ToString() ?? "";
                        var displayVersion = key.GetValue("DisplayVersion")?.ToString() ?? "";
                        
                        if (!string.IsNullOrEmpty(productName) && !string.IsNullOrEmpty(displayVersion))
                        {
                            return $"{productName} {displayVersion}";
                        }
                        
                        if (!string.IsNullOrEmpty(productName))
                        {
                            return productName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Registry Error: {ex.Message}");
            }
            
            // Fallback to OS version
            return $"Windows {Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}";
        }
    }
}
