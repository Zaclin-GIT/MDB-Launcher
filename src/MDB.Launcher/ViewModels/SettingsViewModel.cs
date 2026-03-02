using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDB.Launcher.Services;

namespace MDB.Launcher.ViewModels;

/// <summary>
/// ViewModel for the Settings view — launcher configuration.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly IProfileService _profileService;

    [ObservableProperty]
    private int _injectionPollIntervalMs;

    [ObservableProperty]
    private int _injectionTimeoutSeconds;

    [ObservableProperty]
    private bool _closeAfterInjection;

    [ObservableProperty]
    private string _gitHubToken = string.Empty;

    [ObservableProperty]
    private string _configFilePath = string.Empty;

    [ObservableProperty]
    private string _saveStatus = string.Empty;

    public SettingsViewModel(MainViewModel main, IProfileService profileService)
    {
        _main = main;
        _profileService = profileService;

        ConfigFilePath = profileService.ConfigFilePath;
        LoadFromConfig();
    }

    private void LoadFromConfig()
    {
        InjectionPollIntervalMs = _main.Config.InjectionPollIntervalMs;
        InjectionTimeoutSeconds = _main.Config.InjectionTimeoutSeconds;
        CloseAfterInjection = _main.Config.CloseAfterInjection;
        GitHubToken = _main.Config.GitHubToken ?? string.Empty;
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        _main.Config.InjectionPollIntervalMs = InjectionPollIntervalMs;
        _main.Config.InjectionTimeoutSeconds = InjectionTimeoutSeconds;
        _main.Config.CloseAfterInjection = CloseAfterInjection;
        _main.Config.GitHubToken = string.IsNullOrWhiteSpace(GitHubToken) ? null : GitHubToken;

        await _main.SaveConfigAsync();

        SaveStatus = "Settings saved!";
        _main.SetStatus("Settings saved.");

        // Clear the status after a short delay
        await Task.Delay(2000);
        SaveStatus = string.Empty;
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        InjectionPollIntervalMs = 500;
        InjectionTimeoutSeconds = 60;
        CloseAfterInjection = false;
        GitHubToken = string.Empty;

        _main.SetStatus("Settings reset to defaults. Click Save to apply.");
    }
}
