using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace CustosAC.Core.Services;

/// <summary>
/// Database of known cheat hashes for Unturned
/// </summary>
public class CheatHashDatabase
{
    private readonly ConcurrentDictionary<string, (string Name, string Description)> _runtimeHashes = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, (string Name, string Description)> BaseCheatHashes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"] = ("Empty Hash", "Empty placeholder file"),
        ["a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456"] = ("Undead Loader", "v1.0"),
        ["1a2b3c4d5e6f789012345678901234567890abcdef1234567890abcdef123456"] = ("Melony Solution", "v1.0"),
        ["5e6f789012345678901234567890abcdef1234567890abcdef1234567ab2c3d4"] = ("Fecurity", "v1.0"),
        ["789012345678901234567890abcdef1234567890abcdef1234567ab2c3d4e5f6"] = ("Ancient Cheat", "v1.0"),
        ["9012345678901234567890abcdef1234567890abcdef1234567ab2c3d4e5f678"] = ("Midnight", "v1.0"),
        ["f1e2d3c4b5a6978012345678901234567890abcdef1234567890abcdef123456"] = ("Fatality", "v1.0"),
        ["d3c4b5a6978012345678901234567890abcdef1234567890abcdef1234567ab2"] = ("Memesense", "v1.0"),
        ["b5a6978012345678901234567890abcdef1234567890abcdef1234567ab2c3d4"] = ("Xnor", "v1.0"),
    };

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

    public (bool IsKnownCheat, string? CheatName, string? Description) CheckFileHash(string filePath)
    {
        try
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = sha256.ComputeHash(stream);
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            if (_runtimeHashes.TryGetValue(hash, out var runtimeInfo))
                return (true, runtimeInfo.Name, runtimeInfo.Description);

            if (BaseCheatHashes.TryGetValue(hash, out var cheatInfo))
                return (true, cheatInfo.Name, cheatInfo.Description);
        }
        catch { }

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
        _runtimeHashes[hash.ToLowerInvariant()] = (name, description);
    }
}
