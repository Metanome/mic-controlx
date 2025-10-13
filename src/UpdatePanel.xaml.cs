using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MicControlX
{
    /// <summary>
    /// Update panel UI that slides down from the bottom to show update information and progress
    /// </summary>
    public partial class UpdatePanel : UserControl
    {
        private Storyboard? fadeInAnimation;
        private Storyboard? fadeOutAnimation;
        
        // Public properties for MainWindow access
        public UpdateInfo? currentUpdateInfo;
        public string? downloadedFilePath;
        public UpdatePanelState CurrentState { get; private set; } = UpdatePanelState.Checking;
        
        public UpdatePanel()
        {
            InitializeComponent();
            
            fadeInAnimation = FindResource("FadeInAnimation") as Storyboard;
            fadeOutAnimation = FindResource("FadeOutAnimation") as Storyboard;
            
            // Subscribe to UpdateChecker events
            UpdateChecker.DownloadProgressChanged += OnDownloadProgressChanged;
            UpdateChecker.DownloadCompleted += OnDownloadCompleted;
        }

        #region Events

        public event EventHandler? PanelClosed;
        public event EventHandler? UpdateRequested;
        public event EventHandler? DownloadRequested;
        public event EventHandler? RestartRequested;

        #endregion

        #region Public Methods

        /// <summary>
        /// Show the panel with checking state
        /// </summary>
        public void ShowChecking()
        {
            SetState(UpdatePanelState.Checking);
            ShowPanel();
        }

        /// <summary>
        /// Show update available state
        /// </summary>
        public void ShowUpdateAvailable(UpdateInfo updateInfo)
        {
            currentUpdateInfo = updateInfo;
            SetState(UpdatePanelState.UpdateAvailable);
            
            StatusText.Text = string.Format(Strings.UpdateAvailableMessage, updateInfo.LatestVersion);
            
            ShowPanel();
        }

        /// <summary>
        /// Show no updates available message
        /// </summary>
        public void ShowNoUpdates()
        {
            SetState(UpdatePanelState.NoUpdates);
            StatusText.Text = Strings.UpToDate;
            ShowPanel();
        }

        /// <summary>
        /// Show error state
        /// </summary>
        public void ShowError(string errorMessage)
        {
            // Use appropriate error prefix based on current context
            string errorFormat = CurrentState == UpdatePanelState.Downloading 
                ? Strings.DownloadFailed 
                : Strings.UpdateFailed;
                
            SetState(UpdatePanelState.Error);
            StatusText.Text = string.Format(errorFormat, errorMessage);
            ShowPanel();
        }

        /// <summary>
        /// Hide the panel
        /// </summary>
        public void HidePanel()
        {
            if (fadeOutAnimation != null)
            {
                fadeOutAnimation.Completed += (s, e) =>
                {
                    this.Visibility = Visibility.Collapsed;
                    PanelClosed?.Invoke(this, EventArgs.Empty);
                };
                fadeOutAnimation.Begin(this);
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
                PanelClosed?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Private Methods

        private void ShowPanel()
        {
            this.Visibility = Visibility.Visible;
            fadeInAnimation?.Begin(this);
        }

        private void SetState(UpdatePanelState state)
        {
            CurrentState = state; // Track current state for dismiss functionality
            
            // Hide all conditional elements first
            ProgressSection.Visibility = Visibility.Collapsed;
            DownloadButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;
            RestartButton.Visibility = Visibility.Collapsed;
            TryAgainButton.Visibility = Visibility.Collapsed;

            switch (state)
            {
                case UpdatePanelState.Checking:
                    StatusText.Text = Strings.CheckingForUpdates;
                    break;
                    
                case UpdatePanelState.UpdateAvailable:
                    DownloadButton.Visibility = Visibility.Visible;
                    break;
                    
                case UpdatePanelState.Downloading:
                    ProgressSection.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    StatusText.Text = Strings.DownloadingUpdate;
                    break;
                    
                case UpdatePanelState.ReadyToRestart:
                    RestartButton.Visibility = Visibility.Visible;
                    StatusText.Text = Strings.UpdateReadyToRestart;
                    break;
                    
                case UpdatePanelState.Error:
                    TryAgainButton.Visibility = Visibility.Visible;
                    break;
                    
                case UpdatePanelState.NoUpdates:
                    // StatusText is set by caller
                    break;
            }
        }

        #endregion

        #region Event Handlers

        private void UpdatePanel_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Stop event bubbling to prevent dismissal when clicking on the panel itself
            e.Handled = true;
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentUpdateInfo != null)
            {
                SetState(UpdatePanelState.Downloading);
                DownloadRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateChecker.CancelDownload();
            SetState(UpdatePanelState.UpdateAvailable);
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(downloadedFilePath))
            {
                RestartRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void TryAgainButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnDownloadProgressChanged(object? sender, DownloadProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = e.ProgressPercentage;
                ProgressText.Text = string.Format(Strings.DownloadingProgress, e.FormattedProgress);
            });
        }

        private void OnDownloadCompleted(object? sender, DownloadCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Success && !string.IsNullOrEmpty(e.FilePath))
                {
                    downloadedFilePath = e.FilePath;
                    SetState(UpdatePanelState.ReadyToRestart);
                }
                else
                {
                    ShowError(e.Error ?? Strings.DownloadFailed);
                }
            });
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            UpdateChecker.DownloadProgressChanged -= OnDownloadProgressChanged;
            UpdateChecker.DownloadCompleted -= OnDownloadCompleted;
        }

        #endregion
    }

    /// <summary>
    /// Update panel states
    /// </summary>
    public enum UpdatePanelState
    {
        Checking,
        UpdateAvailable,
        Downloading,
        ReadyToRestart,
        Error,
        NoUpdates
    }
}