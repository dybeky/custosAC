using CustosAC.Configuration;
using CustosAC.Models;
using CustosAC.Scanner;
using CustosAC.Services;

namespace CustosAC.Menu;

/// <summary>
/// Меню автоматической проверки
/// </summary>
public class AutoMenu
{
    private readonly ConsoleUIService _consoleUI;
    private readonly ExternalCheckService _externalCheckService;
    private readonly AppSettings _appSettings;
    private readonly KeywordMatcherService _keywordMatcher;
    private readonly ScanSettings _scanSettings;
    private readonly PathSettings _pathSettings;
    private readonly RegistrySettings _registrySettings;
    private readonly RegistryService _registryService;

    public AutoMenu(
        ConsoleUIService consoleUI,
        ExternalCheckService externalCheckService,
        AppSettings appSettings,
        KeywordMatcherService keywordMatcher,
        ScanSettings scanSettings,
        PathSettings pathSettings,
        RegistrySettings registrySettings,
        RegistryService registryService)
    {
        _consoleUI = consoleUI;
        _externalCheckService = externalCheckService;
        _appSettings = appSettings;
        _keywordMatcher = keywordMatcher;
        _scanSettings = scanSettings;
        _pathSettings = pathSettings;
        _registrySettings = registrySettings;
        _registryService = registryService;
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
                    await RunScannerAsync(CreateAppDataScanner());
                    break;
                case 2:
                    await RunScannerAsync(CreateSystemScanner());
                    break;
                case 3:
                    await RunScannerAsync(CreatePrefetchScanner());
                    break;
                case 4:
                    await RunScannerAsync(CreateRegistryScanner());
                    break;
                case 5:
                    await RunScannerAsync(CreateSteamScanner());
                    break;
                case 6:
                    await _externalCheckService.CheckWebsitesAsync();
                    break;
                case 7:
                    await _externalCheckService.CheckTelegramAsync();
                    break;
                case 8:
                    await RunAllScansAsync();
                    break;
            }
        }
    }

    private async Task RunScannerAsync(BaseScannerAsync scanner)
    {
        using (scanner)
        {
            _consoleUI.PrintHeader();
            _consoleUI.PrintSectionHeader(scanner.Name);
            _consoleUI.PrintInfo(scanner.Description);
            _consoleUI.PrintEmptyLine();

            _consoleUI.Log("Начинается сканирование...", true);
            _consoleUI.PrintEmptyLine();

            var result = await scanner.ScanAsync();

            DisplayScanResult(result);
        }
        _consoleUI.Pause();
    }

    private async Task RunAllScansAsync()
    {
        var startTime = DateTime.Now;
        var allResults = new List<(string name, ScanResult result)>();

        // 1. AppData
        _consoleUI.PrintHeader();
        _consoleUI.PrintBoxOrange(new[] { "ПОЛНАЯ ПРОВЕРКА [1/7]", "", "Сканирование AppData..." });
        _consoleUI.PrintEmptyLine();

        using (var scanner = CreateAppDataScanner())
        {
            var result = await scanner.ScanAsync();
            allResults.Add(("AppData", result));
            DisplayStepResult("AppData", result);
        }
        WaitForNext("Нажмите Enter для следующей проверки...");

        // 2. System
        _consoleUI.PrintHeader();
        _consoleUI.PrintBoxOrange(new[] { "ПОЛНАЯ ПРОВЕРКА [2/7]", "", "Сканирование системных папок..." });
        _consoleUI.PrintEmptyLine();

        using (var scanner = CreateSystemScanner())
        {
            var result = await scanner.ScanAsync();
            allResults.Add(("System", result));
            DisplayStepResult("Системные папки", result);
        }
        WaitForNext("Нажмите Enter для следующей проверки...");

        // 3. Prefetch
        _consoleUI.PrintHeader();
        _consoleUI.PrintBoxOrange(new[] { "ПОЛНАЯ ПРОВЕРКА [3/7]", "", "Сканирование Prefetch..." });
        _consoleUI.PrintEmptyLine();

        using (var scanner = CreatePrefetchScanner())
        {
            var result = await scanner.ScanAsync();
            allResults.Add(("Prefetch", result));
            DisplayStepResult("Prefetch", result);
        }
        WaitForNext("Нажмите Enter для следующей проверки...");

        // 4. Registry
        _consoleUI.PrintHeader();
        _consoleUI.PrintBoxOrange(new[] { "ПОЛНАЯ ПРОВЕРКА [4/7]", "", "Поиск в реестре..." });
        _consoleUI.PrintEmptyLine();

        using (var scanner = CreateRegistryScanner())
        {
            var result = await scanner.ScanAsync();
            allResults.Add(("Registry", result));
            DisplayStepResult("Реестр", result);
        }
        WaitForNext("Нажмите Enter для следующей проверки...");

        // 5. Steam
        _consoleUI.PrintHeader();
        _consoleUI.PrintBoxOrange(new[] { "ПОЛНАЯ ПРОВЕРКА [5/7]", "", "Парсинг Steam аккаунтов..." });
        _consoleUI.PrintEmptyLine();

        using (var scanner = CreateSteamScanner())
        {
            var result = await scanner.ScanAsync();
            allResults.Add(("Steam", result));
            DisplayStepResult("Steam", result);
        }
        WaitForNext("Нажмите Enter для следующей проверки...");

        // 6. Websites
        _consoleUI.PrintHeader();
        _consoleUI.PrintBoxOrange(new[] { "ПОЛНАЯ ПРОВЕРКА [6/7]", "", "Проверка сайтов..." });
        _consoleUI.PrintEmptyLine();
        await _externalCheckService.CheckWebsitesAsync(silent: false);
        WaitForNext("Нажмите Enter для следующей проверки...");

        // 7. Telegram
        _consoleUI.PrintHeader();
        _consoleUI.PrintBoxOrange(new[] { "ПОЛНАЯ ПРОВЕРКА [7/7]", "", "Проверка Telegram..." });
        _consoleUI.PrintEmptyLine();
        await _externalCheckService.CheckTelegramAsync(silent: false);
        WaitForNext("Нажмите Enter для просмотра итогов...");

        // Итоговый отчёт
        ShowFinalReport(allResults, startTime);
    }

    private void DisplayStepResult(string name, ScanResult result)
    {
        _consoleUI.PrintEmptyLine();
        _consoleUI.PrintSeparator();
        _consoleUI.PrintEmptyLine();

        if (result.HasFindings)
        {
            _consoleUI.PrintError($"[{name}] НАЙДЕНО: {result.Count} элементов");
            _consoleUI.PrintEmptyLine();

            // Показываем первую часть результатов
            var previewCount = _appSettings.Console.ItemsPerPage / 2;
            foreach (var finding in result.Findings.Take(previewCount))
            {
                _consoleUI.PrintListItem(finding);
            }

            // Если больше - предлагаем посмотреть все
            if (result.Count > previewCount)
            {
                _consoleUI.PrintEmptyLine();
                _consoleUI.PrintWarning($"... и ещё {result.Count - previewCount} элементов");
                _consoleUI.PrintEmptyLine();
                _consoleUI.PrintHighlight("[V] - Показать все  |  [Enter] - Продолжить");

                var input = Console.ReadLine()?.ToLower().Trim();
                if (input == "v")
                {
                    ShowAllFindings(name, result.Findings);
                }
            }
        }
        else
        {
            _consoleUI.PrintSuccess($"[{name}] ЧИСТО - подозрительных элементов не найдено");
        }

        _consoleUI.PrintEmptyLine();
        _consoleUI.PrintInfo($"Время: {result.Duration.TotalSeconds:F2} сек");
    }

    private void ShowAllFindings(string name, List<string> findings)
    {
        int page = 0;
        int itemsPerPage = _appSettings.Console.ItemsPerPage;
        int totalPages = (findings.Count + itemsPerPage - 1) / itemsPerPage;

        while (true)
        {
            _consoleUI.PrintHeader();
            _consoleUI.PrintSectionHeader($"{name} - Все результаты (стр. {page + 1}/{totalPages})");
            _consoleUI.PrintEmptyLine();

            var pageItems = findings.Skip(page * itemsPerPage).Take(itemsPerPage).ToList();
            int startNum = page * itemsPerPage + 1;

            for (int i = 0; i < pageItems.Count; i++)
            {
                Console.WriteLine($"  [{startNum + i}] {pageItems[i]}");
            }

            _consoleUI.PrintEmptyLine();
            _consoleUI.PrintSeparator();
            _consoleUI.PrintEmptyLine();

            // Навигация
            var nav = new List<string>();
            if (page > 0) nav.Add("[P] Назад");
            if (page < totalPages - 1) nav.Add("[N] Вперёд");
            nav.Add("[Q] Выход");

            _consoleUI.PrintHighlight(string.Join("  |  ", nav));

            var input = Console.ReadLine()?.ToLower().Trim();

            if (input == "n" && page < totalPages - 1)
                page++;
            else if (input == "p" && page > 0)
                page--;
            else if (input == "q" || string.IsNullOrEmpty(input))
                break;
        }
    }

    private void WaitForNext(string message)
    {
        _consoleUI.PrintEmptyLine();
        _consoleUI.PrintHighlight(message);
        Console.ReadLine();
    }

    private void ShowFinalReport(List<(string name, ScanResult result)> allResults, DateTime startTime)
    {
        var elapsed = DateTime.Now - startTime;
        var totalFindings = allResults.Sum(r => r.result.Count);

        _consoleUI.PrintHeader();

        if (totalFindings > 0)
        {
            _consoleUI.PrintBox(new[]
            {
                "╔════════════════════════════════════════════╗",
                "║           ИТОГОВЫЙ ОТЧЁТ                   ║",
                "║      ОБНАРУЖЕНЫ ПОДОЗРИТЕЛЬНЫЕ ЭЛЕМЕНТЫ    ║",
                "╚════════════════════════════════════════════╝"
            }, false);
        }
        else
        {
            _consoleUI.PrintBox(new[]
            {
                "╔════════════════════════════════════════════╗",
                "║           ИТОГОВЫЙ ОТЧЁТ                   ║",
                "║              ВСЁ ЧИСТО!                    ║",
                "╚════════════════════════════════════════════╝"
            }, true);
        }

        _consoleUI.PrintEmptyLine();
        _consoleUI.PrintInfo($"Общее время проверки: {elapsed.TotalSeconds:F1} сек");
        _consoleUI.PrintEmptyLine();

        _consoleUI.PrintSectionHeader("РЕЗУЛЬТАТЫ ПО КАТЕГОРИЯМ");
        _consoleUI.PrintEmptyLine();

        foreach (var (name, result) in allResults)
        {
            if (result.HasFindings)
            {
                _consoleUI.PrintError($"  ✗ {name}: {result.Count} найдено");
            }
            else
            {
                _consoleUI.PrintSuccess($"  ✓ {name}: чисто");
            }
        }

        _consoleUI.PrintEmptyLine();
        _consoleUI.PrintSeparator();
        _consoleUI.PrintEmptyLine();

        if (totalFindings > 0)
        {
            _consoleUI.PrintError($"  ВСЕГО НАЙДЕНО: {totalFindings} подозрительных элементов");
            _consoleUI.PrintEmptyLine();
            _consoleUI.PrintWarning("  Рекомендуется проверить найденные элементы вручную.");
        }
        else
        {
            _consoleUI.PrintSuccess("  СИСТЕМА ЧИСТА!");
            _consoleUI.PrintEmptyLine();
            _consoleUI.PrintInfo("  Подозрительных элементов не обнаружено.");
        }

        _consoleUI.PrintEmptyLine();
        _consoleUI.Pause();
    }

    private void DisplayScanResult(ScanResult result)
    {
        if (result.HasFindings)
        {
            _consoleUI.PrintError($"  Найдено подозрительных элементов: {result.Count}");
            _consoleUI.PrintEmptyLine();

            var displayCount = _appSettings.Console.ItemsPerPage;
            foreach (var finding in result.Findings.Take(displayCount))
            {
                _consoleUI.PrintListItem(finding);
            }

            if (result.Count > displayCount)
            {
                _consoleUI.PrintEmptyLine();
                _consoleUI.PrintInfo($"... и ещё {result.Count - displayCount} элементов");
            }

            _consoleUI.PrintEmptyLine();
            _consoleUI.PrintSuccess("[V] - Просмотреть все постранично");
            _consoleUI.PrintHighlight("[0] - Продолжить");
            _consoleUI.PrintEmptyLine();

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

        _consoleUI.PrintInfo($"Время сканирования: {result.Duration.TotalSeconds:F2}с");
    }

    private BaseScannerAsync CreateAppDataScanner()
    {
        return new AppDataScannerAsync(_keywordMatcher, _consoleUI, _scanSettings);
    }

    private BaseScannerAsync CreateSystemScanner()
    {
        return new SystemScannerAsync(_keywordMatcher, _consoleUI, _scanSettings, _pathSettings);
    }

    private BaseScannerAsync CreatePrefetchScanner()
    {
        return new PrefetchScannerAsync(_keywordMatcher, _consoleUI, _scanSettings, _pathSettings);
    }

    private BaseScannerAsync CreateRegistryScanner()
    {
        return new RegistryScannerAsync(_keywordMatcher, _consoleUI, _registryService, _scanSettings, _registrySettings);
    }

    private BaseScannerAsync CreateSteamScanner()
    {
        return new SteamScannerAsync(_keywordMatcher, _consoleUI, _scanSettings, _pathSettings);
    }
}
