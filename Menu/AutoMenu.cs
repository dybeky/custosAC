using CustosAC.Abstractions;
using CustosAC.Configuration;
using CustosAC.Models;
using Microsoft.Extensions.Options;

namespace CustosAC.Menu;

/// <summary>
/// Меню автоматической проверки с DI и async сканерами
/// </summary>
public class AutoMenu
{
    private readonly IConsoleUI _consoleUI;
    private readonly IScannerFactory _scannerFactory;
    private readonly IProcessService _processService;
    private readonly AppSettings _appSettings;
    private readonly ExternalResourceSettings _externalSettings;

    public AutoMenu(
        IConsoleUI consoleUI,
        IScannerFactory scannerFactory,
        IProcessService processService,
        IOptions<AppSettings> appSettings,
        IOptions<ExternalResourceSettings> externalSettings)
    {
        _consoleUI = consoleUI;
        _scannerFactory = scannerFactory;
        _processService = processService;
        _appSettings = appSettings.Value;
        _externalSettings = externalSettings.Value;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            _consoleUI.PrintHeader();
            _consoleUI.PrintMenu("АВТОМАТИЧЕСКАЯ ПРОВЕРКА", new[]
            {
                "Автосканирование AppData",
                "Автосканирование системных папок",
                "Автосканирование Prefetch",
                "Поиск в реестре по ключевым словам",
                "Парсинг Steam аккаунтов",
                "Проверка сайтов (oplata.info, funpay.com)",
                "Проверка Telegram (боты и загрузки)",
                "────────────────────────────────",
                "> ЗАПУСТИТЬ ВСЕ ПРОВЕРКИ"
            }, true);

            int choice = _consoleUI.GetChoice(8);

            switch (choice)
            {
                case 0:
                    return;
                case 1:
                    await RunScannerAsync(_scannerFactory.CreateAppDataScanner());
                    break;
                case 2:
                    await RunScannerAsync(_scannerFactory.CreateSystemScanner());
                    break;
                case 3:
                    await RunScannerAsync(_scannerFactory.CreatePrefetchScanner());
                    break;
                case 4:
                    await RunScannerAsync(_scannerFactory.CreateRegistryScanner());
                    break;
                case 5:
                    await RunScannerAsync(_scannerFactory.CreateSteamScanner());
                    break;
                case 6:
                    await CheckWebsitesAsync();
                    break;
                case 7:
                    await CheckTelegramAsync();
                    break;
                case 8:
                    await RunAllScansAsync();
                    break;
            }
        }
    }

    private async Task RunScannerAsync(IScanner scanner)
    {
        _consoleUI.PrintHeader();
        Console.WriteLine($"\n\x1b[36m\x1b[1m═══ {scanner.Name.ToUpper()} ═══\x1b[0m\n");
        Console.WriteLine($"  \x1b[34m[i]\x1b[0m {scanner.Description}\n");

        _consoleUI.Log("Начинается сканирование...", true);
        Console.WriteLine();

        var result = await scanner.ScanAsync();

        DisplayScanResult(result);
        _consoleUI.Pause();
    }

    private async Task RunAllScansAsync()
    {
        _consoleUI.PrintHeader();
        Console.WriteLine("\n\x1b[36m\x1b[1m═══ ЗАПУСК ВСЕХ АВТОМАТИЧЕСКИХ ПРОВЕРОК ═══\x1b[0m\n");

        var scanners = _scannerFactory.CreateAllScanners().ToArray();
        var allResults = new List<ScanResult>();

        for (int i = 0; i < scanners.Length; i++)
        {
            var scanner = scanners[i];
            Console.WriteLine($"\x1b[33m[{i + 1}/{scanners.Length}] {scanner.Name}...\x1b[0m");

            var result = await scanner.ScanAsync();
            allResults.Add(result);

            if (result.HasFindings)
            {
                Console.WriteLine($"  \x1b[31m\x1b[1mНайдено: {result.Count}\x1b[0m");
            }
            else
            {
                Console.WriteLine($"  \x1b[32mЧисто\x1b[0m");
            }
            Console.WriteLine();
        }

        // Проверка сайтов и Telegram
        Console.WriteLine($"\x1b[33m[{scanners.Length + 1}/{scanners.Length + 2}] Проверка сайтов...\x1b[0m");
        await CheckWebsitesAsync(silent: true);

        Console.WriteLine($"\x1b[33m[{scanners.Length + 2}/{scanners.Length + 2}] Проверка Telegram...\x1b[0m");
        await CheckTelegramAsync(silent: true);

        // Итог
        _consoleUI.PrintHeader();
        Console.WriteLine("\n\x1b[32m\x1b[1m═══ ВСЕ ПРОВЕРКИ ЗАВЕРШЕНЫ ═══\x1b[0m\n");

        var totalFindings = allResults.Sum(r => r.Count);
        if (totalFindings > 0)
        {
            _consoleUI.Log($"Всего найдено подозрительных элементов: {totalFindings}", false);
        }
        else
        {
            _consoleUI.Log("Подозрительных элементов не найдено!", true);
        }

        _consoleUI.Pause();
    }

    private void DisplayScanResult(ScanResult result)
    {
        if (result.HasFindings)
        {
            Console.WriteLine($"\x1b[31m\x1b[1m  Найдено подозрительных элементов: {result.Count}\x1b[0m\n");

            foreach (var finding in result.Findings.Take(20))
            {
                Console.WriteLine($"  \x1b[36m[>]\x1b[0m {finding}");
            }

            if (result.Count > 20)
            {
                Console.WriteLine($"\n  ... и ещё {result.Count - 20} элементов");
            }

            Console.WriteLine($"\n\x1b[32m[V]\x1b[0m - Просмотреть все постранично");
            Console.WriteLine($"\x1b[36m[0]\x1b[0m - Продолжить");
            Console.Write($"\n\x1b[32m\x1b[1m[>]\x1b[0m Выберите действие: ");

            var choice = Console.ReadLine()?.ToLower().Trim();
            if (choice == "v")
            {
                _consoleUI.DisplayFilesWithPagination(result.Findings, _appSettings.Console.ItemsPerPage);
            }
        }
        else
        {
            _consoleUI.Log("Подозрительных элементов не найдено", true);
        }

        Console.WriteLine($"\n  \x1b[2mВремя сканирования: {result.Duration.TotalSeconds:F2}с\x1b[0m");
    }

    private async Task CheckWebsitesAsync(bool silent = false)
    {
        if (!silent)
        {
            _consoleUI.PrintHeader();
            Console.WriteLine("\n\x1b[36m\x1b[1m═══ ПРОВЕРКА САЙТОВ ═══\x1b[0m\n");
            Console.WriteLine("  \x1b[34m[i]\x1b[0m Открываем сайты для проверки доступности...\n");
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
            Console.WriteLine("\n\x1b[33m\x1b[1mЧТО ПРОВЕРИТЬ:\x1b[0m");
            Console.WriteLine("  \x1b[36m[>]\x1b[0m Доступность сайтов (открываются ли страницы)");
            Console.WriteLine("  \x1b[36m[>]\x1b[0m Нет ли редиректов на подозрительные домены");
            Console.WriteLine("  \x1b[36m[>]\x1b[0m Корректность отображения сайтов");
            _consoleUI.Pause();
        }
    }

    private async Task CheckTelegramAsync(bool silent = false)
    {
        if (!silent)
        {
            _consoleUI.PrintHeader();
            Console.WriteLine("\n\x1b[36m\x1b[1m═══ ПРОВЕРКА TELEGRAM ═══\x1b[0m\n");
            Console.WriteLine("  \x1b[34m[i]\x1b[0m Открываем Telegram ботов для проверки...\n");
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
            Console.WriteLine("\n\x1b[36m─────────────────────────────────────────\x1b[0m\n");
            Console.WriteLine("  \x1b[34m[i]\x1b[0m Поиск папки загрузок Telegram...\n");

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

            Console.WriteLine("\n\x1b[33m\x1b[1mЧТО ПРОВЕРИТЬ В TELEGRAM:\x1b[0m");
            Console.WriteLine("  \x1b[36m[>]\x1b[0m Историю переписки с ботами");
            Console.WriteLine("  \x1b[36m[>]\x1b[0m Загруженные файлы (.exe, .dll, .bat, .zip)");
            Console.WriteLine("  \x1b[36m[>]\x1b[0m Подозрительные архивы и установщики");
            _consoleUI.Pause();
        }
    }
}
