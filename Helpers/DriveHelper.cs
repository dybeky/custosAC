namespace CustosAC.Helpers;

public static class DriveHelper
{
    private static readonly string[] AdditionalDrives = { "D:", "E:", "F:", "G:" };

    /// <summary>
    /// Получить все возможные пути к файлу loginusers.vdf для Steam
    /// </summary>
    public static List<string> GetSteamLoginUsersPaths()
    {
        return GetPathsForAllDrives("Steam", "config", "loginusers.vdf");
    }

    /// <summary>
    /// Получить все возможные пути к папке скриншотов Unturned
    /// </summary>
    public static List<string> GetUnturnedScreenshotsPaths()
    {
        return GetPathsForAllDrives("Steam", "steamapps", "common", "Unturned", "Screenshots");
    }

    /// <summary>
    /// Найти первый существующий путь (файл или папка)
    /// </summary>
    public static string? FindFirstExisting(IEnumerable<string> paths, bool isFile = true)
    {
        foreach (var path in paths)
        {
            if (isFile ? File.Exists(path) : Directory.Exists(path))
                return path;
        }
        return null;
    }

    private static List<string> GetPathsForAllDrives(params string[] subPath)
    {
        var paths = new List<string>
        {
            Path.Combine(@"C:\Program Files (x86)", Path.Combine(subPath)),
            Path.Combine(@"C:\Program Files", Path.Combine(subPath))
        };

        foreach (var drive in AdditionalDrives)
        {
            paths.Add(Path.Combine(drive, Path.Combine(subPath)));
            paths.Add(Path.Combine(drive, "Program Files (x86)", Path.Combine(subPath)));
            paths.Add(Path.Combine(drive, "Program Files", Path.Combine(subPath)));
            paths.Add(Path.Combine(drive, "SteamLibrary", Path.Combine(subPath)));
        }

        return paths;
    }
}
