using System.Net.Http;
using System.Text.RegularExpressions;
using MDB.Launcher.Models;
using Octokit;

namespace MDB.Launcher.Services;

/// <summary>
/// Service for fetching and downloading mods from GitHub repositories via the Octokit API.
/// </summary>
public partial class GitHubModService : IGitHubModService
{
    private GitHubClient _client;
    private readonly HttpClient _httpClient;

    public GitHubModService()
    {
        _client = new GitHubClient(new ProductHeaderValue("MDB-Launcher", "1.0.0"));
        _httpClient = new HttpClient();
    }

    public void SetToken(string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            _client.Credentials = new Credentials(token);
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _client.Credentials = Credentials.Anonymous;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public bool TryParseGitHubUrl(string url, out string owner, out string repoName)
    {
        owner = string.Empty;
        repoName = string.Empty;

        if (string.IsNullOrWhiteSpace(url))
            return false;

        var match = GitHubUrlRegex().Match(url.Trim());
        if (!match.Success)
            return false;

        owner = match.Groups["owner"].Value;
        repoName = match.Groups["repo"].Value;
        return true;
    }

    [GeneratedRegex(@"^https?://github\.com/(?<owner>[^/]+)/(?<repo>[^/\s]+?)(?:\.git)?/?$",
        RegexOptions.IgnoreCase)]
    private static partial Regex GitHubUrlRegex();

    public async Task<List<GitHubModRelease>> GetReleasesAsync(string owner, string repoName)
    {
        var releases = await _client.Repository.Release.GetAll(owner, repoName);
        var result = new List<GitHubModRelease>();

        foreach (var release in releases)
        {
            var dllAssets = release.Assets
                .Where(a => a.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Select(a => new GitHubModAsset
                {
                    FileName = a.Name,
                    DownloadUrl = a.BrowserDownloadUrl,
                    Size = a.Size,
                    ContentType = a.ContentType,
                    DownloadCount = a.DownloadCount
                })
                .ToList();

            // Only include releases that have DLL assets
            if (dllAssets.Count > 0)
            {
                result.Add(new GitHubModRelease
                {
                    Owner = owner,
                    RepoName = repoName,
                    TagName = release.TagName,
                    ReleaseName = release.Name ?? release.TagName,
                    Body = release.Body ?? string.Empty,
                    PublishedAt = release.PublishedAt,
                    IsPreRelease = release.Prerelease,
                    Assets = dllAssets
                });
            }
        }

        return result;
    }

    public async Task<string> DownloadAssetAsync(
        GitHubModAsset asset,
        string destinationDirectory,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(destinationDirectory);

        var destinationPath = Path.Combine(destinationDirectory, asset.FileName);

        using var response = await _httpClient.GetAsync(
            asset.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? asset.Size;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(
            destinationPath,
            System.IO.FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 8192,
            useAsync: true);

        var buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;

            if (totalBytes > 0)
                progress?.Report((double)totalRead / totalBytes);
        }

        progress?.Report(1.0);
        return destinationPath;
    }

    public async Task<(string DownloadUrl, string FileName, long Size, string TagName)?> GetLatestReleaseAssetAsync(
        string owner, string repoName)
    {
        try
        {
            var release = await _client.Repository.Release.GetLatest(owner, repoName);
            if (release.Assets.Count == 0)
                return null;

            var asset = release.Assets[0];
            return (asset.BrowserDownloadUrl, asset.Name, asset.Size, release.TagName);
        }
        catch
        {
            return null;
        }
    }

    public async Task DownloadToFileAsync(
        string url,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(
            destinationPath,
            System.IO.FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 8192,
            useAsync: true);

        var buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;

            if (totalBytes > 0)
                progress?.Report((double)totalRead / totalBytes);
        }

        progress?.Report(1.0);
    }
}
