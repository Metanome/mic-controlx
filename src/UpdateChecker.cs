using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Threading;

namespace MicControlX
{
    /// <summary>
    /// Update checker for application updates with download capabilities
    /// </summary>
    public static class UpdateChecker
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static CancellationTokenSource? downloadCancellationTokenSource;
        private static bool isUserCancellation = false;
        
        // Configure these for your actual GitHub repository
        private const string GITHUB_API_URL = "https://api.github.com/repos/Metanome/mic-controlx/releases/latest";
        private const string GITHUB_REPO_URL = "https://github.com/Metanome/mic-controlx";
        
        // Events for UI updates
        public static event EventHandler<DownloadProgressEventArgs>? DownloadProgressChanged;
        public static event EventHandler<DownloadCompletedEventArgs>? DownloadCompleted;
        
        static UpdateChecker()
        {
            // Set user agent as required by GitHub API
            httpClient.DefaultRequestHeaders.Add("User-Agent", "MicControlX-UpdateChecker");
            httpClient.Timeout = TimeSpan.FromSeconds(10); // Reasonable timeout for API calls
        }
        
        /// <summary>
        /// Check for updates asynchronously with detailed result information
        /// </summary>
        /// <returns>Update result with success/error information</returns>
        public static async Task<UpdateResult> CheckForUpdatesWithResultAsync()
        {
            try
            {
                var response = await httpClient.GetStringAsync(GITHUB_API_URL);
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);
                
                if (release?.tag_name == null) 
                    return UpdateResult.SuccessNoUpdate();
                
                var currentVersion = GetCurrentVersion();
                var latestVersion = ParseVersion(release.tag_name);
                
                if (latestVersion > currentVersion)
                {
                    var updateInfo = new UpdateInfo
                    {
                        LatestVersion = release.tag_name,
                        DownloadUrl = GetDownloadUrl(release)
                    };
                    Logger.Info($"Update available: {release.tag_name}");
                    return UpdateResult.SuccessWithUpdate(updateInfo);
                }
                
                Logger.Info("Update check: No updates available");
                return UpdateResult.SuccessNoUpdate();
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"Network error checking for updates: {ex.Message}");
                
