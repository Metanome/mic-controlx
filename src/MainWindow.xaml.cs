using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace MicControlX
{
    /// <summary>
    /// Main Window for MicControlX WPF - Provides microphone control with Legion Toolkit styling
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        #region Windows API
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        #endregion

        #region Fields
        private readonly AudioController micController = new();
        private readonly FocusAssistMonitor focusAssistMonitor = new();
        private OsdOverlay? osd;
        private ApplicationConfig config = null!;
        private DateTime lastOSDUpdate = DateTime.MinValue;
        private DispatcherTimer? singleClickTimer;
        private bool isDoubleClick = false;
        private bool isPushToTalkActive = false;
        private bool wasMutedBeforePTT = false;
        private DispatcherTimer? pttHoldTimer;
        private DispatcherTimer? keyReleaseMonitor;
        private const int PTT_HOLD_THRESHOLD = 400; // ms to detect hold vs quick press
        private const int KEY_MONITOR_INTERVAL = 30; // ms to check key release
        private readonly object configLock = new object();
        private TaskbarIcon? trayIcon;
        private HotkeyManager? hotkeyManager;
        private bool isInitializing = true; // Flag to prevent OSD during startup
        #endregion

        #region Constructor
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                
                // Load configuration
                config = ConfigurationManager.LoadConfiguration();
                
                // Initialize components safely
                InitializeTimers();
                InitializeSystemTray();
                InitializeFocusAssistMonitor();
                UpdateUI();
                
                // Wire up events
                micController.MuteStateChanged += OnMuteStateChanged;
                micController.ErrorOccurred += OnMicrophoneError;
                micController.ExternalChangeDetected += OnExternalChangeDetected;
                micController.DeviceChanged += OnDeviceChanged;
                LocalizationManager.LanguageChanged += OnLanguageChanged;
                
                // Initialize hotkey manager (works regardless of window visibility)
                InitializeHotkeyManager();
                
                // Handle window events
                Loaded += MainWindow_Loaded;
                Closing += MainWindow_Closing;
                StateChanged += MainWindow_StateChanged;
                WindowState = WindowState.Normal;
                ShowInTaskbar = true;
                
                // Initialize OSD after everything else is ready
                try
                {
                    InitializeOsd();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"OSD initialization failed: {ex.Message}");
                    MessageBox.Show(
                        string.Format(Strings.OSDInitializationWarningMessage, ex.Message),
                        Strings.OSDInitializationWarningTitle,
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format(Strings.ErrorInitializationMessage, ex.Message, ex.StackTrace), 
                    Strings.ErrorStartupTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }
        #endregion

        #region Initialization
        private void InitializeTimers()
        {
            // Single-click timer
            singleClickTimer = new DispatcherTimer();
            singleClickTimer.Interval = TimeSpan.FromMilliseconds(300);
            singleClickTimer.Tick += SingleClickTimer_Tick;

            // PTT hold timer - detects when key is held long enough for push-to-talk
            pttHoldTimer = new DispatcherTimer();
            pttHoldTimer.Interval = TimeSpan.FromMilliseconds(PTT_HOLD_THRESHOLD);
            pttHoldTimer.Tick += PttHoldTimer_Tick;

            // Key release monitor - monitors for key release during push-to-talk
            keyReleaseMonitor = new DispatcherTimer();
            keyReleaseMonitor.Interval = TimeSpan.FromMilliseconds(KEY_MONITOR_INTERVAL);
            keyReleaseMonitor.Tick += KeyReleaseMonitor_Tick;
        }

        private void InitializeOsd()
        {
            osd = new OsdOverlay(config.OSDStyle, config.OSDPosition, config.OSDDurationSeconds);
        }

        private void RefreshOsd()
        {
            // Dispose the old OSD and create a new one with updated settings
            osd?.Close();
            osd = new OsdOverlay(config.OSDStyle, config.OSDPosition, config.OSDDurationSeconds);
        }

        private void InitializeHotkeyManager()
        {
            try
            {
                hotkeyManager = new HotkeyManager();
                hotkeyManager.HotkeyPressed += HandleHotkey;
                
                // Register the current hotkey
                bool success = hotkeyManager.RegisterHotkey(config.HotKeyVirtualKey);
                if (!success)
                {
                    MessageBox.Show(
                        string.Format(Strings.ErrorHotkeyRegistrationMessage, config.HotKeyDisplayName),
                        Strings.ErrorHotkeyRegistrationTitle, 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hotkey manager initialization failed: {ex.Message}");
            }
        }

        private void InitializeSystemTray()
        {
            try
            {
                trayIcon = new TaskbarIcon();
                trayIcon.ToolTipText = Strings.AppTitle;
                trayIcon.MenuActivation = Hardcodet.Wpf.TaskbarNotification.PopupActivationMode.RightClick;
                
                // Use single click timer to distinguish between single and double clicks
                trayIcon.TrayLeftMouseUp += TrayIcon_LeftClick;
                trayIcon.TrayMouseDoubleClick += TrayIcon_DoubleClick;
                
                // Create context menu
                CreateContextMenu();
                
                // Set initial icon
                UpdateTrayIcon(micController.IsMuted);
                
                trayIcon.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tray initialization failed: {ex.Message}");
            }
        }

        private void InitializeFocusAssistMonitor()
        {
            try
            {
                // Start monitoring Focus Assist status
                focusAssistMonitor.StartMonitoring();
                
                // Subscribe to status changes for debugging/logging
                focusAssistMonitor.StatusChanged += OnFocusAssistStatusChanged;
                
                Debug.WriteLine($"Focus Assist monitoring started. Current status: {focusAssistMonitor.CurrentStatus}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Focus Assist monitor initialization failed: {ex.Message}");
            }
        }

        private void CreateContextMenu()
        {
            if (trayIcon != null)
            {
                var contextMenu = new System.Windows.Controls.ContextMenu();
                var toggleItem = new System.Windows.Controls.MenuItem { Header = Strings.ToggleMicrophone };
                toggleItem.Click += TrayToggle_Click;
                var settingsItem = new System.Windows.Controls.MenuItem { Header = Strings.Settings };
                settingsItem.Click += TraySettings_Click;
                var exitItem = new System.Windows.Controls.MenuItem { Header = Strings.Exit };
                exitItem.Click += TrayExit_Click;
                
                contextMenu.Items.Add(toggleItem);
                contextMenu.Items.Add(settingsItem);
                contextMenu.Items.Add(new System.Windows.Controls.Separator());
                contextMenu.Items.Add(exitItem);
                
                trayIcon.ContextMenu = contextMenu;
            }
        }

        private void RefreshContextMenu()
        {
            CreateContextMenu();
        }

        private void UpdateUI()
        {
            // Force UI refresh by dispatching to UI thread
            Dispatcher.Invoke(() =>
            {
                // Update hotkey label
                var newText = string.Format(Strings.HotkeyInstructionTemplate, config.HotKeyDisplayName);
                HotkeyLabel.Text = newText;
                
                // Update system info
                BrandLabel.Text = $"{config.DetectedBrand} {config.DetectedModel}";
                PlatformLabel.Text = GetDetailedWindowsVersion();
                
                // Update microphone device display
                UpdateMicrophoneDeviceDisplay();
                
                // Update microphone status
                UpdateMicrophoneStatus();
                
                // Refresh OSD with updated settings
                RefreshOsd();
                
                // Force visual refresh
                InvalidateVisual();
                UpdateLayout();
            });
            
            // Apply current theme
            ThemeManager.ApplyTheme(config.Theme);
            
            // Update icon visibility based on theme
            UpdateIconVisibility();
        }
        
        private void UpdateIconVisibility()
        {
            // Ensure button icons are visible by setting the Icon property explicitly
            if (SettingsButton != null)
            {
                SettingsButton.Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Settings24 };
            }
            
            if (InfoButton != null)
            {
                InfoButton.Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Info24 };
            }
        }

        private string GetDetailedWindowsVersion()
        {
            try
            {
                var osVersion = Environment.OSVersion;
                string version = "Windows";
                
                if (osVersion.Version.Major == 10)
                {
                    if (osVersion.Version.Build >= 22000)
                        version = "Windows 11";
                    else
                        version = "Windows 10";
                }
                else
                {
                    version = $"Windows {osVersion.Version.Major}.{osVersion.Version.Minor}";
                }
                
                // Try to get edition and build info
                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                    if (key != null)
                    {
                        string? edition = key.GetValue("EditionID")?.ToString();
                        string? buildNumber = key.GetValue("CurrentBuild")?.ToString();
                        string? displayVersion = key.GetValue("DisplayVersion")?.ToString();
                        
                        if (!string.IsNullOrEmpty(edition) && edition != "Core")
                        {
                            version += $" {edition}";
                        }
                        
                        if (!string.IsNullOrEmpty(displayVersion))
                        {
                            version += $" {displayVersion}";
                        }
                    }
                }
                catch
                {
                    // Fallback to basic version
                }
                
                return version;
            }
            catch
            {
                return Environment.OSVersion.VersionString;
            }
        }

        private void UpdateMicrophoneStatus()
        {
            try
            {
                bool isMuted = micController.IsMuted;
                
                if (StatusIcon != null && StatusText != null)
                {
                    if (isMuted)
                    {
                        StatusIcon.Symbol = Wpf.Ui.Controls.SymbolRegular.MicOff24;
                        StatusIcon.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                        StatusText.Text = Strings.Muted;
                        StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    }
                    else
                    {
                        StatusIcon.Symbol = Wpf.Ui.Controls.SymbolRegular.Mic24;
                        StatusIcon.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                        StatusText.Text = Strings.Active;
                        StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                    }
                }
                
                // Update tray icon
                UpdateTrayIcon(isMuted);
            }
            catch (Exception ex)
            {
                if (StatusText != null)
                {
                    StatusText.Text = Strings.ErrorTitle;
                }
                Debug.WriteLine($"Status update error: {ex.Message}");
            }
        }

        private void UpdateMicrophoneDeviceDisplay()
        {
            try
            {
                // Get current microphone device name
                string deviceName = micController.GetCurrentMicrophoneDeviceName();
                
                if (!string.IsNullOrEmpty(deviceName))
                {
                    MicrophoneDeviceLabel.Text = deviceName;
                }
                else
                {
                    MicrophoneDeviceLabel.Text = Strings.Unknown;
                }
                
                // Always show the microphone device information
                MicrophoneLabel.Visibility = Visibility.Visible;
                MicrophoneDeviceLabel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update microphone device display error: {ex.Message}");
                
                // Show "Unknown" on error instead of hiding
                MicrophoneDeviceLabel.Text = Strings.Unknown;
                MicrophoneLabel.Visibility = Visibility.Visible;
                MicrophoneDeviceLabel.Visibility = Visibility.Visible;
            }
        }

        private void UpdateTrayIcon(bool isMuted)
        {
            if (trayIcon == null) return;
            
            try
            {
                // Load icon from embedded resources
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var iconName = isMuted ? "logo_muted.ico" : "logo_active.ico";
                
                using (var stream = assembly.GetManifestResourceStream(iconName))
                {
                    if (stream != null)
                    {
                        trayIcon.Icon = new System.Drawing.Icon(stream);
                    }
                    else
                    {
                        // Fallback to default icon
                        using (var defaultStream = assembly.GetManifestResourceStream("logo.ico"))
                        {
                            if (defaultStream != null)
                            {
                                trayIcon.Icon = new System.Drawing.Icon(defaultStream);
                            }
                        }
                    }
                }
                
                trayIcon.ToolTipText = isMuted ? Strings.TrayTooltipMuted : Strings.TrayTooltipActive;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tray icon update failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Determines if OSD should be shown based on configuration and Focus Assist status
        /// </summary>
        /// <returns>True if OSD should be shown, false if it should be suppressed</returns>
        private bool ShouldShowOSD()
        {
            if (!config.ShowOSD || isInitializing)
                return false;
            
            // Check if Focus Assist should suppress the OSD
            if (config.RespectFocusAssist)
            {
                // Get current Focus Assist status and check if OSD should be suppressed
                var shouldSuppress = focusAssistMonitor.ShouldSuppressOSD(config.RespectFocusAssist);
                
                if (shouldSuppress)
                    return false;
            }
            
            return true;
        }
        
        #endregion

        #region Event Handlers
        private void OnMuteStateChanged(bool isMuted)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateMicrophoneStatus();

                // Only show OSD if conditions allow and enough time has passed since last update
                if (ShouldShowOSD() && DateTime.Now.Subtract(lastOSDUpdate).TotalMilliseconds > 500)
                {
                    osd?.ShowMicrophoneStatus(isMuted);
                    lastOSDUpdate = DateTime.Now;
                }
            });
        }

        private void OnMicrophoneError(string errorMessage)
        {
            Dispatcher.Invoke(() =>
            {
                // StatusLabel.Text = "Microphone error"; // Control doesn't exist in simplified UI
                // DeviceLabel.Text = errorMessage; // Control doesn't exist in simplified UI
                Debug.WriteLine($"Audio error: {errorMessage}");
            });
        }

        private void OnExternalChangeDetected(bool isMuted)
        {
            // Handle external changes (e.g., from hardware keys, other apps)
            Dispatcher.Invoke(() =>
            {
                UpdateMicrophoneStatus();
                
                // Only show OSD if conditions allow
                if (ShouldShowOSD())
                {
                    osd?.ShowMicrophoneStatus(isMuted);
                }
            });
        }

        private void OnDeviceChanged()
        {
            // Handle microphone device changes (e.g., plugging/unplugging headsets)
            System.Diagnostics.Debug.WriteLine("MainWindow: OnDeviceChanged called - updating microphone device display");
            Dispatcher.Invoke(() =>
            {
                UpdateMicrophoneDeviceDisplay();
            });
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            // Handle language changes by updating UI elements
            Dispatcher.Invoke(() =>
            {
                // Update dynamic elements that are programmatically set
                UpdateUI();
                RefreshContextMenu();
                
                // Update window title
                Title = Strings.AppTitle;
                
                // Force visual refresh
                InvalidateVisual();
                UpdateLayout();
            });
        }

        private void OnFocusAssistStatusChanged(object? sender, FocusAssistStatusChangedEventArgs e)
        {
            // Status change is logged in FocusAssistMonitor - no additional logging needed here
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Mark initialization as complete to allow OSD triggers
            isInitializing = false;
            
            // Don't hide automatically - let user decide when to minimize to tray
            // The window will show normally and user can minimize it manually
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Exit application when X button is clicked
            Application.Current.Shutdown();
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                ShowInTaskbar = false;
                if (trayIcon != null)
                {
                    trayIcon.Visibility = Visibility.Visible;
                }
            }
        }

        private void HandleHotkey()
        {
            try
            {
                // Start hold timer to detect if this is a quick press or hold
                if (pttHoldTimer?.IsEnabled != true)
                {
                    pttHoldTimer?.Start();
                    keyReleaseMonitor?.Start();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Hotkey handling error: {ex.Message}");
            }
        }

        private void PttHoldTimer_Tick(object? sender, EventArgs e)
        {
            // Key held long enough - start push-to-talk mode
            if (!isPushToTalkActive)
            {
                isPushToTalkActive = true;
                wasMutedBeforePTT = micController.IsMuted;
                
                // Temporarily change state (opposite of current)
                micController.SetMuted(!wasMutedBeforePTT);
                
                // Show OSD for temporary change
                if (ShouldShowOSD())
                {
                    osd?.ShowMicrophoneStatus(!wasMutedBeforePTT);
                }
                
                // Show tray notification if enabled
                if (config.ShowNotifications)
                {
                    string title = Strings.AppTitle;
                    string message = !wasMutedBeforePTT ? Strings.NotificationMicrophoneTemporarilyMuted : Strings.NotificationMicrophoneTemporarilyActive;
                    var icon = !wasMutedBeforePTT ? BalloonIcon.Warning : BalloonIcon.Info;
                    trayIcon?.ShowBalloonTip(title, message, icon);
                }
            }
            
            pttHoldTimer?.Stop();
        }

        private void KeyReleaseMonitor_Tick(object? sender, EventArgs e)
        {
            // Check if hotkey is still pressed (negative value means pressed)
            bool isKeyPressed = GetAsyncKeyState(config.HotKeyVirtualKey) < 0;
            
            if (!isKeyPressed)
            {
                // Key released
                if (isPushToTalkActive)
                {
                    // We were in push-to-talk mode, restore original state
                    micController.SetMuted(wasMutedBeforePTT);
                    
                    // Show OSD for state restoration
                    if (ShouldShowOSD())
                    {
                        osd?.ShowMicrophoneStatus(wasMutedBeforePTT);
                    }
                    
                    isPushToTalkActive = false;
                }
                else
                {
                    // Key was released before hold timer completed - this is a quick press
                    // Only toggle if the hold timer is still running (meaning it hasn't completed yet)
                    if (pttHoldTimer?.IsEnabled == true)
                    {
                        ToggleMicrophone();
                    }
                }
                
                // Stop all timers
                keyReleaseMonitor?.Stop();
                pttHoldTimer?.Stop();
            }
        }

        private void SingleClickTimer_Tick(object? sender, EventArgs e)
        {
            singleClickTimer?.Stop();
            
            if (!isDoubleClick)
            {
                // Single click - toggle microphone
                ToggleMicrophone();
            }
            
            isDoubleClick = false;
        }

        // Manual Control Events
        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                micController.SetMuted(true);
                if (ShouldShowOSD())
                {
                    osd?.ShowMicrophoneStatus(true);
                }
            }
            catch (Exception ex)
            {
                OnMicrophoneError($"Manual mute failed: {ex.Message}");
            }
        }

        private void UnmuteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                micController.SetMuted(false);
                if (ShouldShowOSD())
                {
                    osd?.ShowMicrophoneStatus(false);
                }
            }
            catch (Exception ex)
            {
                OnMicrophoneError($"Manual unmute failed: {ex.Message}");
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAbout();
        }

        // Tray Events
        private void TrayIcon_LeftClick(object sender, RoutedEventArgs e)
        {
            // Start timer to detect if this is part of a double-click
            if (singleClickTimer?.IsEnabled == true)
            {
                // This is likely a double-click, don't process single click
                return;
            }
            
            // Start the timer to wait for potential double-click
            isDoubleClick = false;
            singleClickTimer?.Start();
        }

        private void TrayIcon_DoubleClick(object sender, RoutedEventArgs e)
        {
            // Mark as double-click and stop single-click timer
            isDoubleClick = true;
            singleClickTimer?.Stop();
            
            // Show/focus the main window
            ShowMainWindow();
        }

        private void TrayToggle_Click(object sender, RoutedEventArgs e)
        {
            ToggleMicrophone();
        }

        private void TraySettings_Click(object sender, RoutedEventArgs e)
        {
            ShowMainWindow();
            ShowSettings();
        }

        private void TrayExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        #endregion

        #region Helper Methods
        private void ToggleMicrophone()
        {
            try
            {
                bool wasToggled = micController.ToggleMute();
                if (wasToggled)
                {
                    bool currentState = micController.IsMuted;
                    
                    // Show OSD if enabled
                    if (ShouldShowOSD())
                    {
                        osd?.ShowMicrophoneStatus(currentState);
                    }
                    
                    // Show tray notification if enabled
                    if (config.ShowNotifications)
                    {
                        string title = Strings.AppTitle;
                        string message = currentState ? Strings.NotificationMicrophoneMuted : Strings.NotificationMicrophoneActive;
                        var icon = currentState ? BalloonIcon.Warning : BalloonIcon.Info;
                        
                        trayIcon?.ShowBalloonTip(title, message, icon);
                    }
                    
                    // Play sound feedback if enabled
                    if (config.EnableSoundFeedback)
                    {
                        if (currentState)
                        {
                            SoundFeedback.PlayMuteSound();
                        }
                        else
                        {
                            SoundFeedback.PlayUnmuteSound();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnMicrophoneError($"Toggle failed: {ex.Message}");
            }
        }

        private void ShowMainWindow()
        {
            try
            {
                Show();
                WindowState = WindowState.Normal;
                ShowInTaskbar = true;
                Activate();
                Focus();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Show window error: {ex.Message}");
            }
        }

        private void ShowSettings()
        {
            try
            {
                // Ensure config is loaded
                if (config == null)
                {
                    config = ConfigurationManager.LoadConfiguration();
                }
                
                var settingsWindow = new SettingsWindow(config);
                settingsWindow.Owner = this;
                
                settingsWindow.ShowDialog();
                
                if (settingsWindow.SettingsWereSaved)
                {
                    // Configuration was updated, reload from saved file to ensure we have all changes
                    config = ConfigurationManager.LoadConfiguration();
                    
                    // Apply theme immediately
                    ThemeManager.ApplyTheme(config.Theme);
                    
                    // Update UI elements
                    UpdateUI();
                    
                    // Re-register hotkeys with new settings
                    hotkeyManager?.UnregisterCurrentHotkey();
                    bool success = hotkeyManager?.RegisterHotkey(config.HotKeyVirtualKey) ?? false;
                    if (!success)
                    {
                        var conflictInfo = ConfigurationManager.VirtualKeys.GetKeyConflictInfo(config.HotKeyVirtualKey);
                        var alternatives = string.Join(", ", ConfigurationManager.VirtualKeys.GetSuggestedAlternatives(config.HotKeyVirtualKey));
                        
                        MessageBox.Show(
                            string.Format(Strings.ErrorHotkeyConflictMessage, config.HotKeyDisplayName, conflictInfo, alternatives),
                            Strings.ErrorHotkeyRegistrationTitle, 
                            System.Windows.MessageBoxButton.OK, 
                            System.Windows.MessageBoxImage.Warning);
                    }
                    
                    // Reinitialize OSD with new style
                    osd?.Close();
                    osd = null;
                    InitializeOsd();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow: Settings error - {ex.Message}");
                System.Windows.MessageBox.Show(string.Format(Strings.SettingsErrorMessage, ex.Message), Strings.ErrorTitle, 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ShowAbout()
        {
            try
            {
                var aboutWindow = new AboutWindow
                {
                    Owner = this
                };
                aboutWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format(Strings.ErrorAboutWindow, ex.Message, ex.StackTrace), 
                    Strings.ErrorTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        #endregion

        #region Cleanup
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Stop timers
                singleClickTimer?.Stop();
                pttHoldTimer?.Stop();
                keyReleaseMonitor?.Stop();
                
                // Dispose hotkey manager
                hotkeyManager?.Dispose();
                
                // Dispose resources
                trayIcon?.Dispose();
                osd?.Close();
                micController?.Dispose();
                focusAssistMonitor?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
            
            base.OnClosed(e);
        }
        #endregion
    }
}
