using System.Diagnostics;

namespace MDB.Launcher.Models;

/// <summary>
/// Represents a running process for the process picker dialog.
/// </summary>
public class ProcessEntry
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string MainWindowTitle { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;

    /// <summary>Display text for the process picker.</summary>
    public string DisplayText => string.IsNullOrEmpty(MainWindowTitle)
        ? $"{ProcessName} (PID: {ProcessId})"
        : $"{ProcessName} — {MainWindowTitle} (PID: {ProcessId})";

    public static ProcessEntry FromProcess(Process process)
    {
        string fileName = string.Empty;
        try { fileName = process.MainModule?.FileName ?? string.Empty; } catch { }

        return new ProcessEntry
        {
            ProcessId = process.Id,
            ProcessName = process.ProcessName,
            MainWindowTitle = process.MainWindowTitle,
            FileName = fileName
        };
    }
}
