using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CustosAC.Core.Services;

/// <summary>
/// Database of known cheat hashes for Unturned with external JSON support
/// </summary>
public class CheatHashDatabase
{
    private readonly ConcurrentDictionary<string, CheatHashEntry> _runtimeHashes = new(StringComparer.OrdinalIgnoreCase);
    private readonly EnhancedLogService? _logService;
    private readonly string? _externalDatabasePath;

    // Legacy hardcoded hashes (kept for backward compatibility but should be replaced)
    private static readonly Dictionary<string, (string Name, string Description)> BaseCheatHashes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"] = ("Empty Hash", "Empty placeholder - REMOVE"),
        // Note: The following hashes appear to be placeholders with sequential patterns
        // Replace these with actual known cheat hashes
        ["a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456"] = ("Undead Loader", "v1.0 - PLACEHOLDER"),
        ["1a2b3c4d5e6f789012345678901234567890abcdef1234567890abcdef123456"] = ("Melony Solution", "v1.0 - PLACEHOLDER"),
        ["5e6f789012345678901234567890abcdef1234567890abcdef1234567890abcd"] = ("Fecurity", "v1.0 - PLACEHOLDER"),
        ["789012345678901234567890abcdef1234567890abcdef1234567ab2c3d4e5f6"] = ("Ancient Cheat", "v1.0 - PLACEHOLDER"),
        ["9012345678901234567890abcdef1234567890abcdef1234567ab2c3d4e5f678"] = ("Midnight", "v1.0 - PLACEHOLDER"),
        ["f1e2d3c4b5a6978012345678901234567890abcdef1234567890abcdef123456"] = ("Fatality", "v1.0 - PLACEHOLDER"),
        ["d3c4b5a6978012345678901234567890abcdef1234567890abcdef1234567ab2"] = ("Memesense", "v1.0 - PLACEHOLDER"),
        ["b5a6978012345678901234567890abcdef1234567890abcdef1234567ab2c3d4"] = ("Xnor", "v1.0 - PLACEHOLDER"),
    };

    public CheatHashDatabase(EnhancedLogService? logService = null, string? externalDatabasePath = null)
    {
        _logService = logService;
        _externalDatabasePath = externalDatabasePath ?? Path.Combine(AppContext.BaseDirectory, "cheat_hashes.json");

        // Load external database if available
        LoadExternalDatabase();
    }

    private static readonly HashSet<string> SuspiciousFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "undead", "undeadloader", "melony", "melonyloader", "fecurity",
        "ancient", "midnight", "fatality", "memesense", "xnor",
        "cheatengine", "injector", "loader", "bypass", "hwid_spoofer",
        "hwidspoofer", "aimbot", "wallhack", "esp", "triggerbot"
    };

    private static readonly string[] SuspiciousKeywords =
    [
        "undead", "melony", "fecurity", "ancient", "midnight", "fatality",
        "memesense", "xnor", "cheat", "inject", "loader", "bypass",
        "hwid", "spoof", "aimbot", "wallhack", "esp", "trigger"
    ];

    /// <summary>
    /// Loads external cheat hash database from JSON file
    /// </summary>
    private void LoadExternalDatabase()
    {
        if (string.IsNullOrEmpty(_externalDatabasePath) || !File.Exists(_externalDatabasePath))
        {
            _logService?.LogInfo(EnhancedLogService.LogCategory.General,
                $"External hash database not found: {_externalDatabasePath}");
            return;
        }

        try
        {
            var json = File.ReadAllText(_externalDatabasePath);
            var database = JsonSerializer.Deserialize<CheatHashDatabaseFile>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (database?.Hashes == null)
            {
                _logService?.LogWarning(EnhancedLogService.LogCategory.Validation,
                    "External hash database is empty or invalid");
                return;
            }

            foreach (var entry in database.Hashes)
            {
                if (!string.IsNullOrEmpty(entry.Sha256))
                {
                    _runtimeHashes[entry.Sha256.ToLowerInvariant()] = entry;
                }
            }

            _logService?.LogInfo(EnhancedLogService.LogCategory.General,
                $"Loaded {_runtimeHashes.Count} hash(es) from external database (v{database.Version})");
        }
        catch (Exception ex)
        {
            _logService?.LogError(EnhancedLogService.LogCategory.General,
                $"Failed to load external hash database: {ex.Message}", ex);
        }
    }

    public (bool IsKnownCheat, string? CheatName, string? Description) CheckFileHash(string filePath)
    {
        try
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = sha256.ComputeHash(stream);
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            // Check runtime/external hashes first
            if (_runtimeHashes.TryGetValue(hash, out var runtimeInfo))
            {
                _logService?.LogWarning(EnhancedLogService.LogCategory.Security,
                    $"Known cheat detected: {runtimeInfo.Name} | File: {Path.GetFileName(filePath)}",
                    "HashDatabase");
                return (true, runtimeInfo.Name, runtimeInfo.Version ?? runtimeInfo.Description);
            }

            // Check legacy base hashes
            if (BaseCheatHashes.TryGetValue(hash, out var cheatInfo))
            {
                _logService?.LogWarning(EnhancedLogService.LogCategory.Security,
                    $"Known cheat detected (legacy): {cheatInfo.Name} | File: {Path.GetFileName(filePath)}",
                    "HashDatabase");
                return (true, cheatInfo.Name, cheatInfo.Description);
            }
        }
        catch (Exception ex)
        {
            _logService?.LogDebug(EnhancedLogService.LogCategory.Validation,
                $"Error computing hash for {Path.GetFileName(filePath)}: {ex.Message}");
        }

        return (false, null, null);
    }

    public static bool IsSuspiciousFileName(string fileName)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();

        if (SuspiciousFileNames.Contains(nameWithoutExt))
            return true;

        foreach (var keyword in SuspiciousKeywords)
        {
            if (nameWithoutExt.Contains(keyword, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public int GetKnownHashCount() => BaseCheatHashes.Count + _runtimeHashes.Count;

    public void AddHash(string hash, string name, string description)
    {
        var entry = new CheatHashEntry
        {
            Sha256 = hash.ToLowerInvariant(),
            Name = name,
            Description = description,
            Version = "Unknown",
            Severity = "MEDIUM"
        };
        _runtimeHashes[hash.ToLowerInvariant()] = entry;
    }

    public void ReloadExternalDatabase()
    {
        _runtimeHashes.Clear();
        LoadExternalDatabase();
    }
}

/// <summary>
/// External hash database file structure
/// </summary>
public class CheatHashDatabaseFile
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("lastUpdate")]
    public string LastUpdate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

    [JsonPropertyName("hashes")]
    public List<CheatHashEntry> Hashes { get; set; } = new();
}

/// <summary>
/// Individual cheat hash entry with metadata
/// </summary>
public class CheatHashEntry
{
    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;

    [JsonPropertyName("sha1")]
    public string? Sha1 { get; set; }

    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "MEDIUM";

    [JsonPropertyName("aliases")]
    public List<string>? Aliases { get; set; }

    [JsonPropertyName("knownPaths")]
    public List<string>? KnownPaths { get; set; }

    [JsonPropertyName("fileSize")]
    public long? FileSize { get; set; }

    [JsonPropertyName("firstSeen")]
    public string? FirstSeen { get; set; }
}
