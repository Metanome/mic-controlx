using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using Wpf.Ui.Controls;

namespace MicControlX
{
    public partial class AboutWindow : FluentWindow
    {
        public AboutWindow()
        {
            InitializeComponent();
            
            // Subscribe to language changes
            LocalizationManager.LanguageChanged += OnLanguageChanged;
            
            LoadVersionInformation();
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            // Reload version information with new language
            Dispatcher.Invoke(() =>
            {
                LoadVersionInformation();
            });
        }

        private void LoadVersionInformation()
        {
            try
            {
                // Get application version
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                var versionString = version?.ToString(3) ?? "1.0.0";
                // Use the localized string format
                VersionText.Text = string.Format(Strings.VersionTemplate, versionString);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading version information: {ex.Message}");
                VersionText.Text = Strings.Version;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from language changes to prevent memory leaks
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
            base.OnClosed(e);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // FluentWindow handles styling automatically
        }
    }
}
