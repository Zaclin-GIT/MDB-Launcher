using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDB.Launcher.Models;
using MDB.Launcher.Services;

namespace MDB.Launcher.ViewModels;

/// <summary>
/// ViewModel for the Download view — fetch and install mods from GitHub repos.
/// </summary>
public partial class ModDownloadViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly IGitHubModService _gitHubModService;

    [ObservableProperty]
    private string _repoUrl = string.Empty;

    [ObservableProperty]
    private ObservableCollection<GitHubModRelease> _releases = [];

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private string _searchError = string.Empty;

    [ObservableProperty]
    private bool _hasProfile;

    public ModDownloadViewModel(MainViewModel main, IGitHubModService gitHubModService)
    {
        _main = main;
        _gitHubModService = gitHubModService;
    }

    public void OnProfileChanged()
    {
        HasProfile = _main.ActiveProfile != null;
    }

    [RelayCommand]
    private async Task SearchReleasesAsync()
    {
        SearchError = string.Empty;
        Releases.Clear();

        if (string.IsNullOrWhiteSpace(RepoUrl))
        {
            SearchError = "Please enter a GitHub repository URL.";
            return;
        }

        if (!_gitHubModService.TryParseGitHubUrl(RepoUrl, out var owner, out var repoName))
        {
            SearchError = "Invalid GitHub URL. Expected format: https://github.com/owner/repo";
            return;
        }

        IsSearching = true;

        try
        {
            var releases = await _gitHubModService.GetReleasesAsync(owner, repoName);

            if (releases.Count == 0)
            {
                SearchError = "No releases with .dll assets found in this repository.";
                return;
            }

            Releases = new ObservableCollection<GitHubModRelease>(releases);
            _main.SetStatus($"Found {releases.Count} release(s) with mod DLLs from {owner}/{repoName}");
        }
        catch (Octokit.NotFoundException)
        {
            SearchError = "Repository not found. Check the URL and try again.";
        }
        catch (Octokit.RateLimitExceededException)
        {
            SearchError = "GitHub API rate limit exceeded. Add a token in Settings to increase the limit.";
        }
        catch (Exception ex)
        {
            SearchError = $"Error: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task DownloadAssetAsync(GitHubModAsset? asset)
    {
        if (asset == null) return;

        var profile = _main.ActiveProfile;
        if (profile == null)
        {
            _main.SetStatus("No active profile. Select a game profile first.");
            return;
        }

        // Ensure mods directory exists
        Directory.CreateDirectory(profile.ModsDirectory);

        IsDownloading = true;
        DownloadProgress = 0;

        try
        {
            var progress = new Progress<double>(p => DownloadProgress = p);

            var downloadedPath = await _gitHubModService.DownloadAssetAsync(
                asset,
                profile.ModsDirectory,
                progress);

            _main.SetStatus($"Downloaded {asset.FileName} to {profile.ModsDirectory}");
        }
        catch (Exception ex)
        {
            _main.SetStatus($"Download failed: {ex.Message}");
        }
        finally
        {
            IsDownloading = false;
            DownloadProgress = 0;
        }
    }
}
