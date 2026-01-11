namespace CustosAC.Core.Configuration;

/// <summary>
/// Registry settings
/// </summary>
public class RegistrySettings
{
    public RegistryScanKey[] ScanKeys { get; set; } =
    {
        new() { Path = @"HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache", Name = "MuiCache" },
        new() { Path = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched", Name = "AppSwitched" },
        new() { Path = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\ShowJumpView", Name = "ShowJumpView" }
    };

    public string RegeditBlockPath { get; set; } = @"HKLM\Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\regedit.exe";
}

/// <summary>
/// Registry key for scanning
/// </summary>
public class RegistryScanKey
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
