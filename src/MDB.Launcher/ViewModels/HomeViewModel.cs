using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDB.Launcher.Models;
using MDB.Launcher.Services;
using Microsoft.Win32;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace MDB.Launcher.ViewModels;

/// <summary>
/// ViewModel for the Home view — profile management, game launching, and injection.
/// </summary>
public partial class HomeViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly IProfileService _profileService;
    private readonly IInjectorService _injectorService;
    private readonly IProcessElevationService _elevationService;
    private readonly IGitHubModService _gitHubModService;

    [ObservableProperty]
    private bool _hasProfile;

    [ObservableProperty]
    private bool _isInjecting;

    [ObservableProperty]
    private string _injectionStatus = string.Empty;

    [ObservableProperty]
    private bool _bridgeFound;

    [ObservableProperty]
    private bool _isIl2CppGame;

    [ObservableProperty]
    private string _bridgeStatusText = "No profile selected";

    [ObservableProperty]
    private bool _isDownloadingMdb;

    [ObservableProperty]
    private string _mdbDownloadStatus = string.Empty;

    [ObservableProperty]
    private double _mdbDownloadPercent;

    [ObservableProperty]
    private bool _mdbInstalled;

    public HomeViewModel(
        MainViewModel main,
        IProfileService profileService,
        IInjectorService injectorService,
        IProcessElevationService elevationService,
        IGitHubModService gitHubModService)
    {
        _main = main;
        _profileService = profileService;
        _injectorService = injectorService;
        _elevationService = elevationService;
        _gitHubModService = gitHubModService;
    }

    public void OnProfileChanged()
    {
        HasProfile = _main.ActiveProfile != null;
        UpdateBridgeStatus();
    }

    private void UpdateBridgeStatus()
    {
        var profile = _main.ActiveProfile;
        if (profile == null)
        {
            BridgeFound = false;
            IsIl2CppGame = false;
            MdbInstalled = false;
            BridgeStatusText = "No profile selected";
            return;
        }

        IsIl2CppGame = profile.IsIl2CppGame;
        BridgeFound = profile.BridgeExists;

        var mdbCoreExists = Directory.Exists(Path.Combine(profile.GameDirectory, "MDB_Core"));
        MdbInstalled = BridgeFound && mdbCoreExists;

        BridgeStatusText = (IsIl2CppGame, BridgeFound) switch
        {
            (false, _) => "⚠ GameAssembly.dll not found — may not be an IL2CPP game",
            (true, true) => "✓ MDB_Bridge.dll found",
            (true, false) => "✗ MDB_Bridge.dll not found — download or provide a custom path",
        };
    }

    [RelayCommand]
    private void AddProfile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Game Executable",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            FilterIndex = 1,
        };

        if (dialog.ShowDialog() == true)
        {
            var profile = _profileService.CreateProfile(dialog.FileName);

            if (!_profileService.IsIl2CppGame(dialog.FileName))
            {
                // Still allow adding — just warn
                _main.SetStatus("⚠ GameAssembly.dll not found beside this executable.");
            }

            _main.Profiles.Add(profile);
            _main.ActiveProfile = profile;
            _main.SetStatus($"Profile added: {profile.Name}");
            _ = _main.SaveConfigAsync();
        }
    }

    [RelayCommand]
    private void RemoveProfile()
    {
        var profile = _main.ActiveProfile;
        if (profile == null) return;

        _main.Profiles.Remove(profile);
        _main.ActiveProfile = _main.Profiles.FirstOrDefault();
        _main.SetStatus("Profile removed.");
        _ = _main.SaveConfigAsync();
    }

    [RelayCommand]
    private void BrowseBridgeDll()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select MDB_Bridge.dll",
            Filter = "DLL files (*.dll)|*.dll",
            FileName = "MDB_Bridge.dll",
        };

        if (dialog.ShowDialog() == true && _main.ActiveProfile != null)
        {
            _main.ActiveProfile.BridgeDllPath = dialog.FileName;
            _main.ActiveProfile.BridgePathOverridden = true;
            UpdateBridgeStatus();
            _main.SetStatus("Bridge path updated.");
            _ = _main.SaveConfigAsync();
        }
    }

    [RelayCommand]
    private async Task LaunchAndInjectAsync()
    {
        var profile = _main.ActiveProfile;
        if (profile == null) return;

        if (!ValidateForInjection(profile))
            return;

        // Check elevation
        if (!_elevationService.IsElevated)
        {
            _main.SetStatus("Requesting administrator privileges...");
            var args = $"--inject-launch \"{profile.GameExePath}\" \"{profile.BridgeDllPath}\" \"{profile.LaunchArguments}\"";
            if (!_elevationService.RelaunchElevated(args))
            {
                _main.SetStatus("Elevation cancelled by user.");
                return;
            }
            _main.SetStatus("Elevated process launched for injection.");
            return;
        }

        IsInjecting = true;
        InjectionStatus = "Starting...";

        try
        {
            var progress = new Progress<string>(msg =>
            {
                InjectionStatus = msg;
                _main.SetStatus(msg);
            });

            var result = await _injectorService.LaunchAndInjectAsync(
                profile.GameExePath,
                profile.BridgeDllPath,
                profile.LaunchArguments,
                _main.Config.InjectionPollIntervalMs,
                _main.Config.InjectionTimeoutSeconds,
                progress);

            InjectionStatus = result.Message;
            _main.SetStatus(result.Message);

            if (result.Success)
            {
                profile.LastUsedAt = DateTime.UtcNow;
                await _main.SaveConfigAsync();
            }
        }
        finally
        {
            IsInjecting = false;
        }
    }

    [RelayCommand]
    private async Task InjectIntoRunningAsync()
    {
        var profile = _main.ActiveProfile;
        if (profile == null) return;

        if (!File.Exists(profile.BridgeDllPath))
        {
            _main.SetStatus("MDB_Bridge.dll not found. Please configure the path.");
            return;
        }

        // Find all running instances of the game process
        var exeName = Path.GetFileNameWithoutExtension(profile.GameExePath);
        var matchingProcesses = Process.GetProcessesByName(exeName);

        if (matchingProcesses.Length == 0)
        {
            _main.SetStatus($"No running instances of {exeName} found.");
            return;
        }

        // Check elevation
        if (!_elevationService.IsElevated)
        {
            _main.SetStatus("Requesting administrator privileges...");
            // Pass all PIDs comma-separated
            var pids = string.Join(",", matchingProcesses.Select(p => p.Id));
            var args = $"--inject-attach {pids} \"{profile.BridgeDllPath}\"";
            if (!_elevationService.RelaunchElevated(args))
            {
                _main.SetStatus("Elevation cancelled by user.");
                return;
            }
            _main.SetStatus("Elevated process launched for injection.");
            return;
        }

        IsInjecting = true;
        InjectionStatus = $"Injecting into {matchingProcesses.Length} instance(s)...";

        try
        {
            var successCount = 0;
            var failCount = 0;

            foreach (var proc in matchingProcesses)
            {
                var progress = new Progress<string>(msg =>
                {
                    InjectionStatus = msg;
                    _main.SetStatus(msg);
                });

                var result = await _injectorService.InjectIntoProcessAsync(
                    proc.Id,
                    profile.BridgeDllPath,
                    progress);

                if (result.Success)
                    successCount++;
                else
                    failCount++;
            }

            var summary = $"Injection complete: {successCount} succeeded, {failCount} failed ({matchingProcesses.Length} total).";
            InjectionStatus = summary;
            _main.SetStatus(summary);

            if (successCount > 0)
            {
                profile.LastUsedAt = DateTime.UtcNow;
                await _main.SaveConfigAsync();
            }
        }
        finally
        {
            IsInjecting = false;
        }
    }

    [RelayCommand]
    private void OpenGameFolder()
    {
        var profile = _main.ActiveProfile;
        if (profile == null || string.IsNullOrEmpty(profile.GameDirectory)) return;

        if (Directory.Exists(profile.GameDirectory))
            Process.Start("explorer.exe", profile.GameDirectory);
    }

    [RelayCommand]
    private void OpenModsFolder()
    {
        var profile = _main.ActiveProfile;
        if (profile == null) return;

        if (Directory.Exists(profile.ModsDirectory))
            Process.Start("explorer.exe", profile.ModsDirectory);
        else
            _main.SetStatus("Mods directory does not exist yet. Launch the game with MDB first.");
    }

    private bool ValidateForInjection(GameProfile profile)
    {
        if (!profile.GameExeExists)
        {
            _main.SetStatus($"Game executable not found: {profile.GameExePath}");
            return false;
        }

        if (!profile.BridgeExists)
        {
            _main.SetStatus("MDB_Bridge.dll not found. Please configure the path.");
            return false;
        }

        return true;
    }

    [RelayCommand]
    private async Task DownloadMdbAsync()
    {
        var profile = _main.ActiveProfile;
        if (profile == null || string.IsNullOrEmpty(profile.GameDirectory)) return;

        IsDownloadingMdb = true;
        MdbDownloadPercent = 0;
        MdbDownloadStatus = "Checking for latest release...";

        try
        {
            var releaseInfo = await _gitHubModService.GetLatestReleaseAssetAsync("Zaclin-GIT", "MDB");
            if (releaseInfo == null)
            {
                MdbDownloadStatus = "✗ Failed to fetch release info from GitHub.";
                _main.SetStatus("Failed to fetch MDB release from GitHub.");
                return;
            }

            var (downloadUrl, fileName, size, tagName) = releaseInfo.Value;
            MdbDownloadStatus = $"Downloading {fileName} ({FormatSize(size)})...";

            var tempFile = Path.Combine(Path.GetTempPath(), fileName);
            try
            {
                var progress = new Progress<double>(pct =>
                {
                    MdbDownloadPercent = pct * 100;
                    MdbDownloadStatus = $"Downloading... {pct:P0}";
                });

                await _gitHubModService.DownloadToFileAsync(downloadUrl, tempFile, progress);

                MdbDownloadStatus = "Extracting to game directory...";
                MdbDownloadPercent = 100;

                // Extract to temp directory first
                var tempExtractDir = Path.Combine(
                    Path.GetTempPath(),
                    "MDB_Extract_" + Guid.NewGuid().ToString("N")[..8]);
                Directory.CreateDirectory(tempExtractDir);

                try
                {
                    using (var stream = File.OpenRead(tempFile))
                    using (var archive = ArchiveFactory.Open(stream))
                    {
                        foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                        {
                            entry.WriteToDirectory(tempExtractDir, new ExtractionOptions
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                    }

                    // Find MDB_Bridge.dll in extracted files
                    var bridgeFile = Directory
                        .GetFiles(tempExtractDir, "MDB_Bridge.dll", SearchOption.AllDirectories)
                        .FirstOrDefault();

                    if (bridgeFile == null)
                    {
                        MdbDownloadStatus = "✗ MDB_Bridge.dll not found in the release archive.";
                        _main.SetStatus("MDB_Bridge.dll not found in downloaded archive.");
                        return;
                    }

                    // The directory containing MDB_Bridge.dll is the MDB root
                    var sourceRoot = Path.GetDirectoryName(bridgeFile)!;

                    // Copy everything from source root to game directory
                    CopyDirectory(sourceRoot, profile.GameDirectory);

                    // Update bridge path on the profile
                    var bridgeInGame = Path.Combine(profile.GameDirectory, "MDB_Bridge.dll");
                    if (File.Exists(bridgeInGame))
                    {
                        profile.BridgeDllPath = bridgeInGame;
                        profile.BridgePathOverridden = false;
                    }

                    UpdateBridgeStatus();
                    MdbDownloadStatus = $"✓ MDB {tagName} installed successfully!";
                    _main.SetStatus($"MDB Framework {tagName} installed to {profile.GameDirectory}");
                    await _main.SaveConfigAsync();
                }
                finally
                {
                    try { Directory.Delete(tempExtractDir, true); } catch { }
                }
            }
            finally
            {
                try { File.Delete(tempFile); } catch { }
            }
        }
        catch (Exception ex)
        {
            MdbDownloadStatus = $"✗ Download failed: {ex.Message}";
            _main.SetStatus($"MDB download failed: {ex.Message}");
        }
        finally
        {
            IsDownloadingMdb = false;
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };
}
