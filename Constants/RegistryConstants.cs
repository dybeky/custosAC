namespace CustosAC.Constants;

/// <summary>
/// Константы для работы с реестром Windows
/// </summary>
public static class RegistryConstants
{
    // ═══════════════════════════════════════════════════════════════
    // ПУТИ РЕЕСТРА ДЛЯ СКАНИРОВАНИЯ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>MuiCache - кэш запущенных программ</summary>
    public const string MuiCachePath = @"HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache";

    /// <summary>AppSwitched - история переключений Alt+Tab</summary>
    public const string AppSwitchedPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched";

    /// <summary>ShowJumpView - история JumpList</summary>
    public const string ShowJumpViewPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\ShowJumpView";

    /// <summary>Ключи реестра для сканирования</summary>
    public static readonly (string path, string name)[] ScanKeys =
    {
        (MuiCachePath, "MuiCache"),
        (AppSwitchedPath, "AppSwitched"),
        (ShowJumpViewPath, "ShowJumpView")
    };

    // ═══════════════════════════════════════════════════════════════
    // БЛОКИРОВКИ ДЛЯ УДАЛЕНИЯ (РАЗБЛОКИРОВКА СИСТЕМЫ)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Значения реестра для удаления при разблокировке системы</summary>
    public static readonly (string key, string value)[] ValuesToDelete =
    {
        // Панель управления и параметры
        (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoControlPanel"),
        (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoControlPanel"),
        (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "SettingsPageVisibility"),
        (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "SettingsPageVisibility"),
        (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "DisallowCpl"),
        (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "DisallowCpl"),

        // Устаревшие ключи сети
        (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Network", "NoNetSetup"),
        (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Network", "NoNetSetup"),
        (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Network", "NoNetSetupSecurityPage"),
        (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Network", "NoNetSetupSecurityPage"),
        (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Network", "NoNetSetupIDPage"),
        (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Network", "NoNetSetupIDPage"),

        // Network Connections
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_AllowAdvancedTCPIPConfig"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_AllowAdvancedTCPIPConfig"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_LanConnect"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_LanConnect"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_LanProperties"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_LanProperties"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_LanChangeProperties"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_LanChangeProperties"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_NewConnectionWizard"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_NewConnectionWizard"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_DialupPrefs"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_DialupPrefs"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_ChangeBindState"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_ChangeBindState"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_AddRemoveComponents"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_AddRemoveComponents"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_Statistics"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_Statistics"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_EnableAdminProhibits"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_EnableAdminProhibits"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_ShowSharedAccessUI"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_ShowSharedAccessUI"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_PersonalFirewallConfig"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_PersonalFirewallConfig"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_ICSEnable"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_ICSEnable"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_RenameConnection"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_RenameConnection"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_DeleteConnection"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_DeleteConnection"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_RasAllUserProperties"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_RasAllUserProperties"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_RasMyProperties"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_RasMyProperties"),
        (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_RasConnect"),
        (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_RasConnect"),

        // Internet Explorer
        (@"HKCU\Software\Policies\Microsoft\Internet Explorer\Control Panel", "ConnectionsTab"),
        (@"HKLM\Software\Policies\Microsoft\Internet Explorer\Control Panel", "ConnectionsTab"),
        (@"HKCU\Software\Policies\Microsoft\Internet Explorer\Control Panel", "Connwiz Admin Lock"),
        (@"HKLM\Software\Policies\Microsoft\Internet Explorer\Control Panel", "Connwiz Admin Lock"),
        (@"HKCU\Software\Policies\Microsoft\Internet Explorer\Control Panel", "Connection Settings"),
        (@"HKLM\Software\Policies\Microsoft\Internet Explorer\Control Panel", "Connection Settings"),
        (@"HKCU\Software\Policies\Microsoft\Internet Explorer\Control Panel", "Proxy"),
        (@"HKLM\Software\Policies\Microsoft\Internet Explorer\Control Panel", "Proxy"),
        (@"HKCU\Software\Policies\Microsoft\Internet Explorer\Control Panel", "AutoConfig"),
        (@"HKLM\Software\Policies\Microsoft\Internet Explorer\Control Panel", "AutoConfig"),
        (@"HKCU\Software\Policies\Microsoft\Internet Explorer\Control Panel", "LAN Settings"),
        (@"HKLM\Software\Policies\Microsoft\Internet Explorer\Control Panel", "LAN Settings"),

        // Windows Settings
        (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\System", "NoDispCPL"),
        (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\System", "NoDispCPL"),
        (@"HKCU\Software\Policies\Microsoft\Windows\System", "SettingsPageVisibility"),
        (@"HKLM\Software\Policies\Microsoft\Windows\System", "SettingsPageVisibility"),

        // Wi-Fi
        (@"HKCU\Software\Policies\Microsoft\Windows\System", "DenyDeviceIDs"),
        (@"HKLM\Software\Policies\Microsoft\Windows\System", "DenyDeviceIDs"),
        (@"HKLM\Software\Policies\Microsoft\Windows\WcmSvc\GroupPolicy", "fBlockNonDomain"),
        (@"HKLM\Software\Policies\Microsoft\Windows\WcmSvc\GroupPolicy", "fMinimizeConnections"),
    };

    /// <summary>Ветки реестра для полного удаления</summary>
    public static readonly string[] KeysToDelete =
    {
        @"HKCU\Software\Policies\Microsoft\Windows\Network Connections",
        @"HKLM\Software\Policies\Microsoft\Windows\Network Connections",
        @"HKCU\Software\Policies\Microsoft\Internet Explorer\Control Panel",
        @"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Network",
        @"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Network",
    };

    /// <summary>Путь для разблокировки regedit</summary>
    public const string RegeditBlockPath = @"HKLM\Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\regedit.exe";
}
