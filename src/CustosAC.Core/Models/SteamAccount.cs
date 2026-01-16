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

    /// <summary>Last login timestamp (Unix)</summary>
    public long Timestamp { get; set; }

    /// <summary>Last login date/time (converted from Unix timestamp)</summary>
    public DateTime? LastLogin => Timestamp > 0
        ? DateTimeOffset.FromUnixTimeSeconds(Timestamp).LocalDateTime
        : null;

    /// <summary>Formatted last login string</summary>
    public string LastLoginFormatted => LastLogin.HasValue
        ? LastLogin.Value.ToString("dd.MM.yyyy HH:mm")
        : "Unknown";

    /// <summary>Steam profile URL</summary>
    public string ProfileUrl => $"https://steamcommunity.com/profiles/{SteamId}";

    public override string ToString()
    {
        return $"{PersonaName} ({AccountName}) - {SteamId}";
    }
}
