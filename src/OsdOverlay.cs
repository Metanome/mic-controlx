using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace MicControlX
{
    /// <summary>
    /// OSD (On-Screen Display) styles for microphone status
    /// UI components are defined in OsdOverlay_UI.cs
    /// </summary>
    public enum LenovoOSDStyle
    {
        /// <summary>Windows Default - Universal clean style</summary>
        WindowsDefault,
        /// <summary>Lenovo Vantage Style - Transparent background with rounded border around icon</summary>
        LenovoStyle,
        /// <summary>Legion Toolkit Style - Dark background with icon + text (matches LLT notifications)</summary>
        LegionStyle
    }

    /// <summary>
    /// Theme modes for application UI (not OSD - OSD has fixed styling to match Lenovo/Legion overlays)
    /// </summary>
    public enum AppTheme
    {
        /// <summary>Follow system theme</summary>
        System,
        /// <summary>Always use dark theme</summary>
        Dark,
        /// <summary>Always use light theme</summary>
        Light
    }

    /// <summary>
    /// Enhanced microphone OSD with multiple visual styles for different laptop brands
    /// Specifically optimized for Lenovo gaming laptops (LOQ, Legion) but works universally
    /// Logic separated from UI components (see OsdOverlay_UI.cs)
    /// </summary>
    public partial class OsdOverlay : Form
    {
        private readonly System.Windows.Forms.Timer fadeTimer;
        private readonly System.Windows.Forms.Timer displayTimer;
        private int fadeStep = 0;
        private const int FADE_STEPS = 20;
        private const int DISPLAY_TIME = 1000; // 1 second like Legion Toolkit default (Normal duration)
        private LenovoOSDStyle _osdStyle = LenovoOSDStyle.WindowsDefault;
        private bool _isMuted = false;
        
        // Icon support - will be set externally by providing user-supplied icons
        private Image? _microphoneIcon = null;
        private Image? _microphoneMutedIcon = null;
        
        // Rounded square borders for Vantage style
        private Image? _roundedSquareIcon = null;
        private Image? _roundedSquareMutedIcon = null;
        
        // Fixed color schemes to match Lenovo/Legion overlays
        private readonly Color _mutedColor = Color.FromArgb(220, 53, 69);     // Bootstrap danger red
        private readonly Color _unmutedColor = Color.FromArgb(25, 135, 84);   // Bootstrap success green
        private readonly Color _darkBg = Color.FromArgb(240, 32, 32, 32);     // LLT dark background
        private readonly Color _darkText = Color.White;

        public OsdOverlay(LenovoOSDStyle style = LenovoOSDStyle.WindowsDefault)
        {
            _osdStyle = style;
            InitializeOSD();
            LoadDefaultIcons(); // Load PNG icons
            
            // Timer for auto-hide
            displayTimer = new System.Windows.Forms.Timer();
            displayTimer.Interval = DISPLAY_TIME;
            displayTimer.Tick += (s, e) => StartFadeOut();
            
            // Timer for fade effect
            fadeTimer = new System.Windows.Forms.Timer();
            fadeTimer.Interval = 20; // 50 FPS fade animation
            fadeTimer.Tick += FadeTimer_Tick;
        }

        /// <summary>
        /// Change the OSD style at runtime
        /// </summary>
        public void SetOSDStyle(LenovoOSDStyle style)
        {
            _osdStyle = style;
            InitializeOSD(); // Reinitialize with new style
            LoadDefaultIcons(); // Reload icons for new style (rounded vs regular)
        }
        
        /// <summary>
        /// Load default microphone icons from embedded PNG resources
        /// </summary>
        public void LoadDefaultIcons()
        {
            try
            {
                // Dispose existing icons to prevent memory leaks
                _microphoneIcon?.Dispose();
                _microphoneMutedIcon?.Dispose();
                _roundedSquareIcon?.Dispose();
                _roundedSquareMutedIcon?.Dispose();
                
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                
                // Load microphone icons (always load these)
                using var unmutedStream = assembly.GetManifestResourceStream("mic_unmute.png");
                if (unmutedStream != null)
                {
                    _microphoneIcon = Image.FromStream(unmutedStream);
                    System.Diagnostics.Debug.WriteLine("Loaded mic_unmute.png successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to load mic_unmute.png - resource not found");
                }
                
                using var mutedStream = assembly.GetManifestResourceStream("mic_mute.png");
                if (mutedStream != null)
                {
                    _microphoneMutedIcon = Image.FromStream(mutedStream);
                    System.Diagnostics.Debug.WriteLine("Loaded mic_mute.png successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to load mic_mute.png - resource not found");
                }
                
                // Load rounded square borders for Vantage style
                using var roundedUnmutedStream = assembly.GetManifestResourceStream("mic_unmute-rounded_square.png");
                if (roundedUnmutedStream != null)
                {
                    _roundedSquareIcon = Image.FromStream(roundedUnmutedStream);
                    System.Diagnostics.Debug.WriteLine("Loaded mic_unmute-rounded_square.png successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Rounded square unmute icon not found - this is normal for non-Vantage styles");
                }
                
                using var roundedMutedStream = assembly.GetManifestResourceStream("mic_mute-rounded_square.png");
                if (roundedMutedStream != null)
                {
                    _roundedSquareMutedIcon = Image.FromStream(roundedMutedStream);
                    System.Diagnostics.Debug.WriteLine("Loaded mic_mute-rounded_square.png successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Rounded square mute icon not found - this is normal for non-Vantage styles");
                }
                
                if (_microphoneIcon == null || _microphoneMutedIcon == null)
                {
                    System.Diagnostics.Debug.WriteLine("Basic microphone icons missing - loading fallback icons");
                    LoadFallbackIcons();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load embedded microphone icons: {ex.Message}");
                LoadFallbackIcons();
            }
        }
        
        /// <summary>
        /// Load fallback icons if SVG loading fails
        /// </summary>
        private void LoadFallbackIcons()
        {
            try
            {
                // Create simple fallback icons using text/shapes
                int iconSize = _osdStyle == LenovoOSDStyle.LenovoStyle ? 80 : 48;
                
                _microphoneIcon = CreateFallbackIcon("MIC", iconSize, Color.Green);
                _microphoneMutedIcon = CreateFallbackIcon("X", iconSize, Color.Red);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create fallback icons: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Create a simple fallback icon with text/emoji
        /// </summary>
        private Bitmap CreateFallbackIcon(string text, int size, Color color)
        {
            var bitmap = new Bitmap(size, size);
            using var graphics = Graphics.FromImage(bitmap);
            
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            
            using var brush = new SolidBrush(color);
            using var font = new Font("Segoe UI Emoji", size * 0.6f, FontStyle.Bold);
            
            var textSize = graphics.MeasureString(text, font);
            var x = (size - textSize.Width) / 2;
            var y = (size - textSize.Height) / 2;
            
            graphics.DrawString(text, font, brush, x, y);
            
            return bitmap;
        }
        
        /// <summary>
        /// Set microphone icons (provide both normal and muted versions)
        /// Icons should be PNG/JPG/ICO format, preferably 64x64 or 128x128 pixels
        /// </summary>
        public void SetIcons(Image microphoneIcon, Image microphoneMutedIcon)
        {
            _microphoneIcon?.Dispose();
            _microphoneMutedIcon?.Dispose();
            
            _microphoneIcon = microphoneIcon;
            _microphoneMutedIcon = microphoneMutedIcon;
        }
        
        /// <summary>
        /// Load microphone icons from file paths (PNG/JPG/ICO format, preferably 64x64 or 128x128 pixels)
        /// </summary>
        /// <param name="microphoneIconPath">Path to the normal microphone icon</param>
        /// <param name="microphoneMutedIconPath">Path to the muted microphone icon</param>
        public void LoadIconsFromFiles(string microphoneIconPath, string microphoneMutedIconPath)
        {
            try
            {
                // Dispose existing icons
                _microphoneIcon?.Dispose();
                _microphoneMutedIcon?.Dispose();
                
                // Load new icons
                if (File.Exists(microphoneIconPath))
                {
                    _microphoneIcon = Image.FromFile(microphoneIconPath);
                }
                
                if (File.Exists(microphoneMutedIconPath))
                {
                    _microphoneMutedIcon = Image.FromFile(microphoneMutedIconPath);
                }
            }
            catch (Exception ex)
            {
                // If loading fails, try to load default embedded icons
                System.Diagnostics.Debug.WriteLine($"Failed to load microphone icons from files: {ex.Message}");
                LoadDefaultIcons();
            }
        }
        
        /// <summary>
        /// Check if user-supplied icons are loaded
        /// </summary>
        public bool HasUserIcons => _microphoneIcon != null && _microphoneMutedIcon != null;

        public void ShowMicrophoneStatus(bool isMuted)
        {
            // Ensure we're on the UI thread
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(ShowMicrophoneStatus), isMuted);
                return;
            }

            try
            {
                _isMuted = isMuted;
                
                // Stop any existing timers first to prevent conflicts
                displayTimer.Stop();
                fadeTimer.Stop();
                
                // Update display based on style
                if (_osdStyle == LenovoOSDStyle.WindowsDefault)
                {
                    UpdateSimpleOSD(isMuted);
                }
                else
                {
                    // For Lenovo styles, trigger repaint
                    this.Invalidate();
                }
                
                // Reset fade state
                fadeStep = 0;
                this.Opacity = 0.9;
                
                // Force proper window state
                this.TopMost = true;
                this.WindowState = FormWindowState.Normal;
                this.Show();
                this.BringToFront();
                this.Activate();
                
                // Start the display timer for auto-hide
                displayTimer.Start();
            }
            catch (Exception ex)
            {
                // If OSD fails, don't crash the app
                System.Diagnostics.Debug.WriteLine($"OSD Error: {ex.Message}");
                HideOSD();
            }
        }

        private void HideOSD()
        {
            try
            {
                displayTimer.Stop();
                fadeTimer.Stop();
                this.Hide();
                fadeStep = 0;
                this.Opacity = 0.9; // Reset for next time
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OSD Hide Error: {ex.Message}");
            }
        }

        private void StartFadeOut()
        {
            displayTimer.Stop();
            
            // Ensure we're on the UI thread
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(StartFadeOut));
                return;
            }
            
            fadeTimer.Start();
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Ensure we're on the UI thread
                if (this.InvokeRequired)
                {
                    this.Invoke(new EventHandler(FadeTimer_Tick), sender, e);
                    return;
                }

                fadeStep++;
                double newOpacity = 0.9 * (1.0 - (double)fadeStep / FADE_STEPS);
                
                if (newOpacity <= 0.1 || fadeStep >= FADE_STEPS)
                {
                    fadeTimer.Stop();
                    HideOSD();
                }
                else
                {
                    this.Opacity = Math.Max(0.1, newOpacity); // Prevent negative opacity
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fade Timer Error: {ex.Message}");
                fadeTimer.Stop();
                HideOSD();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    displayTimer?.Stop();
                    fadeTimer?.Stop();
                    displayTimer?.Dispose();
                    fadeTimer?.Dispose();
                    
                    // Dispose icon resources
                    _microphoneIcon?.Dispose();
                    _microphoneMutedIcon?.Dispose();
                    _roundedSquareIcon?.Dispose();
                    _roundedSquareMutedIcon?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"OSD Dispose Error: {ex.Message}");
                }
            }
            base.Dispose(disposing);
        }

        // Force close method for emergency cleanup
        public void ForceClose()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(ForceClose));
                    return;
                }
                
                HideOSD();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OSD Force Close Error: {ex.Message}");
            }
        }
    }
}
