using CustosAC.Abstractions;
using CustosAC.Configuration;
using Microsoft.Extensions.Options;

namespace CustosAC.Services;

/// <summary>
/// Сервис проверки внешних ресурсов (сайты, Telegram)
/// </summary>
public class ExternalCheckService : IExternalCheckService
{
    private readonly IConsoleUI _consoleUI;
    private readonly IProcessService _processService;
    private readonly ExternalResourceSettings _externalSettings;

    public ExternalCheckService(
        IConsoleUI consoleUI,
        IProcessService processService,
        IOptions<ExternalResourceSettings> externalSettings)
    {
        _consoleUI = consoleUI;
        _processService = processService;
        _externalSettings = externalSettings.Value;
    }

    public async Task CheckWebsitesAsync(bool silent = false)
    {
        if (!silent)
        {
            _consoleUI.PrintHeader();
            _consoleUI.PrintSectionHeader("ПРОВЕРКА САЙТОВ");
            _consoleUI.PrintInfo("Открываем сайты для проверки доступности...");
            _consoleUI.PrintEmptyLine();
        }

        foreach (var website in _externalSettings.WebsitesToCheck)
        {
            await _processService.OpenUrlAsync(website.Url);
            if (!silent)
            {
                _consoleUI.Log($"Открыт: {website.Name}", true);
            }
        }

        if (!silent)
        {
            _consoleUI.PrintHint("ЧТО ПРОВЕРИТЬ:");
            _consoleUI.PrintListItem("Доступность сайтов (открываются ли страницы)");
            _consoleUI.PrintListItem("Нет ли редиректов на подозрительные домены");
            _consoleUI.PrintListItem("Корректность отображения сайтов");
            _consoleUI.Pause();
        }
    }

    public async Task CheckTelegramAsync(bool silent = false)
    {
        if (!silent)
        {
            _consoleUI.PrintHeader();
            _consoleUI.PrintSectionHeader("ПРОВЕРКА TELEGRAM");
            _consoleUI.PrintInfo("Открываем Telegram ботов для проверки...");
            _consoleUI.PrintEmptyLine();
        }

        foreach (var bot in _externalSettings.TelegramBots)
        {
            var telegramUrl = $"tg://resolve?domain={bot.Username.TrimStart('@')}";
            await _processService.OpenUrlAsync(telegramUrl);
            if (!silent)
            {
                _consoleUI.Log($"Открыт: {bot.Name} ({bot.Username})", true);
            }
        }

        if (!silent)
        {
            // Поиск папки загрузок Telegram
            _consoleUI.PrintSeparator();
            _consoleUI.PrintInfo("Поиск папки загрузок Telegram...");
            _consoleUI.PrintEmptyLine();

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var possiblePaths = new[]
            {
                Path.Combine(userProfile, "Downloads", "Telegram Desktop"),
                Path.Combine(userProfile, "Downloads"),
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    _consoleUI.Log($"Найдена папка загрузок: {path}", true);
                    await _processService.OpenFolderAsync(path);
                    break;
                }
            }

            _consoleUI.PrintHint("ЧТО ПРОВЕРИТЬ В TELEGRAM:");
            _consoleUI.PrintListItem("Историю переписки с ботами");
            _consoleUI.PrintListItem("Загруженные файлы (.exe, .dll, .bat, .zip)");
            _consoleUI.PrintListItem("Подозрительные архивы и установщики");
            _consoleUI.Pause();
        }
    }
}
