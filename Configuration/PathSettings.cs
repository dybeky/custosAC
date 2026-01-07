namespace CustosAC.Configuration;

/// <summary>
/// Настройки путей
/// </summary>
public class PathSettings
{
    public WindowsPathSettings Windows { get; set; } = new();
    public SteamPathSettings Steam { get; set; } = new();
}

/// <summary>
/// Пути Windows
/// </summary>
public class WindowsPathSettings
{
    public string PrefetchPath { get; set; } = @"C:\Windows\Prefetch";
    public string WindowsPath { get; set; } = @"C:\Windows";
    public string ProgramFilesX86 { get; set; } = @"C:\Program Files (x86)";
    public string ProgramFiles { get; set; } = @"C:\Program Files";
}

/// <summary>
/// Пути Steam
/// </summary>
public class SteamPathSettings
{
    public string[] AdditionalDrives { get; set; } = { "D:", "E:", "F:", "G:" };
    public string LoginUsersRelativePath { get; set; } = @"Steam\config\loginusers.vdf";
    public string UnturnedScreenshotsRelativePath { get; set; } = @"Steam\steamapps\common\Unturned\Screenshots";
}
