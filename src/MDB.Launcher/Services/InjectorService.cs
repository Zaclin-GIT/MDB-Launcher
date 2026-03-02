using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using MDB.Launcher.Native;

namespace MDB.Launcher.Services;

/// <summary>
/// DLL injection service using kernel32 P/Invoke.
/// Supports launch-and-inject (with poll-until-ready for GameAssembly.dll)
/// and attach-to-running-process modes.
/// </summary>
public class InjectorService : IInjectorService
{
    public async Task<InjectionResult> LaunchAndInjectAsync(
        string gameExePath,
        string bridgeDllPath,
        string launchArgs = "",
        int pollIntervalMs = 500,
        int timeoutSeconds = 60,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Validate paths
        if (!File.Exists(gameExePath))
            return new InjectionResult(false, $"Game executable not found: {gameExePath}");

        if (!File.Exists(bridgeDllPath))
            return new InjectionResult(false, $"MDB_Bridge.dll not found: {bridgeDllPath}");

        var bridgeFullPath = Path.GetFullPath(bridgeDllPath);

        try
        {
            // Launch the game
            progress?.Report("Launching game...");
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = gameExePath,
                Arguments = launchArgs,
                WorkingDirectory = Path.GetDirectoryName(gameExePath),
                UseShellExecute = true
            };

            var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
                return new InjectionResult(false, "Failed to start game process.");

            progress?.Report($"Game launched (PID: {process.Id}). Waiting for GameAssembly.dll...");

            // Poll for GameAssembly.dll
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);

            while (!IsModuleLoaded((uint)process.Id, "GameAssembly.dll"))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (process.HasExited)
                    return new InjectionResult(false, "Game exited before GameAssembly.dll could be detected.");

                if (stopwatch.Elapsed > timeout)
                    return new InjectionResult(false, $"Timed out waiting for GameAssembly.dll after {timeoutSeconds}s.");

                var elapsed = stopwatch.Elapsed;
                progress?.Report($"Waiting for GameAssembly.dll... ({elapsed.Seconds}s / {timeoutSeconds}s)");

                await Task.Delay(pollIntervalMs, cancellationToken);
            }

            progress?.Report("GameAssembly.dll detected. Injecting MDB_Bridge.dll...");

            // Small delay to let the game initialize
            await Task.Delay(1000, cancellationToken);

            // Inject
            return await InjectIntoProcessAsync(process.Id, bridgeFullPath, progress);
        }
        catch (OperationCanceledException)
        {
            return new InjectionResult(false, "Injection cancelled by user.");
        }
        catch (Exception ex)
        {
            return new InjectionResult(false, $"Injection failed: {ex.Message}");
        }
    }

    public Task<InjectionResult> InjectIntoProcessAsync(
        int processId,
        string bridgeDllPath,
        IProgress<string>? progress = null)
    {
        return Task.Run(() =>
        {
            var bridgeFullPath = Path.GetFullPath(bridgeDllPath);

            if (!File.Exists(bridgeFullPath))
                return new InjectionResult(false, $"MDB_Bridge.dll not found: {bridgeFullPath}");

            IntPtr hProcess = IntPtr.Zero;
            IntPtr remoteMem = IntPtr.Zero;
            IntPtr hThread = IntPtr.Zero;

            try
            {
                // Open target process
                progress?.Report($"Opening process {processId}...");
                hProcess = Kernel32.OpenProcess(
                    NativeConstants.PROCESS_INJECTION_ACCESS,
                    false,
                    (uint)processId);

                if (hProcess == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    return new InjectionResult(false,
                        $"Failed to open process (PID: {processId}). Error: {error}. " +
                        "Try running as Administrator.");
                }

                // Get LoadLibraryW address from kernel32.dll
                progress?.Report("Resolving LoadLibraryW...");
                var hKernel32 = Kernel32.GetModuleHandleW("kernel32.dll");
                if (hKernel32 == IntPtr.Zero)
                    return new InjectionResult(false, "Failed to get kernel32.dll handle.");

                var loadLibraryAddr = Kernel32.GetProcAddress(hKernel32, "LoadLibraryW");
                if (loadLibraryAddr == IntPtr.Zero)
                    return new InjectionResult(false, "Failed to get LoadLibraryW address.");

                // Write DLL path into target process memory
                progress?.Report("Allocating remote memory...");
                var dllPathBytes = Encoding.Unicode.GetBytes(bridgeFullPath + '\0');
                var pathSize = (uint)dllPathBytes.Length;

                remoteMem = Kernel32.VirtualAllocEx(
                    hProcess,
                    IntPtr.Zero,
                    pathSize,
                    NativeConstants.MEM_COMMIT | NativeConstants.MEM_RESERVE,
                    NativeConstants.PAGE_READWRITE);

                if (remoteMem == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    return new InjectionResult(false, $"Failed to allocate remote memory. Error: {error}");
                }

                progress?.Report("Writing DLL path to remote process...");
                if (!Kernel32.WriteProcessMemory(hProcess, remoteMem, dllPathBytes, pathSize, out _))
                {
                    var error = Marshal.GetLastWin32Error();
                    return new InjectionResult(false, $"Failed to write process memory. Error: {error}");
                }

                // Create remote thread to call LoadLibraryW with our DLL path
                progress?.Report("Creating remote thread...");
                hThread = Kernel32.CreateRemoteThread(
                    hProcess,
                    IntPtr.Zero,
                    0,
                    loadLibraryAddr,
                    remoteMem,
                    0,
                    out _);

                if (hThread == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    return new InjectionResult(false, $"Failed to create remote thread. Error: {error}");
                }

                // Wait for the remote thread to finish
                progress?.Report("Waiting for injection to complete...");
                var waitResult = Kernel32.WaitForSingleObject(hThread, 10000); // 10s timeout

                if (waitResult != NativeConstants.WAIT_OBJECT_0)
                    return new InjectionResult(false, $"Remote thread wait failed. Result: 0x{waitResult:X8}");

                progress?.Report("MDB_Bridge.dll injected successfully!");
                return new InjectionResult(true, "MDB_Bridge.dll injected successfully.");
            }
            catch (Exception ex)
            {
                return new InjectionResult(false, $"Injection error: {ex.Message}");
            }
            finally
            {
                // Cleanup
                if (remoteMem != IntPtr.Zero && hProcess != IntPtr.Zero)
                    Kernel32.VirtualFreeEx(hProcess, remoteMem, 0, NativeConstants.MEM_RELEASE);
                if (hThread != IntPtr.Zero)
                    Kernel32.CloseHandle(hThread);
                if (hProcess != IntPtr.Zero)
                    Kernel32.CloseHandle(hProcess);
            }
        });
    }

    public bool IsModuleLoaded(uint processId, string moduleName)
    {
        IntPtr snapshot = Kernel32.CreateToolhelp32Snapshot(
            NativeConstants.TH32CS_SNAPMODULE | NativeConstants.TH32CS_SNAPMODULE32,
            processId);

        if (snapshot == IntPtr.Zero || snapshot == NativeConstants.INVALID_HANDLE_VALUE)
            return false;

        try
        {
            var entry = new MODULEENTRY32W();
            entry.dwSize = (uint)Marshal.SizeOf<MODULEENTRY32W>();

            if (!Kernel32.Module32FirstW(snapshot, ref entry))
                return false;

            do
            {
                if (string.Equals(entry.szModule, moduleName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            while (Kernel32.Module32NextW(snapshot, ref entry));

            return false;
        }
        finally
        {
            Kernel32.CloseHandle(snapshot);
        }
    }
}
