namespace CustosAC.Helpers;

public static class DriveHelper
{
    // Дополнительные диски для проверки (помимо C:)
    private static readonly string[] AdditionalDrives = { "D:", "E:", "F:", "G:" };

    /// <summary>
    /// Получить все возможные пути к файлу loginusers.vdf для Steam
    /// </summary>
    public static List<string> GetSteamLoginUsersPaths()
    {
        var paths = new List<string>
        {
            @"C:\Program Files (x86)\Steam\config\loginusers.vdf",
            @"C:\Program Files\Steam\config\loginusers.vdf"
        };

        foreach (var drive in AdditionalDrives)
        {
            paths.Add(Path.Combine(drive, "Steam", "config", "loginusers.vdf"));
            paths.Add(Path.Combine(drive, "Program Files (x86)", "Steam", "config", "loginusers.vdf"));
            paths.Add(Path.Combine(drive, "Program Files", "Steam", "config", "loginusers.vdf"));
            paths.Add(Path.Combine(drive, "SteamLibrary", "config", "loginusers.vdf"));
        }

        return paths;
    }

    /// <summary>
    /// Получить все возможные пути к папке скриншотов Unturned
    /// </summary>
    public static List<string> GetUnturnedScreenshotsPaths()
    {
        var paths = new List<string>
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\Unturned\Screenshots",
            @"C:\Program Files\Steam\steamapps\common\Unturned\Screenshots"
        };

        foreach (var drive in AdditionalDrives)
        {
            paths.Add(Path.Combine(drive, "Steam", "steamapps", "common", "Unturned", "Screenshots"));
            paths.Add(Path.Combine(drive, "Program Files (x86)", "Steam", "steamapps", "common", "Unturned", "Screenshots"));
            paths.Add(Path.Combine(drive, "Program Files", "Steam", "steamapps", "common", "Unturned", "Screenshots"));
            paths.Add(Path.Combine(drive, "SteamLibrary", "steamapps", "common", "Unturned", "Screenshots"));
        }

        return paths;
    }

    /// <summary>
    /// Найти первый существующий файл из списка путей
    /// </summary>
    public static string? FindFirstExistingFile(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }
        return null;
    }

    /// <summary>
    /// Найти первую существующую папку из списка путей
    /// </summary>
    public static string? FindFirstExistingDirectory(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                return path;
            }
        }
        return null;
    }
}
