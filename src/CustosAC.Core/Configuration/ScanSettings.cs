namespace CustosAC.Core.Configuration;

/// <summary>
/// Scanning settings.
/// Default values can be overridden in appsettings.json
/// </summary>
public class ScanSettings
{
    public int AppDataScanDepth { get; set; } = 5;
    public int WindowsScanDepth { get; set; } = 2;
    public int ProgramFilesScanDepth { get; set; } = 4;
    public int UserFoldersScanDepth { get; set; } = 5;
    public int RecentFilesDays { get; set; } = 7;

    public string[] ExecutableExtensions { get; set; } = { ".exe", ".dll", ".bat", ".cmd", ".ps1", ".vbs" };

    public string[] ExcludedDirectories { get; set; } =
    {
        "windows.old", "$recycle.bin", "system volume information",
        "recovery", "perflogs", "windowsapps", "winsxs",
        ".git", "node_modules"
    };
}
