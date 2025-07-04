using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MicControlX
{
    /// <summary>
    /// UI rendering and visual components for the OSD Overlay
    /// </summary>
    public partial class OsdOverlay
    {
        /// <summary>
        /// OSD uses fixed themes to match actual Lenovo/Legion overlay appearance
        /// Legion style: always dark (matches LLT), Vantage style: transparent with colored border
        /// </summary>
        private bool ShouldUseDarkBackground()
        {
            // Legion style always uses dark background to match Legion Toolkit
            // Vantage style uses transparent background
            return _osdStyle == LenovoOSDStyle.LegionStyle;
        }

        private void InitializeOSD()
        {
            // Clear existing controls
            this.Controls.Clear();
            
            // Window properties - different for each style
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.AllowTransparency = true;
            
            // Set transparency based on style
            if (_osdStyle == LenovoOSDStyle.LenovoStyle)
            {
                // Vantage style: fully transparent background
                this.TransparencyKey = Color.Black;
                this.BackColor = Color.Black;
                this.Opacity = 0.9;
            }
            else if (_osdStyle == LenovoOSDStyle.LegionStyle)
            {
                // Legion style: completely opaque dark background matching LLT notifications exactly
                // Use a unique transparency key color that won't appear in our drawing
                this.TransparencyKey = Color.FromArgb(255, 0, 255, 0); // Bright green - won't appear in our pill
                this.BackColor = Color.FromArgb(255, 0, 255, 0); // Same bright green for transparent background
                this.Opacity = 1.0; // Completely opaque
                this.AllowTransparency = true; // Keep transparency enabled but use custom drawing
            }
            else
            {
                // Windows Default style: use magenta key
                this.TransparencyKey = Color.Magenta;
                this.BackColor = Color.Magenta;
                this.Opacity = 0.9;
            }
            
            // Set size and position based on style
            SetStyleProperties();
            
            // For simple styles, create a label. For advanced styles, use custom painting
            if (_osdStyle == LenovoOSDStyle.WindowsDefault)
            {
                CreateSimpleOSD();
            }
            else
            {
                // Enable custom painting for Lenovo styles (like LLT's UiWindow)
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                             ControlStyles.UserPaint | 
                             ControlStyles.DoubleBuffer | 
                             ControlStyles.ResizeRedraw, true);
            }
        }

        private void SetStyleProperties()
        {
            Size osdSize;
            Point osdPosition;
            var screen = Screen.PrimaryScreen?.WorkingArea ?? Screen.AllScreens[0].WorkingArea;
            
            // Use consistent positioning for all OSD styles (bottom-center above taskbar)
            const int bottomMargin = 60; // Distance from bottom of screen
            
            switch (_osdStyle)
            {
                case LenovoOSDStyle.LenovoStyle:
                    // Lenovo Vantage style - larger size to accommodate 128px border
                    osdSize = new Size(160, 160);
                    osdPosition = new Point(
                        screen.Left + (screen.Width - osdSize.Width) / 2, // Center horizontally
                        screen.Bottom - osdSize.Height - bottomMargin     // Position above taskbar
                    );
                    break;
                    
                case LenovoOSDStyle.LegionStyle:
                    // Legion Toolkit style - modern horizontal pill shape
                    osdSize = new Size(280, 64); // Horizontal pill dimensions
                    osdPosition = new Point(
                        screen.Left + (screen.Width - osdSize.Width) / 2, // Center horizontally
                        screen.Bottom - osdSize.Height - bottomMargin     // Position above taskbar
                    );
                    break;
                    
                default: // WindowsDefault
                    osdSize = new Size(300, 80);
                    osdPosition = new Point(
                        screen.Left + (screen.Width - osdSize.Width) / 2, // Center horizontally
                        screen.Bottom - osdSize.Height - bottomMargin     // Position above taskbar
                    );
                    break;
            }
            
            this.Size = osdSize;
            this.Location = osdPosition;
        }

        private void CreateSimpleOSD()
        {
            // Create main label for status (Windows Default style)
            Label statusLabel = new Label();
            statusLabel.Text = "Microphone Status";
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            statusLabel.Name = "OSDLabel";
            statusLabel.BackColor = Color.Black;
            statusLabel.ForeColor = Color.White;
            this.Controls.Add(statusLabel);
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Empty;
        }

        private void UpdateSimpleOSD(bool isMuted)
        {
            // Find and update the label for Windows Default style
            var label = this.Controls.Find("OSDLabel", false);
            if (label.Length > 0 && label[0] is Label osdLabel)
            {
                osdLabel.Text = isMuted ? "Microphone Muted" : "Microphone Active";
                osdLabel.ForeColor = isMuted ? Color.Red : Color.LimeGreen;
            }
        }

        /// <summary>
        /// Custom painting for clean, minimal OSD styles
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Only custom paint for Lenovo styles
            if (_osdStyle == LenovoOSDStyle.WindowsDefault)
                return;
                
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            
            // Draw based on selected style
            switch (_osdStyle)
            {
                case LenovoOSDStyle.LenovoStyle:
                    DrawLenovoStyle(graphics);
                    break;
                case LenovoOSDStyle.LegionStyle:
                    DrawLegionStyle(graphics);
                    break;
            }
        }

        private void DrawLenovoStyle(Graphics g)
        {
            // Lenovo Vantage style - use PNG rounded square border with microphone icon inside
            var iconToUse = _isMuted ? _microphoneMutedIcon : _microphoneIcon;
            var borderToUse = _isMuted ? _roundedSquareMutedIcon : _roundedSquareIcon;
            
            // Use exact native dimensions - NO SCALING for crisp rendering
            var borderSize = 128; // Your rounded square PNG native size
            var iconSize = 48;    // Your microphone icon PNG native size
            var centerX = this.Width / 2;
            var centerY = this.Height / 2;
            
            // Use minimal rendering settings to preserve PNG pixel quality
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.None;
            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.CompositingMode = CompositingMode.SourceCopy;
            
            // Draw the rounded square border UNSCALED at exact position
            if (borderToUse != null)
            {
                var borderX = centerX - borderSize / 2;
                var borderY = centerY - borderSize / 2;
                
                // Always use DrawImageUnscaled for pixel-perfect rendering
                g.DrawImageUnscaled(borderToUse, borderX, borderY);
            }
            
            // Draw the microphone icon UNSCALED on top/inside the border
            if (iconToUse != null)
            {
                var iconX = centerX - iconSize / 2;
                var iconY = centerY - iconSize / 2;
                
                // Always use DrawImageUnscaled for pixel-perfect rendering
                g.DrawImageUnscaled(iconToUse, iconX, iconY);
            }
            else
            {
                // Fallback: draw status text if no icon available
                var statusColor = _isMuted ? _mutedColor : _unmutedColor;
                using var font = new Font("Segoe UI", 14f, FontStyle.Regular);
                using var textBrush = new SolidBrush(statusColor);
                var message = _isMuted ? "MIC MUTED" : "MIC ACTIVE";
                var textSize = g.MeasureString(message, font);
                var textX = (this.Width - textSize.Width) / 2;
                var textY = (this.Height - textSize.Height) / 2;
                g.DrawString(message, font, textBrush, textX, textY);
            }
        }

        private void DrawLegionStyle(Graphics g)
        {
            // Legion Toolkit style - modern horizontal pill-shaped container
            var statusColor = _isMuted ? _mutedColor : _unmutedColor;
            
            // First, clear the entire background with transparency color
            g.Clear(Color.FromArgb(255, 0, 255, 0)); // Bright green - will be transparent
            
            // Modern pill container colors - exact match to LLT notification style
            var pillBgColor = Color.FromArgb(255, 47, 47, 47);      // LLT notification background (completely opaque)
            var pillBorderColor = Color.FromArgb(255, 64, 64, 64);  // LLT notification border (exact match)
            
            var rect = new Rectangle(0, 0, this.Width, this.Height);
            
            // Modern pill shape with smooth rounded end caps
            var pillRadius = this.Height / 2; // Half height for perfect pill shape
            
            // Fill the pill background - solid opaque
            using var bgBrush = new SolidBrush(pillBgColor);
            g.FillRoundedRectangle(bgBrush, rect, pillRadius);
            
            // Draw subtle border around the pill (matching LLT 3px border thickness)
            using var borderPen = new Pen(pillBorderColor, 2); // 2px for better visibility in WinForms
            var borderRect = new Rectangle(1, 1, this.Width - 3, this.Height - 3); // Adjust for border thickness
            g.DrawRoundedRectangle(borderPen, borderRect, pillRadius - 1);
            
            // Content layout with consistent padding
            var contentPadding = 12;
            var iconSize = 32; // Smaller icon for pill design
            var iconTextGap = 12;
            
            // Icon positioning (left side with padding)
            var iconX = contentPadding;
            var iconY = (this.Height - iconSize) / 2;
            
            // Text positioning (right of icon)
            var textX = iconX + iconSize + iconTextGap;
            var textY = 0;
            var textWidth = this.Width - textX - contentPadding;
            var textHeight = this.Height;
            
            // Draw microphone icon with pixel-perfect rendering
            var iconToUse = _isMuted ? _microphoneMutedIcon : _microphoneIcon;
            if (iconToUse != null)
            {
                // High-quality scaling for the smaller icon size
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                
                // Scale the icon to fit the pill design
                var iconRect = new Rectangle(iconX, iconY, iconSize, iconSize);
                g.DrawImage(iconToUse, iconRect);
            }
            else
            {
                // Fallback: draw colored circle
                using var iconBrush = new SolidBrush(statusColor);
                var iconRect = new Rectangle(iconX, iconY, iconSize, iconSize);
                g.FillEllipse(iconBrush, iconRect);
            }
            
            // Draw status text with modern styling
            var statusText = _isMuted ? "Microphone Off" : "Microphone On";
            using var font = new Font("Segoe UI", 12f, FontStyle.Regular); // Smaller font for pill
            
            // Use status color for text
            using var textBrush = new SolidBrush(statusColor);
            
            var textSize = g.MeasureString(statusText, font);
            var textYCentered = textY + (textHeight - textSize.Height) / 2;
            var drawRect = new RectangleF(textX, textYCentered, textWidth, textSize.Height);
            
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.DrawString(statusText, font, textBrush, drawRect);
        }
    }

    /// <summary>
    /// Extension methods for drawing rounded rectangles
    /// </summary>
    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle rect, int radius)
        {
            using var path = GetRoundedRectanglePath(rect, radius);
            graphics.FillPath(brush, path);
        }

        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle rect, int radius)
        {
            using var path = GetRoundedRectanglePath(rect, radius);
            graphics.DrawPath(pen, path);
        }

        private static GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
