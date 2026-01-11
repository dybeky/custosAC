namespace CustosAC.Core.Configuration;

/// <summary>
/// Main application settings
/// </summary>
public class AppSettings
{
    public TimeoutSettings Timeouts { get; set; } = new();
}

/// <summary>
/// Timeout settings
/// </summary>
public class TimeoutSettings
{
    public int DefaultProcessTimeoutMs { get; set; } = 10000;
    public int ServiceTimeoutMs { get; set; } = 5000;
    public int PowerShellTimeoutMs { get; set; } = 15000;
    public int ExitDelayMs { get; set; } = 800;
    public int CleanupDelayMs { get; set; } = 1500;
    public int UiDelayMs { get; set; } = 500;
}
