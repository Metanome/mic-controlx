using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Wpf.Ui.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace MicControlX
{
    /// <summary>
    /// Main Window for MicControlX WPF Application
    /// Provides microphone control with Legion Toolkit styling
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        #region Windows API
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 1;
        
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        #endregion

        #region Fields
        private readonly AudioController micController = new();
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
                UpdateUI();
                
                // Wire up events
                micController.MuteStateChanged += OnMuteStateChanged;
                micController.ErrorOccurred += OnMicrophoneError;
                micController.ExternalChangeDetected += OnExternalChangeDetected;
                
            // Handle window events
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            StateChanged += MainWindow_StateChanged;                // Start normally - user can minimize to tray
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
                        $"Warning: Visual overlay (OSD) initialization failed.\n\n" +
                        $"Error: {ex.Message}\n\n" +
                        $"The application will work normally, but you won't see visual feedback when toggling the microphone.\n\n" +
                        $"You can try:\n" +
                        $"• Changing the OSD style in Settings\n" +
                        $"• Disabling OSD in Settings if the problem persists",
                        "OSD Initialization Warning",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Initialization error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "Startup Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
            osd = new OsdOverlay(config.OSDStyle);
        }

        private void InitializeSystemTray()
        {
            try
            {
                trayIcon = new TaskbarIcon();
                trayIcon.ToolTipText = "MicControlX";
                trayIcon.MenuActivation = Hardcodet.Wpf.TaskbarNotification.PopupActivationMode.RightClick;
                
                // Use single click timer to distinguish between single and double clicks
                trayIcon.TrayLeftMouseUp += TrayIcon_LeftClick;
                trayIcon.TrayMouseDoubleClick += TrayIcon_DoubleClick;
                
                // Create context menu
                var contextMenu = new System.Windows.Controls.ContextMenu();
                var toggleItem = new System.Windows.Controls.MenuItem { Header = "_Toggle Microphone" };
                toggleItem.Click += TrayToggle_Click;
                var settingsItem = new System.Windows.Controls.MenuItem { Header = "_Settings" };
                settingsItem.Click += TraySettings_Click;
                var exitItem = new System.Windows.Controls.MenuItem { Header = "E_xit" };
                exitItem.Click += TrayExit_Click;
                
                contextMenu.Items.Add(toggleItem);
                contextMenu.Items.Add(settingsItem);
                contextMenu.Items.Add(new System.Windows.Controls.Separator());
                contextMenu.Items.Add(exitItem);
                
                trayIcon.ContextMenu = contextMenu;
                
                // Set initial icon
                UpdateTrayIcon(micController.IsMuted);
                
                trayIcon.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tray initialization failed: {ex.Message}");
            }
        }

        private void UpdateUI()
        {
            // Force UI refresh by dispatching to UI thread
            Dispatcher.Invoke(() =>
            {
                // Update hotkey label
                var newText = $"Press {config.HotKeyDisplayName} to toggle, hold to mute/unmute temporarily";
                HotkeyLabel.Text = newText;
                
                // Update system info
                BrandLabel.Text = $"{config.DetectedBrand} {config.DetectedModel}";
                PlatformLabel.Text = GetDetailedWindowsVersion();
                
                // Update microphone status
                UpdateMicrophoneStatus();
                
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

        private string GetNAudioVersion()
        {
            try
            {
                var assembly = typeof(NAudio.CoreAudioApi.MMDevice).Assembly;
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
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
                        StatusText.Text = "Muted";
                        StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    }
                    else
                    {
                        StatusIcon.Symbol = Wpf.Ui.Controls.SymbolRegular.Mic24;
                        StatusIcon.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                        StatusText.Text = "Active";
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
                    StatusText.Text = "Error";
                }
                Debug.WriteLine($"Status update error: {ex.Message}");
            }
        }

        private string? GetDefaultMicrophoneName()
        {
            try
            {
                using var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Capture, NAudio.CoreAudioApi.Role.Communications);
                return device.FriendlyName;
            }
            catch
            {
                return null;
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
                
                trayIcon.ToolTipText = isMuted ? "MicControlX - Muted" : "MicControlX - Active";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tray icon update failed: {ex.Message}");
            }
        }
        #endregion

        #region Event Handlers
        private void OnMuteStateChanged(bool isMuted)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateMicrophoneStatus();

                if (config.ShowOSD && DateTime.Now.Subtract(lastOSDUpdate).TotalMilliseconds > 500)
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
                
                if (config?.ShowOSD == true)
                {
                    osd?.ShowMicrophoneStatus(isMuted);
                }
            });
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Register hotkey
            var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            hwndSource.AddHook(HwndHook);
            
            bool hotkeyRegistered = RegisterHotKey(new WindowInteropHelper(this).Handle, HOTKEY_ID, 0, config.HotKeyVirtualKey);
            if (!hotkeyRegistered)
            {
                var conflictInfo = ConfigurationManager.VirtualKeys.GetKeyConflictInfo(config.HotKeyVirtualKey);
                var alternatives = string.Join(", ", ConfigurationManager.VirtualKeys.GetSuggestedAlternatives(config.HotKeyVirtualKey));
                
                MessageBox.Show(
                    $"Failed to register the {config.HotKeyDisplayName} hotkey.\n\n" +
                    $"Reason: {conflictInfo}\n\n" +
                    $"Suggested alternatives: {alternatives}\n\n" +
                    $"You can:\n" +
                    $"• Change the hotkey in Settings\n" +
                    $"• Use the system tray icon to toggle the microphone\n" +
                    $"• Close applications that might be using {config.HotKeyDisplayName}",
                    "Hotkey Registration Failed", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Warning);
            }
            
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

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HandleHotkey();
                handled = true;
            }
            return IntPtr.Zero;
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
                if (config.ShowOSD)
                {
                    osd?.ShowMicrophoneStatus(!wasMutedBeforePTT);
                }
                
                // Show tray notification if enabled
                if (config.ShowNotifications)
                {
                    string title = "MicControlX";
                    string message = !wasMutedBeforePTT ? "Microphone Temporarily Muted" : "Microphone Temporarily Active";
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
                    if (config.ShowOSD)
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
                if (config.ShowOSD)
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
                if (config.ShowOSD)
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
                    if (config.ShowOSD)
                    {
                        osd?.ShowMicrophoneStatus(currentState);
                    }
                    
                    // Show tray notification if enabled
                    if (config.ShowNotifications)
                    {
                        string title = "MicControlX";
                        string message = currentState ? "Microphone Muted" : "Microphone Active";
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
                    UnregisterHotKey(new WindowInteropHelper(this).Handle, HOTKEY_ID);
                    bool hotkeyRegistered = RegisterHotKey(new WindowInteropHelper(this).Handle, HOTKEY_ID, 0, config.HotKeyVirtualKey);
                    if (!hotkeyRegistered)
                    {
                        var conflictInfo = ConfigurationManager.VirtualKeys.GetKeyConflictInfo(config.HotKeyVirtualKey);
                        var alternatives = string.Join(", ", ConfigurationManager.VirtualKeys.GetSuggestedAlternatives(config.HotKeyVirtualKey));
                        
                        MessageBox.Show(
                            $"Failed to register the new {config.HotKeyDisplayName} hotkey.\n\n" +
                            $"Reason: {conflictInfo}\n\n" +
                            $"Suggested alternatives: {alternatives}\n\n" +
                            $"The hotkey setting has been saved, but won't work until you:\n" +
                            $"• Choose a different hotkey in Settings\n" +
                            $"• Close other applications that might be using {config.HotKeyDisplayName}\n" +
                            $"• Use the system tray icon to toggle the microphone",
                            "Hotkey Registration Failed", 
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
                System.Windows.MessageBox.Show($"Settings error: {ex.Message}", "Error", 
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
                System.Windows.MessageBox.Show($"Error creating About window: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                
                // Unregister hotkey
                UnregisterHotKey(new WindowInteropHelper(this).Handle, HOTKEY_ID);
                
                // Dispose resources
                trayIcon?.Dispose();
                osd?.Close();
                micController?.Dispose();
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
