namespace CustosAC.Core.Configuration;

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
        "undead", "melony", "fecurity", "ancient",
        "medusa", "mason", "midnight", "fatality",
        "memesense", "xnor", "aimbot", "wallhack",
        "triggerbot", "norecoil", "speedhack",
        "hwid_spoofer", "unturned_cheat", "unturnedcheat",
        "unturnex", "uloader", "ucheats", "utools",
        "cheatengine", "megadumper", "extremedumper",
        "neverlose", "nixware"
    };

    /// <summary>
    /// Keywords for exact match only.
    /// Short words that might cause false positives
    /// </summary>
    public string[] ExactMatch { get; set; } =
    {
        "esp", "hwid", "spoofer"
    };
}