                // Check for specific error types
                if (ex.Message.Contains("403") || ex.Message.Contains("API rate limit exceeded"))
                {
                    System.Diagnostics.Debug.WriteLine("GitHub API rate limit exceeded");
                    return UpdateResult.Error(Strings.UpdateErrorRateLimited, UpdateErrorType.RateLimited);
                }
                else if (ex.Message.Contains("404"))
                {
                    System.Diagnostics.Debug.WriteLine("Repository not found or not accessible");
                    return UpdateResult.Error(Strings.UpdateErrorNotFound, UpdateErrorType.NotFound);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("General network error occurred");
                    return UpdateResult.Error(Strings.UpdateErrorNetworkError, UpdateErrorType.NetworkError);
                }
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON parsing error: {ex.Message}");
                return UpdateResult.Error(Strings.UpdateErrorInvalidResponse, UpdateErrorType.Other);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error checking for updates: {ex.Message}");
                return UpdateResult.Error(ex.Message, UpdateErrorType.Other);
            }
        }
        /// <summary>
        /// Get current application version
        /// </summary>
        private static Version GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version ?? new Version(1, 0, 0, 0);
        }
        
        /// <summary>
        /// Parse version string from GitHub tag (e.g., "v1.2.3" -> Version(1,2,3))
        /// </summary>
        private static Version ParseVersion(string tagName)
        {
            try
            {
                // Remove 'v' prefix if present
                var versionString = tagName.StartsWith("v") ? tagName.Substring(1) : tagName;
                return new Version(versionString);
            }
            catch
            {
                return new Version(0, 0, 0, 0);
            }
        }
        
        /// <summary>
        /// Get download URL for the executable from release assets
        /// </summary>
        private static string GetDownloadUrl(GitHubRelease release)
        {
            if (release.assets != null)
            {
                foreach (var asset in release.assets)
                {
                    if (asset.name?.EndsWith(".exe") == true || 
                        asset.name?.Contains("MicControlX") == true)
                    {
                        return asset.browser_download_url ?? GITHUB_REPO_URL;
                    }
                }
            }
            
            return GITHUB_REPO_URL;
        }
        
        /// <summary>
        /// Download update file with progress reporting
        /// </summary>
        /// <param name="updateInfo">Update information containing download URL</param>
        /// <returns>Path to downloaded file or null if failed</returns>
        public static async Task<string?> DownloadUpdateAsync(UpdateInfo updateInfo)
        {
            try
            {
                // Reset user cancellation flag
                isUserCancellation = false;
                
                downloadCancellationTokenSource = new CancellationTokenSource();
                // Add 60 second timeout for downloads - allows for slower connections
                downloadCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(60));
                var cancellationToken = downloadCancellationTokenSource.Token;
                
                // Create updates directory in AppData (same location as config)
                var updatesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MicControlX", "Updates");
                Directory.CreateDirectory(updatesDir);
                
                // Generate filename from URL or use default
                var fileName = GetFileNameFromUrl(updateInfo.DownloadUrl) ?? "MicControlX.exe";
                var filePath = Path.Combine(updatesDir, fileName);
                
                // Delete existing file if it exists
                if (File.Exists(filePath))
                    File.Delete(filePath);
                
                using var response = await httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                
                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                
                var buffer = new byte[8192];
                long totalBytesRead = 0;
                int bytesRead;
                
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    totalBytesRead += bytesRead;
                    
                    // Report progress
                    var progressPercentage = totalBytes > 0 ? (int)((totalBytesRead * 100) / totalBytes) : 0;
                    DownloadProgressChanged?.Invoke(null, new DownloadProgressEventArgs
                    {
                        ProgressPercentage = progressPercentage,
                        BytesReceived = totalBytesRead,
                        TotalBytes = totalBytes
                    });
                }
                
                DownloadCompleted?.Invoke(null, new DownloadCompletedEventArgs
                {
                    Success = true,
                    FilePath = filePath,
                    Error = null
                });
                
                return filePath;
            }
            catch (OperationCanceledException)
            {
                // Check if it was user cancellation or timeout
                string errorMessage = isUserCancellation 
                    ? Strings.DownloadCancelledByUser
                    : Strings.DownloadErrorTimeout;
                    
                DownloadCompleted?.Invoke(null, new DownloadCompletedEventArgs
                {
                    Success = false,
                    FilePath = null,
                    Error = errorMessage
                });
                return null;
            }
            catch (HttpRequestException)
            {
                DownloadCompleted?.Invoke(null, new DownloadCompletedEventArgs
                {
                    Success = false,
                    FilePath = null,
                    Error = Strings.DownloadErrorNetwork
                });
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Download error: {ex.Message}");
                DownloadCompleted?.Invoke(null, new DownloadCompletedEventArgs
                {
                    Success = false,
                    FilePath = null,
                    Error = ex.Message
                });
                return null;
            }
        }
        
        /// <summary>
        /// Cancel ongoing download
        /// </summary>
        public static void CancelDownload()
        {
            isUserCancellation = true;
            downloadCancellationTokenSource?.Cancel();
        }
        
        /// <summary>
        /// Replace current executable and restart application
        /// </summary>
        /// <param name="updateFilePath">Path to downloaded update file</param>
        public static void ReplaceAndRestart(string updateFilePath)
        {
            try
            {
                var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(currentExePath))
                {
                    throw new InvalidOperationException("Could not determine current executable path");
                }
                
                // Create a batch file to handle the replacement and restart
                var batchContent = $@"
@echo off
timeout /t 2 /nobreak > nul
taskkill /f /im ""{Path.GetFileName(currentExePath)}"" > nul 2>&1
timeout /t 1 /nobreak > nul
move /y ""{updateFilePath}"" ""{currentExePath}""
start """" ""{currentExePath}""
del ""%~f0""
";
                
                var batchPath = Path.Combine(Path.GetTempPath(), "MicControlX_Update.bat");
                File.WriteAllText(batchPath, batchContent);
                
                // Start the batch file and exit current application
                Process.Start(new ProcessStartInfo
                {
                    FileName = batchPath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                
                // Exit current application
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File replacement error: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Extract filename from download URL
        /// </summary>
        private static string? GetFileNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var fileName = Path.GetFileName(uri.LocalPath);
                return string.IsNullOrEmpty(fileName) ? null : fileName;
            }
            catch
            {
                return null;
            }
        }
    }
    
    /// <summary>
    /// Result of update check operation
    /// </summary>
    public class UpdateResult
    {
        public bool Success { get; set; }
        public UpdateInfo? UpdateInfo { get; set; }
        public string? ErrorMessage { get; set; }
        public UpdateErrorType ErrorType { get; set; }
        
        public static UpdateResult SuccessWithUpdate(UpdateInfo updateInfo) => new()
        {
            Success = true,
            UpdateInfo = updateInfo,
            ErrorMessage = null,
            ErrorType = UpdateErrorType.None
        };
        
        public static UpdateResult SuccessNoUpdate() => new()
        {
            Success = true,
            UpdateInfo = null,
            ErrorMessage = null,
            ErrorType = UpdateErrorType.None
        };
        
        public static UpdateResult Error(string message, UpdateErrorType errorType) => new()
        {
            Success = false,
            UpdateInfo = null,
            ErrorMessage = message,
            ErrorType = errorType
        };
    }
    
    /// <summary>
    /// Types of update check errors
    /// </summary>
    public enum UpdateErrorType
    {
        None,
        RateLimited,
        NetworkError,
        NotFound,
        Other
    }
    
    /// <summary>
    /// Update information returned by the update checker
    /// </summary>
    public class UpdateInfo
    {
        public string LatestVersion { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
    }
    
    /// <summary>
    /// GitHub API response models
    /// </summary>
    internal class GitHubRelease
    {
        public string? tag_name { get; set; }
        public GitHubAsset[]? assets { get; set; }
    }
    
    internal class GitHubAsset
    {
        public string? name { get; set; }
        public string? browser_download_url { get; set; }
    }
    
    /// <summary>
    /// Event arguments for download progress updates
    /// </summary>
    public class DownloadProgressEventArgs : EventArgs
    {
        public int ProgressPercentage { get; set; }
        public long BytesReceived { get; set; }
        public long TotalBytes { get; set; }
        
        public string FormattedProgress => TotalBytes > 0 
            ? $"{BytesReceived / 1024 / 1024:F1} MB / {TotalBytes / 1024 / 1024:F1} MB ({ProgressPercentage}%)"
            : $"{BytesReceived / 1024 / 1024:F1} MB ({ProgressPercentage}%)";
    }
    
    /// <summary>
    /// Event arguments for download completion
    /// </summary>
    public class DownloadCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string? FilePath { get; set; }
        public string? Error { get; set; }
    }
}