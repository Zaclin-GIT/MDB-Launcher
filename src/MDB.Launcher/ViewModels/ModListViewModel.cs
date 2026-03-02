using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDB.Launcher.Models;
using MDB.Launcher.Services;

namespace MDB.Launcher.ViewModels;

/// <summary>
/// ViewModel for the Mods view — lists installed mods with enable/disable/delete controls.
/// </summary>
public partial class ModListViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly IModDiscoveryService _modDiscoveryService;

    [ObservableProperty]
    private ObservableCollection<ModEntry> _mods = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _modCountText = "No mods found";

    [ObservableProperty]
    private bool _hasProfile;

    [ObservableProperty]
    private bool _hasAnyProfiles;

    [ObservableProperty]
    private string _activeGameName = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ModEntry> _filteredMods = [];

    [ObservableProperty]
    private string _newModName = string.Empty;

    [ObservableProperty]
    private string _newModOutputDir = string.Empty;

    [ObservableProperty]
    private bool _isCreatingMod;

    [ObservableProperty]
    private string _createModStatus = string.Empty;

    public ModListViewModel(MainViewModel main, IModDiscoveryService modDiscoveryService)
    {
        _main = main;
        _modDiscoveryService = modDiscoveryService;
    }

    public void OnProfileChanged()
    {
        HasProfile = _main.ActiveProfile != null;
        HasAnyProfiles = _main.Profiles.Count > 0;
        ActiveGameName = _main.ActiveProfile?.Name ?? string.Empty;
        if (HasProfile)
            _ = RefreshModsAsync();
        else
        {
            Mods.Clear();
            FilteredMods.Clear();
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredMods = new ObservableCollection<ModEntry>(Mods);
        }
        else
        {
            var filtered = Mods.Where(m =>
                m.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                m.Author.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                m.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            FilteredMods = new ObservableCollection<ModEntry>(filtered);
        }
    }

    [RelayCommand]
    public async Task RefreshModsAsync()
    {
        var profile = _main.ActiveProfile;
        if (profile == null) return;

        IsLoading = true;

        try
        {
            if (!Directory.Exists(profile.ModsDirectory))
            {
                Mods.Clear();
                ModCountText = "Mods directory not found. Launch the game with MDB first.";
                return;
            }

            var discoveredMods = await _modDiscoveryService.DiscoverModsAsync(
                profile.ModsDirectory, profile.ManagedDirectory);
            Mods = new ObservableCollection<ModEntry>(discoveredMods);
            ApplyFilter();

            var enabledCount = discoveredMods.Count(m => m.IsEnabled);
            var totalCount = discoveredMods.Count;
            ModCountText = totalCount == 0
                ? "No mods found"
                : $"{enabledCount} of {totalCount} mod{(totalCount == 1 ? "" : "s")} enabled";

            _main.SetStatus($"Found {totalCount} mod(s) in {profile.ModsDirectory}");
        }
        catch (Exception ex)
        {
            _main.SetStatus($"Error scanning mods: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleModAsync(ModEntry? mod)
    {
        if (mod == null) return;

        bool success;
        if (mod.IsEnabled)
            success = _modDiscoveryService.DisableMod(mod);
        else
            success = _modDiscoveryService.EnableMod(mod);

        if (success)
        {
            _main.SetStatus($"{mod.Name} {(mod.IsEnabled ? "enabled" : "disabled")}.");
            await RefreshModsAsync();
        }
        else
        {
            _main.SetStatus($"Failed to toggle {mod.Name}.");
        }
    }

    [RelayCommand]
    private void BrowseOutputDir()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select output folder for your new mod project",
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            NewModOutputDir = dialog.SelectedPath;
        }
    }

    [RelayCommand]
    private async Task CreateModFromTemplateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewModName))
        {
            CreateModStatus = "✗ Please enter a mod name.";
            return;
        }

        // Sanitize mod name — only allow valid C# identifier characters
        var sanitized = new string(NewModName.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        if (string.IsNullOrEmpty(sanitized))
        {
            CreateModStatus = "✗ Mod name must contain at least one letter or digit.";
            return;
        }

        var outputDir = string.IsNullOrWhiteSpace(NewModOutputDir)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : NewModOutputDir;

        var targetDir = Path.Combine(outputDir, sanitized);

        if (Directory.Exists(targetDir) && Directory.EnumerateFileSystemEntries(targetDir).Any())
        {
            CreateModStatus = $"✗ Directory already exists: {targetDir}";
            return;
        }

        IsCreatingMod = true;
        CreateModStatus = "Cloning template...";

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"clone -b mdbmod https://github.com/Zaclin-GIT/MDB.Templates.git \"{targetDir}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                // Remove .git folder so it's a clean project
                var gitDir = Path.Combine(targetDir, ".git");
                if (Directory.Exists(gitDir))
                {
                    try { Directory.Delete(gitDir, true); } catch { }
                }

                CreateModStatus = $"✓ Mod project created at {targetDir}";
                _main.SetStatus($"Mod template '{sanitized}' created successfully.");
                Process.Start("explorer.exe", targetDir);
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync();
                CreateModStatus = $"✗ Git clone failed: {error.Trim()}";
                _main.SetStatus("Git clone failed — make sure Git is installed.");
            }
        }
        catch
        {
            CreateModStatus = "✗ Git not found. Please install Git and try again.";
            _main.SetStatus("Git not found — install Git to create mods from template.");
        }
        finally
        {
            IsCreatingMod = false;
        }
    }
}
