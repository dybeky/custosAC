using CustosAC.Abstractions;
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

    private async Task RunScannerAsync(IScanner scanner)
    {
        _consoleUI.PrintHeader();
        _consoleUI.PrintSectionHeader(scanner.Name);
        _consoleUI.PrintInfo(scanner.Description);
        _consoleUI.PrintEmptyLine();

        _consoleUI.Log("Начинается сканирование...", true);
        _consoleUI.PrintEmptyLine();

        var result = await scanner.ScanAsync();

        DisplayScanResult(result);
        _consoleUI.Pause();
    }

    private async Task RunAllScansAsync()
    {
        _consoleUI.PrintHeader();
        _consoleUI.PrintSectionHeader("ЗАПУСК ВСЕХ АВТОМАТИЧЕСКИХ ПРОВЕРОК");

        var scanners = new IScanner[]
        {
            CreateAppDataScanner(),
            CreateSystemScanner(),
            CreatePrefetchScanner(),
            CreateRegistryScanner(),
            CreateSteamScanner()
        };

        var allResults = new List<ScanResult>();
        var totalSteps = scanners.Length + 2;

        for (int i = 0; i < scanners.Length; i++)
        {
            var scanner = scanners[i];
            _consoleUI.PrintWarning($"[{i + 1}/{totalSteps}] {scanner.Name}...");

            var result = await scanner.ScanAsync();
            allResults.Add(result);

            if (result.HasFindings)
            {
                _consoleUI.PrintError($"  Найдено: {result.Count}");
            }
            else
            {
                _consoleUI.PrintSuccess("  Чисто");
            }
            _consoleUI.PrintEmptyLine();
        }

        // Проверка сайтов и Telegram
        _consoleUI.PrintWarning($"[{scanners.Length + 1}/{totalSteps}] Проверка сайтов...");
        await _externalCheckService.CheckWebsitesAsync(silent: true);

        _consoleUI.PrintWarning($"[{totalSteps}/{totalSteps}] Проверка Telegram...");
        await _externalCheckService.CheckTelegramAsync(silent: true);

        // Итог
        _consoleUI.PrintHeader();
        _consoleUI.PrintSectionHeader("ВСЕ ПРОВЕРКИ ЗАВЕРШЕНЫ");

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
            _consoleUI.PrintError($"  Найдено подозрительных элементов: {result.Count}");
            _consoleUI.PrintEmptyLine();

            foreach (var finding in result.Findings.Take(20))
            {
                _consoleUI.PrintListItem(finding);
            }

            if (result.Count > 20)
            {
                _consoleUI.PrintEmptyLine();
                _consoleUI.PrintInfo($"... и ещё {result.Count - 20} элементов");
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

    private IScanner CreateAppDataScanner()
    {
        return new AppDataScannerAsync(_keywordMatcher, _consoleUI, _scanSettings);
    }

    private IScanner CreateSystemScanner()
    {
        return new SystemScannerAsync(_keywordMatcher, _consoleUI, _scanSettings, _pathSettings);
    }

    private IScanner CreatePrefetchScanner()
    {
        return new PrefetchScannerAsync(_keywordMatcher, _consoleUI, _scanSettings, _pathSettings);
    }

    private IScanner CreateRegistryScanner()
    {
        return new RegistryScannerAsync(_keywordMatcher, _consoleUI, _registryService, _scanSettings, _registrySettings);
    }

    private IScanner CreateSteamScanner()
    {
        return new SteamScannerAsync(_keywordMatcher, _consoleUI, _scanSettings, _pathSettings);
    }
}
