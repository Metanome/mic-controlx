using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Reflection;
using System.Diagnostics;

namespace MicControlX
{
    /// <summary>
    /// GitHub update checker for application updates
    /// </summary>
    public static class GitHubUpdateChecker
    {
        private static readonly HttpClient httpClient = new HttpClient();
        
        // Configure these for your actual GitHub repository
        private const string GITHUB_API_URL = "https://api.github.com/repos/Metanome/mic-controlx/releases/latest";
        private const string GITHUB_REPO_URL = "https://github.com/Metanome/mic-controlx";
        
        static GitHubUpdateChecker()
        {
            // Set user agent as required by GitHub API
            httpClient.DefaultRequestHeaders.Add("User-Agent", "MicControlX-UpdateChecker");
            httpClient.Timeout = TimeSpan.FromSeconds(10); // 10 second timeout
        }
        
        /// <summary>
        /// Check for updates asynchronously
        /// </summary>
        /// <returns>Update information or null if no update available or error occurred</returns>
        public static async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                var response = await httpClient.GetStringAsync(GITHUB_API_URL);
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);
                
                if (release?.tag_name == null) return null;
                
                var currentVersion = GetCurrentVersion();
                var latestVersion = ParseVersion(release.tag_name);
                
                if (latestVersion > currentVersion)
                {
                    return new UpdateInfo
                    {
                        LatestVersion = release.tag_name,
                        CurrentVersion = currentVersion.ToString(),
                        ReleaseUrl = release.html_url ?? GITHUB_REPO_URL,
                        DownloadUrl = GetDownloadUrl(release),
                        ReleaseNotes = release.body ?? "No release notes available.",
                        PublishedAt = release.published_at
                    };
                }
                
                return null; // No update available
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Network error checking for updates: {ex.Message}");
                return null;
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON parsing error: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error checking for updates: {ex.Message}");
                return null;
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
                        return asset.browser_download_url ?? release.html_url ?? GITHUB_REPO_URL;
                    }
                }
            }
            
            return release.html_url ?? GITHUB_REPO_URL;
        }
        
        /// <summary>
        /// Open the GitHub repository in the default browser
        /// </summary>
        public static void OpenGitHubRepository()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = GITHUB_REPO_URL,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open GitHub repository: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Open the download URL in the default browser
        /// </summary>
        public static void OpenDownloadUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open download URL: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Update information returned by the update checker
    /// </summary>
    public class UpdateInfo
    {
        public string LatestVersion { get; set; } = "";
        public string CurrentVersion { get; set; } = "";
        public string ReleaseUrl { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public DateTime? PublishedAt { get; set; }
        
        public string FormattedReleaseNotes
        {
            get
            {
                if (string.IsNullOrEmpty(ReleaseNotes)) return "No release notes available.";
                
                // Limit to first 500 characters for display
                return ReleaseNotes.Length > 500 
                    ? ReleaseNotes.Substring(0, 500) + "..." 
                    : ReleaseNotes;
            }
        }
    }
    
    /// <summary>
    /// GitHub API response models
    /// </summary>
    internal class GitHubRelease
    {
        public string? tag_name { get; set; }
        public string? name { get; set; }
        public string? html_url { get; set; }
        public string? body { get; set; }
        public DateTime published_at { get; set; }
        public GitHubAsset[]? assets { get; set; }
    }
    
    internal class GitHubAsset
    {
        public string? name { get; set; }
        public string? browser_download_url { get; set; }
    }
}
