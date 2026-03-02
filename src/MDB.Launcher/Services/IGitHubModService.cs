using MDB.Launcher.Models;

namespace MDB.Launcher.Services;

/// <summary>
/// Service for downloading mods from GitHub repositories.
/// </summary>
public interface IGitHubModService
{
    /// <summary>
    /// Fetch releases from a GitHub repository, filtering for DLL assets.
    /// </summary>
    /// <param name="owner">Repository owner.</param>
    /// <param name="repoName">Repository name.</param>
    /// <returns>List of releases with DLL assets.</returns>
    Task<List<GitHubModRelease>> GetReleasesAsync(string owner, string repoName);

    /// <summary>
    /// Parse a GitHub URL into owner and repo name.
    /// </summary>
    /// <param name="url">GitHub URL (e.g., https://github.com/owner/repo).</param>
    /// <param name="owner">Parsed owner.</param>
    /// <param name="repoName">Parsed repo name.</param>
    /// <returns>True if parsing succeeded.</returns>
    bool TryParseGitHubUrl(string url, out string owner, out string repoName);

    /// <summary>
    /// Download a mod asset to the specified directory.
    /// </summary>
    /// <param name="asset">The asset to download.</param>
    /// <param name="destinationDirectory">Target directory for the downloaded file.</param>
    /// <param name="progress">Progress reporter (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to the downloaded file.</returns>
    Task<string> DownloadAssetAsync(
        GitHubModAsset asset,
        string destinationDirectory,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Set a GitHub personal access token for higher API rate limits.
    /// </summary>
    void SetToken(string? token);

    /// <summary>
    /// Get the latest release asset info from a GitHub repository.
    /// Returns the download URL, file name, size, and tag name.
    /// </summary>
    Task<(string DownloadUrl, string FileName, long Size, string TagName)?> GetLatestReleaseAssetAsync(
        string owner, string repoName);

    /// <summary>
    /// Download a file from a URL to a local path with progress reporting.
    /// </summary>
    Task DownloadToFileAsync(
        string url,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
