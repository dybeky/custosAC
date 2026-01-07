namespace CustosAC.Abstractions;

/// <summary>
/// Интерфейс для проверки внешних ресурсов (сайты, Telegram)
/// </summary>
public interface IExternalCheckService
{
    /// <summary>Проверить сайты из конфигурации</summary>
    Task CheckWebsitesAsync(bool silent = false);

    /// <summary>Проверить Telegram ботов из конфигурации</summary>
    Task CheckTelegramAsync(bool silent = false);
}
