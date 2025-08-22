using System;
using System.Windows;
using System.Windows.Controls;
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
        private readonly DispatcherTimer _hideTimer;
        private bool _isMuted;
        
        // Asset paths for Lenovo Vantage inspired style (layered icons)
        private const string MutedBorderPath = "pack://application:,,,/assets/icons/mic_mute-rounded_square.png";
        private const string MutedIconPath = "pack://application:,,,/assets/icons/mic_mute.png";
        private const string UnmutedBorderPath = "pack://application:,,,/assets/icons/mic_unmute-rounded_square.png";
        private const string UnmutedIconPath = "pack://application:,,,/assets/icons/mic_unmute.png";

        public OsdOverlay(OSDStyles style)
        {
            InitializeComponent();
            _osdStyle = style;
            
            // Initialize hide timer
            _hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _hideTimer.Tick += HideTimer_Tick;
            
            InitializeOSD();
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
                case OSDStyles.WindowsDefault:
                    Width = 300;
                    Height = 80;
                    WindowsDefaultContent.Visibility = Visibility.Visible;
                    VantageStyleContent.Visibility = Visibility.Collapsed;
                    LLTStyleContent.Visibility = Visibility.Collapsed;
                    break;
                    
                case OSDStyles.VantageStyle:
                    Width = 160;
                    Height = 160;
                    WindowsDefaultContent.Visibility = Visibility.Collapsed;
                    VantageStyleContent.Visibility = Visibility.Visible;
                    LLTStyleContent.Visibility = Visibility.Collapsed;
                    break;
                    
                case OSDStyles.LLTStyle:
                    Width = 280;
                    Height = 64;
                    WindowsDefaultContent.Visibility = Visibility.Collapsed;
                    VantageStyleContent.Visibility = Visibility.Collapsed;
                    LLTStyleContent.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void PositionWindow()
        {
            // Position at bottom-center of primary screen
            var screen = SystemParameters.WorkArea;
            const int bottomMargin = 60;
            
            Left = screen.Left + (screen.Width - Width) / 2;
            Top = screen.Bottom - Height - bottomMargin;
        }

        public void ShowMicrophoneStatus(bool isMuted)
        {
            _isMuted = isMuted;
            UpdateVisualState();
            
            // Stop any existing timer
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
                case OSDStyles.WindowsDefault:
                    UpdateWindowsDefaultStyle(statusColor);
                    break;
                    
                case OSDStyles.VantageStyle:
                    UpdateVantageStyle();
                    break;
                    
                case OSDStyles.LLTStyle:
                    UpdateLLTStyle(statusColor);
                    break;
            }
        }

        private void UpdateWindowsDefaultStyle(SolidColorBrush statusColor)
        {
            WindowsDefaultText.Text = _isMuted ? "Microphone Muted" : "Microphone Active";
            WindowsDefaultIndicator.Fill = statusColor;
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
            var indicator = LLTStyleContent.FindName("LLTStyleIndicator") as Ellipse;
            if (indicator != null)
            {
                indicator.Fill = new SolidColorBrush(_isMuted ? Color.FromRgb(255, 33, 33) : Color.FromRgb(0, 255, 0));
            }

            var statusText = LLTStyleContent.FindName("LLTStyleText") as TextBlock;
            if (statusText != null)
            {
                statusText.Text = _isMuted ? "MUTED" : "ACTIVE";
            }
        }

        private void BeginFadeIn()
        {
            var fadeIn = (Storyboard)FindResource("FadeInAnimation");
            fadeIn.Begin(this);
        }

        private void BeginFadeOut()
        {
            var fadeOut = (Storyboard)FindResource("FadeOutAnimation");
            fadeOut.Completed += (s, e) => Hide();
            fadeOut.Begin(this);
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
            base.OnClosed(e);
        }
    }
}
