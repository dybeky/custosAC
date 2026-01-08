using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

namespace CustosAC.Services;

/// <summary>
/// Сервис для работы с реестром Windows через Microsoft.Win32.Registry API
/// </summary>
[SupportedOSPlatform("windows")]
public class RegistryService
{
    /// <summary>
    /// Экспортировать ключ реестра в строку
    /// </summary>
    public bool ExportKeyToString(string keyPath, out string content)
    {
        content = string.Empty;
        try
        {
            var (hive, subKey) = ParseRegistryPath(keyPath);
            using var key = hive.OpenSubKey(subKey);
            if (key == null) return false;

            var sb = new StringBuilder();
            foreach (var valueName in key.GetValueNames())
            {
                var value = key.GetValue(valueName);
                sb.AppendLine($"{valueName}={value}");
            }
            content = sb.ToString();
            return true;
        }
        catch
        {
            // Registry access may fail due to permissions or missing key - return empty content
            return false;
        }
    }

    /// <summary>
    /// Удалить значение из реестра
    /// </summary>
    public bool DeleteValue(string keyPath, string valueName)
    {
        try
        {
            var (hive, subKey) = ParseRegistryPath(keyPath);
            using var key = hive.OpenSubKey(subKey, writable: true);
            key?.DeleteValue(valueName, throwOnMissingValue: false);
            return true;
        }
        catch
        {
            // Value deletion may fail if key doesn't exist or access denied - return false
            return false;
        }
    }

    /// <summary>
    /// Удалить ключ реестра целиком
    /// </summary>
    public bool DeleteKey(string keyPath)
    {
        try
        {
            var (hive, subKey) = ParseRegistryPath(keyPath);
            hive.DeleteSubKeyTree(subKey, throwOnMissingSubKey: false);
            return true;
        }
        catch
        {
            // Key deletion may fail due to permissions or protected key - return false
            return false;
        }
    }

    /// <summary>
    /// Проверить существование ключа реестра
    /// </summary>
    public bool KeyExists(string keyPath)
    {
        try
        {
            var (hive, subKey) = ParseRegistryPath(keyPath);
            using var key = hive.OpenSubKey(subKey);
            return key != null;
        }
        catch
        {
            // Registry access error - treat as key not existing
            return false;
        }
    }

    /// <summary>
    /// Открыть редактор реестра с копированием пути в буфер
    /// </summary>
    public async Task OpenRegistryEditorAsync(string keyPath, ProcessService processService)
    {
        await processService.CopyToClipboardAsync(keyPath);
        var psi = new ProcessStartInfo
        {
            FileName = "regedit",
            UseShellExecute = true
        };
        var process = Process.Start(psi);
        if (process != null)
        {
            processService.TrackProcess(process);
        }
    }

    /// <summary>
    /// Распарсить путь реестра на hive и subKey
    /// </summary>
    private static (RegistryKey hive, string subKey) ParseRegistryPath(string path)
    {
        var parts = path.Split('\\', 2);
        var hiveName = parts[0];
        var subKey = parts.Length > 1 ? parts[1] : "";

        RegistryKey hive = hiveName switch
        {
            "HKEY_CURRENT_USER" or "HKCU" => Registry.CurrentUser,
            "HKEY_LOCAL_MACHINE" or "HKLM" => Registry.LocalMachine,
            "HKEY_CLASSES_ROOT" or "HKCR" => Registry.ClassesRoot,
            "HKEY_USERS" or "HKU" => Registry.Users,
            "HKEY_CURRENT_CONFIG" or "HKCC" => Registry.CurrentConfig,
            _ => throw new ArgumentException($"Unknown registry hive: {hiveName}")
        };

        return (hive, subKey);
    }
}
