using System.Diagnostics;
using System.Security.Principal;

namespace MDB.Launcher.Services;

/// <summary>
/// Manages process elevation for DLL injection.
/// The app runs unelevated by default and only requests admin
/// via UAC when performing injection operations.
/// </summary>
public class ProcessElevationService : IProcessElevationService
{
    public bool IsElevated
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public bool RelaunchElevated(string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath!,
                Arguments = arguments,
                Verb = "runas",
                UseShellExecute = true,
            };

            Process.Start(startInfo);
            return true;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User cancelled UAC prompt
            return false;
        }
    }
}
