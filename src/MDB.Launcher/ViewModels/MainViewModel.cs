using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDB.Launcher.Models;
using MDB.Launcher.Services;

namespace MDB.Launcher.ViewModels;

/// <summary>
/// Root ViewModel for the application. Manages navigation and the active profile.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IProfileService _profileService;
    private readonly IInjectorService _injectorService;
    private readonly IModDiscoveryService _modDiscoveryService;
    private readonly IGitHubModService _gitHubModService;
    private readonly IProcessElevationService _elevationService;

    [ObservableProperty]
    private ObservableObject? _currentView;

    [ObservableProperty]
    private string _selectedNav = "Home";

    [ObservableProperty]
    private GameProfile? _activeProfile;

    [ObservableProperty]
    private ObservableCollection<GameProfile> _profiles = [];

    [ObservableProperty]
    private LauncherConfig _config = new();

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isElevated;

    // Child ViewModels
    public HomeViewModel HomeViewModel { get; }
    public ModListViewModel ModListViewModel { get; }
    public ModDownloadViewModel ModDownloadViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public DocsViewModel DocsViewModel { get; }

    public MainViewModel(
        IProfileService profileService,
        IInjectorService injectorService,
        IModDiscoveryService modDiscoveryService,
        IGitHubModService gitHubModService,
        IProcessElevationService elevationService)
    {
        _profileService = profileService;
        _injectorService = injectorService;
        _modDiscoveryService = modDiscoveryService;
        _gitHubModService = gitHubModService;
        _elevationService = elevationService;

        IsElevated = _elevationService.IsElevated;

        HomeViewModel = new HomeViewModel(this, profileService, injectorService, elevationService, gitHubModService);
        ModListViewModel = new ModListViewModel(this, modDiscoveryService);
        ModDownloadViewModel = new ModDownloadViewModel(this, gitHubModService);
        SettingsViewModel = new SettingsViewModel(this, profileService);
        DocsViewModel = new DocsViewModel();

        CurrentView = HomeViewModel;
    }

    public async Task InitializeAsync()
    {
        Config = await _profileService.LoadConfigAsync();
        Profiles = new ObservableCollection<GameProfile>(Config.Profiles);

        if (!string.IsNullOrEmpty(Config.GitHubToken))
            _gitHubModService.SetToken(Config.GitHubToken);

        // Restore active profile
        if (!string.IsNullOrEmpty(Config.ActiveProfileId))
        {
            ActiveProfile = Profiles.FirstOrDefault(p => p.Id == Config.ActiveProfileId);
        }

        ActiveProfile ??= Profiles.FirstOrDefault();
    }

    public async Task SaveConfigAsync()
    {
        Config.Profiles = [.. Profiles];
        Config.ActiveProfileId = ActiveProfile?.Id;
        await _profileService.SaveConfigAsync(Config);
    }

    partial void OnActiveProfileChanged(GameProfile? value)
    {
        HomeViewModel.OnProfileChanged();
        ModListViewModel.OnProfileChanged();
        ModDownloadViewModel.OnProfileChanged();

        _ = SaveConfigAsync();
    }

    [RelayCommand]
    private void NavigateTo(string viewName)
    {
        SelectedNav = viewName;
        CurrentView = viewName switch
        {
            "Home" => HomeViewModel,
            "Mods" => ModListViewModel,
            "Download" => ModDownloadViewModel,
            "Settings" => SettingsViewModel,
            "Docs" => DocsViewModel,
            _ => HomeViewModel
        };
    }

    public void SetStatus(string message)
    {
        StatusMessage = message;
    }

    [RelayCommand]
    private void OpenLink(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to open link: {ex.Message}");
        }
    }
}
