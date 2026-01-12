using System;
using System.Threading;
using System.Windows;

namespace MicControlX
{
    /// <summary>
    /// WPF Application class with single-instance enforcement
    /// </summary>
    public partial class App : Application
    {
        private static Mutex? applicationMutex;
        private const string APP_MUTEX_NAME = "MicControlX_SingleInstance_Mutex";

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {                
                // Initialize logging first
                Logger.Initialize();
                
                // Single instance check
                bool isNewInstance;
                applicationMutex = new Mutex(true, APP_MUTEX_NAME, out isNewInstance);

                if (!isNewInstance)
                {
                    // Another instance is already running
                    MessageBox.Show(Strings.AppAlreadyRunning, 
                        Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                    Current.Shutdown();
                    return;
                }

                // Load configuration and apply theme BEFORE base.OnStartup
                var config = ConfigurationManager.LoadConfiguration();
                
                // Initialize localization
                LocalizationManager.Initialize(config.Language);
                
                ThemeManager.ApplyTheme(config.Theme);

                // Call base startup
                base.OnStartup(e);

                // Create main window
                var mainWindow = new MainWindow();
                MainWindow = mainWindow;
                
                // Check if we should start minimized (from Windows startup)
                bool startMinimized = e.Args.Contains("--minimized") || e.Args.Contains("/minimized");
                
                if (startMinimized)
                {
                    // Start minimized to tray - don't show window
                    mainWindow.WindowState = WindowState.Minimized;
                    mainWindow.Hide();
                }
                else
                {
                    // Normal startup - show window
                    mainWindow.Show();
                    // Ensure window is visible and focused
                    mainWindow.Activate();
                    mainWindow.Focus();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in OnStartup: {ex.Message}");
                MessageBox.Show($"Application startup failed:\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    Strings.StartupError, MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            applicationMutex?.ReleaseMutex();
            applicationMutex?.Dispose();
            base.OnExit(e);
        }
    }
}
