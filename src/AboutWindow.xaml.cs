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

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disable button during check
                CheckUpdatesButton.IsEnabled = false;
                CheckUpdatesButton.Content = Strings.Checking;

                // Use GitHubUpdateChecker static method
                var updateInfo = await GitHubUpdateChecker.CheckForUpdatesAsync();
                
                if (updateInfo != null)
                {
                    var result = System.Windows.MessageBox.Show(
                        string.Format(Strings.UpdateAvailableMessage, updateInfo.LatestVersion),
                        Strings.UpdateAvailableTitle,
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Information);

                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        GitHubUpdateChecker.OpenDownloadUrl(updateInfo.ReleaseUrl);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        Strings.NoUpdatesMessage,
                        Strings.NoUpdatesTitle,
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking for updates: {ex.Message}");
                
                // Fallback: open releases page
                try
                {
                    GitHubUpdateChecker.OpenGitHubRepository();
                }
                catch
                {
                    System.Windows.MessageBox.Show(
                        Strings.UpdateCheckFailedMessage,
                        Strings.UpdateCheckFailedTitle,
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
            }
            finally
            {
                // Re-enable button
                CheckUpdatesButton.IsEnabled = true;
                CheckUpdatesButton.Content = Strings.CheckForUpdates;
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
