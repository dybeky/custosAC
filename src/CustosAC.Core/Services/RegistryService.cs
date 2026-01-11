using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Text;

namespace CustosAC.Core.Services;

/// <summary>
/// Windows Registry service
/// </summary>
[SupportedOSPlatform("windows")]
public class RegistryService
{
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
                string valueStr = value switch
                {
                    byte[] bytes => BitConverter.ToString(bytes).Replace("-", " "),
                    string[] strings => string.Join(", ", strings),
                    _ => value?.ToString() ?? string.Empty
                };
                sb.AppendLine($"{valueName}={valueStr}");
            }
            content = sb.ToString();
            return true;
        }
        catch { return false; }
    }

    public bool KeyExists(string keyPath)
    {
        try
        {
            var (hive, subKey) = ParseRegistryPath(keyPath);
            using var key = hive.OpenSubKey(subKey);
            return key != null;
        }
        catch { return false; }
    }

    private static (RegistryKey hive, string subKey) ParseRegistryPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Registry path cannot be null or empty", nameof(path));

        var parts = path.Split('\\', 2);
        var hiveName = parts[0];
        var subKey = parts.Length > 1 ? parts[1] : "";

        RegistryKey hive = hiveName.ToUpperInvariant() switch
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
