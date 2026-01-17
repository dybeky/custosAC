namespace Custos.Core.Configuration;

/// <summary>
/// Keyword settings for cheat detection
/// </summary>
public class KeywordSettings
{
    /// <summary>
    /// Keywords for substring matching
    /// </summary>
    public string[] Patterns { get; set; } =
    {
        // Cheat names
        "undead", "melony", "fecurity", "ancient",
        "medusa", "mason", "midnight", "fatality",
        "memesense", "xnor", "neverlose", "nixware",
        "uloader", "cheatengine", "norecoil",
        // Generic keywords
        "hack", "cheat", "soft", "чит",
        "aimbot", "wallhack", "esp", "hwid",
        "spoofer", "empty"
    };

    /// <summary>
    /// Keywords for exact match only (currently empty - all moved to Patterns)
    /// </summary>
    public string[] ExactMatch { get; set; } = Array.Empty<string>();
}
