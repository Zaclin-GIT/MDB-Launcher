using System.Windows;
using MDB.Launcher.Services;
using MDB.Launcher.ViewModels;

namespace MDB.Launcher;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        // Create services
        var profileService = new ProfileService();
        var injectorService = new InjectorService();
        var modDiscoveryService = new ModDiscoveryService();
        var gitHubModService = new GitHubModService();
        var elevationService = new ProcessElevationService();

        // Create and bind ViewModel
        _viewModel = new MainViewModel(
            profileService,
            injectorService,
            modDiscoveryService,
            gitHubModService,
            elevationService);

        DataContext = _viewModel;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    // ── Window Chrome Handlers ──

    private void MinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
