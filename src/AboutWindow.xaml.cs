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
            LoadVersionInformation();
        }

        private void LoadVersionInformation()
        {
            try
            {
                // Get application version
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                VersionText.Text = $"Version {version?.ToString(3) ?? "1.0.0"}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading version information: {ex.Message}");
                VersionText.Text = "Version 1.0.0";
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
                CheckUpdatesButton.Content = "Checking...";

                // Use GitHubUpdateChecker static method
                var updateInfo = await GitHubUpdateChecker.CheckForUpdatesAsync();
                
                if (updateInfo != null)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"A new version {updateInfo.LatestVersion} is available!\n\nWould you like to download it?",
                        "Update Available",
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
                        "You are running the latest version!",
                        "No Updates",
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
                        "Could not check for updates. Please visit:\nhttps://github.com/Metanome/mic-controlx/releases",
                        "Update Check Failed",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
            }
            finally
            {
                // Re-enable button
                CheckUpdatesButton.IsEnabled = true;
                CheckUpdatesButton.Content = "Check for Updates";
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // FluentWindow handles styling automatically
        }
    }
}
