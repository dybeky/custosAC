namespace CustosAC.Configuration;

/// <summary>
/// Настройки сканирования.
/// Значения по умолчанию могут быть переопределены в appsettings.json
/// </summary>
public class ScanSettings
{
    public int AppDataScanDepth { get; set; } = 10;
    public int WindowsScanDepth { get; set; } = 2;
    public int ProgramFilesScanDepth { get; set; } = 3;
    public int UserFoldersScanDepth { get; set; } = 5;

    public string[] ExecutableExtensions { get; set; } = { ".exe", ".dll" };

    public string[] ExcludedDirectories { get; set; } =
    {
        "windows.old", "$recycle.bin", "system volume information",
        "recovery", "perflogs", "windowsapps", "winsxs",
        ".git", "node_modules"
    };
}
