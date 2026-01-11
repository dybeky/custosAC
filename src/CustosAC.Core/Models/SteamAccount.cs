namespace CustosAC.Core.Models;

/// <summary>
/// Steam account information
/// </summary>
public class SteamAccount
{
    /// <summary>Steam ID (64-bit)</summary>
    public string SteamId { get; set; } = string.Empty;

    /// <summary>Account login name</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Display name (nickname)</summary>
    public string PersonaName { get; set; } = string.Empty;

    /// <summary>Whether password is remembered</summary>
    public bool RememberPassword { get; set; }

    /// <summary>Last login timestamp</summary>
    public long Timestamp { get; set; }

    /// <summary>Steam profile URL</summary>
    public string ProfileUrl => $"https://steamcommunity.com/profiles/{SteamId}";

    public override string ToString()
    {
        return $"{PersonaName} ({AccountName}) - {SteamId}";
    }
}
