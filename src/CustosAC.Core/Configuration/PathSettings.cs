namespace CustosAC.Core.Configuration;

/// <summary>
/// Path settings
/// </summary>
public class PathSettings
{
    public WindowsPathSettings Windows { get; set; } = new();
    public SteamPathSettings Steam { get; set; } = new();
}

/// <summary>
/// Windows paths
/// </summary>
public class WindowsPathSettings
{
    public string PrefetchPath { get; set; } = @"C:\Windows\Prefetch";
    public string WindowsPath { get; set; } = @"C:\Windows";
    public string ProgramFilesX86 { get; set; } = @"C:\Program Files (x86)";
    public string ProgramFiles { get; set; } = @"C:\Program Files";
}

/// <summary>
/// Steam paths
/// </summary>
public class SteamPathSettings
{
    public string[] AdditionalDrives { get; set; } = { "D:", "E:", "F:", "G:" };
    public string LoginUsersRelativePath { get; set; } = @"Steam\config\loginusers.vdf";
}
