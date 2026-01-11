using CustosAC.Core.Abstractions;

namespace CustosAC.WPF.Services;

/// <summary>
/// WPF implementation of IUIService
/// </summary>
public class WpfUIService : IUIService
{
    public void ReportScanProgress(string scannerName, string? currentPath, int foundCount)
    {
        // Progress is handled by ViewModel directly in WPF
    }

    public void ReportScanComplete(string scannerName, int findingsCount, TimeSpan duration)
    {
        // Completion is handled by ViewModel directly in WPF
    }

    public void LogInfo(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[INFO] {message}");
    }

    public void LogSuccess(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[SUCCESS] {message}");
    }

    public void LogWarning(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[WARN] {message}");
    }

    public void LogError(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[ERROR] {message}");
    }
}
