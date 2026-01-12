using Microsoft.Win32;

namespace CustosAC.Core.Services;

/// <summary>
/// Service for finding game and Steam installation paths.
/// Uses registry, Steam library folders, and full disk search as fallback.
/// </summary>
public class GamePathFinderService
{
    private readonly LogService _logService;
    private string? _cachedSteamPath;
    private string? _cachedUnturnedPath;
    private List<string>? _cachedSteamLibraries;

    public GamePathFinderService(LogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// Gets the Steam installation path from registry or disk search.
    /// </summary>
    public string? GetSteamPath()
    {
        if (_cachedSteamPath != null && Directory.Exists(_cachedSteamPath))
            return _cachedSteamPath;

        // Try registry first (most reliable)
        var registryPath = GetSteamPathFromRegistry();
        if (!string.IsNullOrEmpty(registryPath) && Directory.Exists(registryPath))
        {
            _cachedSteamPath = registryPath;
            return _cachedSteamPath;
        }

        // Try common paths
        var commonPaths = new[]
        {
            @"C:\Program Files (x86)\Steam",
            @"C:\Program Files\Steam",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam")
        };

        foreach (var path in commonPaths)
        {
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "steam.exe")))
            {
                _cachedSteamPath = path;
                return _cachedSteamPath;
            }
        }

        // Search all drives
        _cachedSteamPath = SearchAllDrives("Steam", "steam.exe");
        return _cachedSteamPath;
    }

    /// <summary>
    /// Gets Steam path from Windows registry.
    /// </summary>
    private string? GetSteamPathFromRegistry()
    {
        try
        {
            // Try 64-bit registry view first
            using var key64 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            if (key64?.GetValue("InstallPath") is string path64 && !string.IsNullOrEmpty(path64))
                return path64;

            // Try 32-bit registry view
            using var key32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
            if (key32?.GetValue("InstallPath") is string path32 && !string.IsNullOrEmpty(path32))
                return path32;

            // Try current user
            using var keyCU = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
            if (keyCU?.GetValue("SteamPath") is string pathCU && !string.IsNullOrEmpty(pathCU))
                return pathCU.Replace("/", "\\");
        }
        catch (Exception ex)
        {
            _logService.LogError("Failed to read Steam path from registry", ex);
        }

        return null;
    }

    /// <summary>
    /// Gets all Steam library folders.
    /// </summary>
    public List<string> GetSteamLibraries()
    {
        if (_cachedSteamLibraries != null)
            return _cachedSteamLibraries;

        _cachedSteamLibraries = new List<string>();
        var steamPath = GetSteamPath();

        if (string.IsNullOrEmpty(steamPath))
            return _cachedSteamLibraries;

        // Add main Steam library
        var mainLibrary = Path.Combine(steamPath, "steamapps");
        if (Directory.Exists(mainLibrary))
            _cachedSteamLibraries.Add(mainLibrary);

        // Parse libraryfolders.vdf for additional libraries
        var libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (File.Exists(libraryFoldersPath))
        {
            try
            {
                var content = File.ReadAllText(libraryFoldersPath);
                var additionalLibraries = ParseLibraryFolders(content);
                foreach (var lib in additionalLibraries)
                {
                    var libSteamApps = Path.Combine(lib, "steamapps");
                    if (Directory.Exists(libSteamApps) && !_cachedSteamLibraries.Contains(libSteamApps))
                        _cachedSteamLibraries.Add(libSteamApps);
                }
            }
            catch (Exception ex)
            {
                _logService.LogError("Failed to parse libraryfolders.vdf", ex);
            }
        }

        return _cachedSteamLibraries;
    }

    /// <summary>
    /// Parses Steam's libraryfolders.vdf to find additional library paths.
    /// </summary>
    private List<string> ParseLibraryFolders(string content)
    {
        var libraries = new List<string>();
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Look for "path" entries in VDF format
            if (trimmed.StartsWith("\"path\""))
            {
                var pathMatch = ExtractVdfValue(trimmed);
                if (!string.IsNullOrEmpty(pathMatch) && Directory.Exists(pathMatch))
                {
                    libraries.Add(pathMatch);
                }
            }
            // Also check for numbered entries (old format): "1" "D:\\SteamLibrary"
            else if (trimmed.Length > 0 && char.IsDigit(trimmed[0]))
            {
                var parts = trimmed.Split('"');
                if (parts.Length >= 4)
                {
                    var path = parts[3].Replace("\\\\", "\\");
                    if (Directory.Exists(path))
                        libraries.Add(path);
                }
            }
        }

        return libraries;
    }

    /// <summary>
    /// Extracts value from VDF "key" "value" format.
    /// </summary>
    private string? ExtractVdfValue(string line)
    {
        var parts = line.Split('"');
        if (parts.Length >= 4)
        {
            return parts[3].Replace("\\\\", "\\");
        }
        return null;
    }

    /// <summary>
    /// Gets the Unturned installation path.
    /// </summary>
    public string? GetUnturnedPath()
    {
        if (_cachedUnturnedPath != null && Directory.Exists(_cachedUnturnedPath))
            return _cachedUnturnedPath;

        // Search in all Steam libraries first
        var libraries = GetSteamLibraries();
        foreach (var library in libraries)
        {
            var unturnedPath = Path.Combine(library, "common", "Unturned");
            if (Directory.Exists(unturnedPath))
            {
                _cachedUnturnedPath = unturnedPath;
                return _cachedUnturnedPath;
            }
        }

        // Try to find via Steam app manifest
        foreach (var library in libraries)
        {
            var manifestPath = Path.Combine(library, "appmanifest_304930.acf"); // Unturned AppID
            if (File.Exists(manifestPath))
            {
                try
                {
                    var content = File.ReadAllText(manifestPath);
                    var installDir = ExtractInstallDir(content);
                    if (!string.IsNullOrEmpty(installDir))
                    {
                        var fullPath = Path.Combine(library, "common", installDir);
                        if (Directory.Exists(fullPath))
                        {
                            _cachedUnturnedPath = fullPath;
                            return _cachedUnturnedPath;
                        }
                    }
                }
                catch { }
            }
        }

        // Fallback: search all drives for Unturned
        _cachedUnturnedPath = SearchAllDrives("Unturned", "Unturned.exe");
        return _cachedUnturnedPath;
    }

    /// <summary>
    /// Extracts installdir from Steam app manifest.
    /// </summary>
    private string? ExtractInstallDir(string manifestContent)
    {
        var lines = manifestContent.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("\"installdir\""))
            {
                return ExtractVdfValue(line.Trim());
            }
        }
        return null;
    }

    /// <summary>
    /// Searches all available drives for a folder containing a specific file.
    /// </summary>
    public string? SearchAllDrives(string folderName, string? verifyFile = null)
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable))
            .Select(d => d.Name)
            .ToList();

        // Common installation paths to check first (faster)
        var commonSubPaths = new[]
        {
            "Program Files (x86)",
            "Program Files",
            "Games",
            "SteamLibrary",
            "Steam",
            "" // Root
        };

        foreach (var drive in drives)
        {
            foreach (var subPath in commonSubPaths)
            {
                var basePath = string.IsNullOrEmpty(subPath) ? drive : Path.Combine(drive, subPath);
                if (!Directory.Exists(basePath)) continue;

                // Direct check
                var directPath = Path.Combine(basePath, folderName);
                if (Directory.Exists(directPath))
                {
                    if (string.IsNullOrEmpty(verifyFile) || File.Exists(Path.Combine(directPath, verifyFile)))
                        return directPath;
                }

                // Check Steam structure
                var steamPath = Path.Combine(basePath, "steamapps", "common", folderName);
                if (Directory.Exists(steamPath))
                {
                    if (string.IsNullOrEmpty(verifyFile) || File.Exists(Path.Combine(steamPath, verifyFile)))
                        return steamPath;
                }
            }
        }

        // Deep search as last resort (limited depth for performance)
        foreach (var drive in drives)
        {
            try
            {
                var result = DeepSearch(drive, folderName, verifyFile, maxDepth: 4);
                if (result != null) return result;
            }
            catch { /* Access denied or other error */ }
        }

        return null;
    }

    /// <summary>
    /// Performs a depth-limited search for a folder.
    /// </summary>
    private string? DeepSearch(string basePath, string folderName, string? verifyFile, int maxDepth, int currentDepth = 0)
    {
        if (currentDepth > maxDepth) return null;

        try
        {
            var directories = Directory.GetDirectories(basePath);
            foreach (var dir in directories)
            {
                var dirName = Path.GetFileName(dir);

                // Skip system and hidden directories
                if (IsExcludedDirectory(dirName)) continue;

                if (dirName.Equals(folderName, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(verifyFile) || File.Exists(Path.Combine(dir, verifyFile)))
                        return dir;
                }

                // Recurse
                var result = DeepSearch(dir, folderName, verifyFile, maxDepth, currentDepth + 1);
                if (result != null) return result;
            }
        }
        catch { /* Access denied */ }

        return null;
    }

    /// <summary>
    /// Checks if a directory should be excluded from search.
    /// </summary>
    private bool IsExcludedDirectory(string dirName)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Windows", "Windows.old", "$Recycle.Bin", "System Volume Information",
            "ProgramData", "Recovery", "PerfLogs", "Config.Msi", "$WinREAgent",
            "node_modules", ".git", ".svn", "AppData", "Application Data"
        };
        return excluded.Contains(dirName) || dirName.StartsWith("$");
    }

    /// <summary>
    /// Gets Unturned screenshots path.
    /// </summary>
    public string? GetUnturnedScreenshotsPath()
    {
        var unturnedPath = GetUnturnedPath();
        if (string.IsNullOrEmpty(unturnedPath)) return null;

        var screenshotsPath = Path.Combine(unturnedPath, "Screenshots");
        return Directory.Exists(screenshotsPath) ? screenshotsPath : null;
    }

    /// <summary>
    /// Clears cached paths to force re-discovery.
    /// </summary>
    public void ClearCache()
    {
        _cachedSteamPath = null;
        _cachedUnturnedPath = null;
        _cachedSteamLibraries = null;
    }
}
