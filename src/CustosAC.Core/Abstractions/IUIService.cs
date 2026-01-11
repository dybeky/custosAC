namespace CustosAC.Core.Abstractions;

/// <summary>
/// UI service abstraction for decoupling business logic from presentation layer.
/// Implementations: WpfUIService (GUI), ConsoleUIService (CLI)
/// </summary>
public interface IUIService
{
    /// <summary>
    /// Report progress during scanning
    /// </summary>
    /// <param name="scannerName">Name of the current scanner</param>
    /// <param name="currentPath">Current path being scanned (optional)</param>
    /// <param name="foundCount">Number of findings so far</param>
    void ReportScanProgress(string scannerName, string? currentPath, int foundCount);

    /// <summary>
    /// Report that a scanner has completed
    /// </summary>
    void ReportScanComplete(string scannerName, int findingsCount, TimeSpan duration);

    /// <summary>
    /// Log an informational message
    /// </summary>
    void LogInfo(string message);

    /// <summary>
    /// Log a success message
    /// </summary>
    void LogSuccess(string message);

    /// <summary>
    /// Log a warning message
    /// </summary>
    void LogWarning(string message);

    /// <summary>
    /// Log an error message
    /// </summary>
    void LogError(string message);
}

/// <summary>
/// Null UI service for silent/headless mode
/// </summary>
public class NullUIService : IUIService
{
    public static NullUIService Instance { get; } = new();

    public void ReportScanProgress(string scannerName, string? currentPath, int foundCount) { }
    public void ReportScanComplete(string scannerName, int findingsCount, TimeSpan duration) { }
    public void LogInfo(string message) { }
    public void LogSuccess(string message) { }
    public void LogWarning(string message) { }
    public void LogError(string message) { }
}
