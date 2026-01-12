namespace CustosAC.Core.Configuration;

/// <summary>
/// Registry settings
/// </summary>
public class RegistrySettings
{
    public RegistryScanKey[] ScanKeys { get; set; } =
    {
        new() { Path = @"HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache", Name = "MuiCache (Run History)" },
        new() { Path = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched", Name = "AppSwitched (ALT+TAB History)" },
        new() { Path = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\ShowJumpView", Name = "ShowJumpView (Taskbar History)" },
        new() { Path = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppBadgeUpdated", Name = "AppBadgeUpdated (App Notifications)" },
        new() { Path = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppLaunch", Name = "AppLaunch (App Launch History)" },
        new() { Path = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", Name = "Autorun HKCU" },
        new() { Path = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run", Name = "Autorun HKLM" },
        new() { Path = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", Name = "Compatibility Flags" },
        new() { Path = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU", Name = "Run Dialog History" },
        new() { Path = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU", Name = "Open/Save Dialog History" },
        new() { Path = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs", Name = "Recent Documents" },
        new() { Path = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist", Name = "UserAssist (Program Usage)" }
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
