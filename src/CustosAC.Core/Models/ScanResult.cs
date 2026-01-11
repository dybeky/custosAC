namespace CustosAC.Core.Models;

/// <summary>
/// Scan result from a scanner
/// </summary>
public class ScanResult
{
    /// <summary>Scanner name</summary>
    public string ScannerName { get; set; } = string.Empty;

    /// <summary>Whether the scan completed successfully</summary>
    public bool Success { get; set; }

    /// <summary>List of suspicious findings</summary>
    public List<string> Findings { get; set; } = new();

    /// <summary>Error message if any</summary>
    public string? Error { get; set; }

    /// <summary>Scan start time</summary>
    public DateTime StartTime { get; set; }

    /// <summary>Scan end time</summary>
    public DateTime EndTime { get; set; }

    /// <summary>Scan duration</summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>Number of findings</summary>
    public int Count => Findings.Count;

    /// <summary>Whether there are any findings</summary>
    public bool HasFindings => Findings.Count > 0;
}

/// <summary>
/// Scan progress information
/// </summary>
public class ScanProgress
{
    /// <summary>Scanner name</summary>
    public string ScannerName { get; set; } = string.Empty;

    /// <summary>Current item number</summary>
    public int Current { get; set; }

    /// <summary>Total items</summary>
    public int Total { get; set; }

    /// <summary>Current path being scanned</summary>
    public string? CurrentPath { get; set; }

    /// <summary>Progress percentage</summary>
    public int Percentage => Total > 0 ? (Current * 100) / Total : 0;
}
