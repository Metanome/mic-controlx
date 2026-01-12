using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MicControlX
{
    /// <summary>
    /// Enhanced microphone OSD with multiple visual styles
    /// WPF implementation with various aesthetic inspirations
    /// </summary>
    public partial class OsdOverlay : Window
    {
        private readonly OSDStyles _osdStyle;
        private readonly OSDPosition _position;
        private readonly double _durationSeconds;
        private readonly DispatcherTimer _hideTimer;
        private bool _isMuted;
        private HwndSource? _hwndSource;
        private const int WM_DISPLAYCHANGE = 0x007E;
        private const int WM_WININICHANGE = 0x001A;
        
        // P/Invoke for real-time taskbar detection
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        
        // Asset paths for Lenovo Vantage inspired style (layered icons)
        private const string MutedBorderPath = "pack://application:,,,/assets/icons/mic_mute-rounded_square.png";
        private const string MutedIconPath = "pack://application:,,,/assets/icons/mic_mute.png";
        private const string UnmutedBorderPath = "pack://application:,,,/assets/icons/mic_unmute-rounded_square.png";
        private const string UnmutedIconPath = "pack://application:,,,/assets/icons/mic_unmute.png";

        public OsdOverlay(OSDStyles style, OSDPosition position, double durationSeconds)
        {
            InitializeComponent();
            _osdStyle = style;
            _position = position;
            _durationSeconds = durationSeconds;
            
            // Initialize hide timer with configurable duration
            _hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_durationSeconds)
            };
            _hideTimer.Tick += HideTimer_Tick;
            
            InitializeOSD();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Hook into Windows messages for real-time display/taskbar changes
            var helper = new WindowInteropHelper(this);
            _hwndSource = HwndSource.FromHwnd(helper.Handle);
            _hwndSource.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Listen for display changes and taskbar state changes
            if (msg == WM_DISPLAYCHANGE || msg == WM_WININICHANGE)
            {
                // Reposition immediately when display settings or taskbar changes
                Dispatcher.BeginInvoke(() => PositionWindow());
            }
            return IntPtr.Zero;
        }

        private void InitializeOSD()
        {
            // Configure window properties
            WindowStartupLocation = WindowStartupLocation.Manual;
            
            // Set style-specific properties and positioning
            SetStyleProperties();
            
            // Position window on screen
            PositionWindow();
        }

        private void SetStyleProperties()
        {
            switch (_osdStyle)
            {
                case OSDStyles.DefaultStyle:
                    Width = 300;
                    Height = 80;
                    DefaultStyleContent.Visibility = Visibility.Visible;
                    VantageStyleContent.Visibility = Visibility.Collapsed;
                    LLTStyleContent.Visibility = Visibility.Collapsed;
                    ModernTranslucentStyleContent.Visibility = Visibility.Collapsed;
                    break;
                    
                case OSDStyles.VantageStyle:
                    Width = 160;
                    Height = 160;
                    DefaultStyleContent.Visibility = Visibility.Collapsed;
                    VantageStyleContent.Visibility = Visibility.Visible;
                    LLTStyleContent.Visibility = Visibility.Collapsed;
                    ModernTranslucentStyleContent.Visibility = Visibility.Collapsed;
                    break;
                    
                case OSDStyles.LLTStyle:
                    Width = 280;
                    Height = 64;
                    DefaultStyleContent.Visibility = Visibility.Collapsed;
                    VantageStyleContent.Visibility = Visibility.Collapsed;
                    LLTStyleContent.Visibility = Visibility.Visible;
                    ModernTranslucentStyleContent.Visibility = Visibility.Collapsed;
                    break;
                    
                case OSDStyles.TranslucentStyle:
                    Width = 300;
                    Height = 80;
                    DefaultStyleContent.Visibility = Visibility.Collapsed;
                    VantageStyleContent.Visibility = Visibility.Collapsed;
                    LLTStyleContent.Visibility = Visibility.Collapsed;
                    ModernTranslucentStyleContent.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void PositionWindow()
        {
            // Use WorkArea to respect taskbar and auto-hide settings
            var workArea = SystemParameters.WorkArea;
            const int cornerMargin = 20; // Reduced spacing for corners
            const int centerMargin = 60; // Keep original spacing for center positions
            
            // Use Width and Height instead of ActualWidth and ActualHeight since the window
            // hasn't been rendered yet when this method is called
            double windowWidth = Width;
            double windowHeight = Height;
            
            switch (_position)
            {
                case OSDPosition.TopLeft:
                    Left = workArea.Left + cornerMargin;
                    Top = workArea.Top + cornerMargin;
                    break;
                    
                case OSDPosition.TopCenter:
                    Left = workArea.Left + (workArea.Width - windowWidth) / 2;
                    Top = workArea.Top + centerMargin;
                    break;
                    
                case OSDPosition.TopRight:
                    Left = workArea.Right - windowWidth - cornerMargin;
                    Top = workArea.Top + cornerMargin;
                    break;
                    
                case OSDPosition.MiddleCenter:
                    Left = workArea.Left + (workArea.Width - windowWidth) / 2;
                    Top = workArea.Top + (workArea.Height - windowHeight) / 2;
                    break;
                    
                case OSDPosition.BottomLeft:
                    Left = workArea.Left + cornerMargin;
                    Top = workArea.Bottom - windowHeight - cornerMargin;
                    break;
                    
                case OSDPosition.BottomCenter:
                default:
                    Left = workArea.Left + (workArea.Width - windowWidth) / 2;
                    Top = workArea.Bottom - windowHeight - centerMargin;
                    break;
                    
                case OSDPosition.BottomRight:
                    Left = workArea.Right - windowWidth - cornerMargin;
                    Top = workArea.Bottom - windowHeight - cornerMargin;
                    break;
            }
            
            // Ensure the window stays within screen bounds as a safety measure
            EnsureOnScreen();
        }
        
        private void EnsureOnScreen()
        {
            var workArea = SystemParameters.WorkArea;
            
            // Use Width and Height instead of ActualWidth and ActualHeight
            double windowWidth = Width;
            double windowHeight = Height;
            
            // Clamp position to ensure window is fully visible
            if (Left < workArea.Left)
                Left = workArea.Left;
            if (Top < workArea.Top)
                Top = workArea.Top;
            if (Left + windowWidth > workArea.Right)
                Left = workArea.Right - windowWidth;
            if (Top + windowHeight > workArea.Bottom)
                Top = workArea.Bottom - windowHeight;
        }

        public void ShowMicrophoneStatus(bool isMuted)
        {
            _isMuted = isMuted;
            UpdateVisualState();
            
            // Stop any existing timers
            _hideTimer.Stop();
            
            // Show window with fade-in animation
            Show();
            BeginFadeIn();
            
            // Start hide timer
            _hideTimer.Start();
        }

        private void UpdateVisualState()
        {
            var statusColor = _isMuted 
                ? (SolidColorBrush)FindResource("MutedBrush") 
                : (SolidColorBrush)FindResource("UnmutedBrush");

            switch (_osdStyle)
            {
                case OSDStyles.DefaultStyle:
                    UpdateDefaultStyle(statusColor);
                    break;
                    
                case OSDStyles.VantageStyle:
                    UpdateVantageStyle();
                    break;
                    
                case OSDStyles.LLTStyle:
                    UpdateLLTStyle(statusColor);
                    break;
                    
                case OSDStyles.TranslucentStyle:
                    UpdateTranslucentStyle(statusColor);
                    break;
            }
        }

        private void UpdateDefaultStyle(SolidColorBrush statusColor)
        {
            DefaultStyleText.Text = _isMuted ? Strings.MicrophoneMuted : Strings.MicrophoneActive;
            DefaultStyleIndicator.Fill = statusColor;
        }

        private void UpdateVantageStyle()
        {
            try
            {
                // Lenovo Vantage inspired style uses layered icons:
                // - Rounded square as colored border/background
                // - Regular mic icon on top
                var borderPath = _isMuted ? MutedBorderPath : UnmutedBorderPath;
                var iconPath = _isMuted ? MutedIconPath : UnmutedIconPath;
                
                // Load border (rounded square background)
                var borderBitmap = new BitmapImage(new Uri(borderPath));
                VantageStyleBorder.Source = borderBitmap;
                
                // Load main icon (microphone on top)
                var iconBitmap = new BitmapImage(new Uri(iconPath));
                VantageStyleIcon.Source = iconBitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading Lenovo Vantage inspired style icons: {ex.Message}");
                // Fallback: hide both images
                VantageStyleBorder.Source = null;
                VantageStyleIcon.Source = null;
            }
        }

        private void UpdateLLTStyle(SolidColorBrush statusColor)
        {
            // Use Legion Toolkit inspired colors
            LLTStyleIndicator.Fill = new SolidColorBrush(_isMuted ? Color.FromRgb(255, 33, 33) : Color.FromRgb(0, 255, 0));
            LLTStyleText.Text = _isMuted ? Strings.Muted : Strings.Active;
        }

        private void UpdateTranslucentStyle(SolidColorBrush statusColor)
        {
            // Update the translucent style elements
            ModernTranslucentStyleIndicator.Fill = statusColor;
            ModernTranslucentStyleText.Text = _isMuted ? Strings.MicrophoneMuted : Strings.MicrophoneActive;
        }

        private void BeginFadeIn()
        {
            var fadeIn = (Storyboard)FindResource("FadeInAnimation");
            fadeIn.Begin(this);
        }

        private bool _fadeOutHandlerAdded = false;
        
        private void BeginFadeOut()
        {
            var fadeOut = (Storyboard)FindResource("FadeOutAnimation");
            if (!_fadeOutHandlerAdded)
            {
                fadeOut.Completed += FadeOut_Completed;
                _fadeOutHandlerAdded = true;
            }
            fadeOut.Begin(this);
        }
        
        private void FadeOut_Completed(object? sender, EventArgs e)
        {
            Hide();
        }

        private void HideTimer_Tick(object? sender, EventArgs e)
        {
            _hideTimer.Stop();
            BeginFadeOut();
        }

        public void ForceHide()
        {
            _hideTimer.Stop();
            Hide();
        }

        protected override void OnClosed(EventArgs e)
        {
            _hideTimer?.Stop();
            _hwndSource?.RemoveHook(WndProc);
            _hwndSource?.Dispose();
            base.OnClosed(e);
        }

    }
}
