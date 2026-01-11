namespace CustosAC.Core.Configuration;

/// <summary>
/// Scanning settings with embedded defaults.
/// </summary>
public class ScanSettings
{
    public int AppDataScanDepth { get; set; } = 3;
    public int WindowsScanDepth { get; set; } = 1;
    public int ProgramFilesScanDepth { get; set; } = 2;
    public int UserFoldersScanDepth { get; set; } = 3;
    public int RecentFilesDays { get; set; } = 7;

    public string[] ExecutableExtensions { get; set; } = { ".exe", ".bat", ".cmd", ".ps1" };

    public string[] ExcludedDirectories { get; set; } =
    {
        "windows.old", "$recycle.bin", "system volume information",
        "recovery", "perflogs", "windowsapps", "winsxs",
        ".git", "node_modules", "cache", "caches", "temp", "tmp",
        "logs", "log", "crash reports", "crashreports",
        "gpucache", "code cache", "shadercache", "shader cache",
        "service worker", "webcache", "blob_storage",
        "session storage", "local storage", "indexeddb",
        "installer", "google", "microsoft", "mozilla",
        "discord", "spotify", "steam", "nvidia", "amd", "intel", "adobe",
        "packages", "program files", "program files (x86)", "programdata",
        "drivers", "fonts", "assembly", "microsoft.net", "reference assemblies",
        "vscode", "visual studio", "jetbrains", "unity", "unreal",
        "epic games", "battlenet", "riot games", "origin", "ea",
        "ubisoft", "gog galaxy", "nuget"
    };
}
