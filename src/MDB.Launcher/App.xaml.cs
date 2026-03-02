using System.Windows;
using Microsoft.Win32;
using MDB.Launcher.Services;

namespace MDB.Launcher;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        SetBrowserEmulationMode();

        // ── Handle elevated injection commands (headless mode) ──
        if (e.Args.Length > 0 && e.Args[0].StartsWith("--inject"))
        {
            await HandleInjectionCommandAsync(e.Args);
            Shutdown();
            return;
        }

        // ── Normal startup — show main window ──
        // (MainWindow is set via StartupUri in App.xaml)
    }

    /// <summary>
    /// Force the WPF WebBrowser to use IE11 (Edge) rendering mode
    /// instead of the default IE7 mode. This fixes modern CSS rendering.
    /// </summary>
    private static void SetBrowserEmulationMode()
    {
        try
        {
            var appName = System.IO.Path.GetFileName(Environment.ProcessPath ?? "MDB.Launcher.exe");
            using var key = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION",
                true);
            key?.SetValue(appName, 11001, RegistryValueKind.DWord);
        }
        catch
        {
            // Non-critical — docs will render in legacy mode
        }
    }

    private static async Task HandleInjectionCommandAsync(string[] args)
    {
        var injector = new InjectorService();

        try
        {
            if (args[0] == "--inject-launch" && args.Length >= 3)
            {
                var gameExe = args[1];
                var bridgeDll = args[2];
                var launchArgs = args.Length >= 4 ? args[3] : "";

                var result = await injector.LaunchAndInjectAsync(
                    gameExe, bridgeDll, launchArgs);

                Environment.ExitCode = result.Success ? 0 : 1;
            }
            else if (args[0] == "--inject-attach" && args.Length >= 3)
            {
                var pid = int.Parse(args[1]);
                var bridgeDll = args[2];

                var result = await injector.InjectIntoProcessAsync(pid, bridgeDll);
                Environment.ExitCode = result.Success ? 0 : 1;
            }
            else
            {
                Environment.ExitCode = 1;
            }
        }
        catch
        {
            Environment.ExitCode = 1;
        }
    }
}
