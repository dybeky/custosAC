namespace CustosAC.Models;

/// <summary>
/// Информация об аккаунте Steam
/// </summary>
public class SteamAccount
{
    /// <summary>Steam ID (64-bit)</summary>
    public string SteamId { get; set; } = string.Empty;

    /// <summary>Имя аккаунта (login)</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Отображаемое имя (nickname)</summary>
    public string PersonaName { get; set; } = string.Empty;

    /// <summary>Запомнен ли пароль</summary>
    public bool RememberPassword { get; set; }

    /// <summary>Последний вход (timestamp)</summary>
    public long Timestamp { get; set; }

    /// <summary>URL профиля в Steam</summary>
    public string ProfileUrl => $"https://steamcommunity.com/profiles/{SteamId}";

    public override string ToString()
    {
        return $"{PersonaName} ({AccountName}) - {SteamId}";
    }
}
