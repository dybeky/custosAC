using CustosAC.Abstractions;
using CustosAC.Configuration;
using Microsoft.Extensions.Options;

namespace CustosAC.Menu;

/// <summary>
/// Меню ручной проверки с DI
/// </summary>
public class ManualMenu
{
    private readonly IConsoleUI _consoleUI;
    private readonly IProcessService _processService;
    private readonly IKeywordMatcher _keywordMatcher;
    private readonly IRegistryService _registryService;
    private readonly IScannerFactory _scannerFactory;
    private readonly IExternalCheckService _externalCheckService;
    private readonly PathSettings _pathSettings;
    private readonly RegistrySettings _registrySettings;

    public ManualMenu(
        IConsoleUI consoleUI,
        IProcessService processService,
        IKeywordMatcher keywordMatcher,
        IRegistryService registryService,
        IScannerFactory scannerFactory,
        IExternalCheckService externalCheckService,
        IOptions<PathSettings> pathSettings,
        IOptions<RegistrySettings> registrySettings)
    {
        _consoleUI = consoleUI;
        _processService = processService;
        _keywordMatcher = keywordMatcher;
        _registryService = registryService;
        _scannerFactory = scannerFactory;
        _externalCheckService = externalCheckService;
        _pathSettings = pathSettings.Value;
        _registrySettings = registrySettings.Value;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            _consoleUI.PrintHeader();
            _consoleUI.PrintMenu("РУЧНАЯ ПРОВЕРКА", new[]
            {
                "Сеть и интернет",
                "Защита Windows",
                "Утилиты",
                "Системные папки",
                "Реестр Windows",
                "Проверка Steam аккаунтов",
                "Unturned",
                "Проверка сайтов (oplata.info, funpay.com)",
                "Проверка Telegram (боты и загрузки)",
                "Скопировать ключевые слова"
            }, true);

            int choice = _consoleUI.GetChoice(10);

            switch (choice)
            {
                case 0:
                    return;
                case 1:
                    await NetworkMenuAsync();
                    break;
                case 2:
                    await DefenderMenuAsync();
                    break;
                case 3:
                    await UtilitiesMenuAsync();
                    break;
                case 4:
                    await FoldersMenuAsync();
                    break;
                case 5:
                    await RegistryMenuAsync();
                    break;
                case 6:
                    await SteamCheckMenuAsync();
                    break;
                case 7:
                    await UnturnedMenuAsync();
                    break;
                case 8:
                    await _externalCheckService.CheckWebsitesAsync();
                    break;
                case 9:
                    await _externalCheckService.CheckTelegramAsync();
                    break;
                case 10:
                    CopyKeywordsToClipboard();
                    break;
            }
        }
    }

    private async Task NetworkMenuAsync()
    {
        _consoleUI.PrintHeader();
        _consoleUI.PrintSectionHeader("СЕТЬ И ИНТЕРНЕТ");

        await _processService.OpenUrlAsync("ms-settings:datausage");

        _consoleUI.PrintHint("ЧТО НУЖНО ПРОВЕРИТЬ:");
        _consoleUI.PrintListItem("Неизвестные .exe файлы с сетевой активностью");
        _consoleUI.PrintListItem("Подозрительные названия процессов");
        _consoleUI.PrintListItem("Большой объем переданных данных");
        _consoleUI.Pause();
    }

    private async Task DefenderMenuAsync()
    {
        _consoleUI.PrintHeader();
        _consoleUI.PrintSectionHeader("ЗАЩИТА WINDOWS");

        await _processService.OpenUrlAsync("windowsdefender://threat/");

        _consoleUI.PrintHint("КЛЮЧЕВЫЕ СЛОВА ДЛЯ ПОИСКА:");
        _consoleUI.PrintListItem("undead, melony, ancient, loader, xnor");
        _consoleUI.PrintListItem("hack, cheat, unturned, bypass");
        _consoleUI.PrintListItem("inject, overlay, esp, aimbot");
        _consoleUI.Pause();
    }

    private async Task UtilitiesMenuAsync()
    {
        _consoleUI.PrintHeader();
        _consoleUI.PrintSectionHeader("УТИЛИТЫ");
        _consoleUI.PrintInfo("Открываем ссылки на утилиты для проверки...");
        _consoleUI.PrintEmptyLine();

        await _processService.OpenUrlAsync("https://www.voidtools.com/downloads/");
        _consoleUI.Log("Everything (поиск файлов)", true);

        await _processService.OpenUrlAsync("https://www.nirsoft.net/utils/computer_activity_view.html");
        _consoleUI.Log("ComputerActivityView", true);

        await _processService.OpenUrlAsync("https://www.nirsoft.net/utils/usb_devices_view.html");
        _consoleUI.Log("USBDevicesView", true);

        await _processService.OpenUrlAsync("https://privazer.com/en/download-shellbag-analyzer-shellbag-cleaner.php");
        _consoleUI.Log("ShellBag Analyzer", true);

        _consoleUI.PrintHint("УТИЛИТЫ:");
        _consoleUI.PrintListItem("Everything - быстрый поиск файлов на ПК");
        _consoleUI.PrintListItem("ComputerActivityView - активность компьютера");
        _consoleUI.PrintListItem("USBDevicesView - история USB устройств");
        _consoleUI.PrintListItem("ShellBag Analyzer - анализ посещенных папок");
        _consoleUI.Pause();
    }

    private async Task FoldersMenuAsync()
    {
        while (true)
        {
            _consoleUI.PrintHeader();
            _consoleUI.PrintMenu("СИСТЕМНЫЕ ПАПКИ", new[]
            {
                @"AppData\Roaming",
                @"AppData\Local",
                @"AppData\LocalLow",
                "Videos (видео)",
                "Prefetch (запущенные .exe)",
                "Открыть все"
            }, true);

            int choice = _consoleUI.GetChoice(6);
            if (choice == 0)
                break;

            _consoleUI.PrintHeader();

            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localappdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            switch (choice)
            {
                case 1:
                    await OpenFolderAsync(appdata, @"AppData\Roaming");
                    break;
                case 2:
                    await OpenFolderAsync(localappdata, @"AppData\Local");
                    break;
                case 3:
                    await OpenFolderAsync(Path.Combine(userprofile, "AppData", "LocalLow"), @"AppData\LocalLow");
                    break;
                case 4:
                    await OpenFolderAsync(Path.Combine(userprofile, "Videos"), "Videos");
                    break;
                case 5:
                    await OpenFolderAsync(_pathSettings.Windows.PrefetchPath, "Prefetch");
                    break;
                case 6:
                    await OpenFolderAsync(appdata, "Roaming");
                    await OpenFolderAsync(localappdata, "Local");
                    await OpenFolderAsync(Path.Combine(userprofile, "AppData", "LocalLow"), "LocalLow");
                    await OpenFolderAsync(Path.Combine(userprofile, "Videos"), "Videos");
                    await OpenFolderAsync(_pathSettings.Windows.PrefetchPath, "Prefetch");
                    break;
            }
            _consoleUI.Pause();
        }
    }

    private async Task RegistryMenuAsync()
    {
        while (true)
        {
            _consoleUI.PrintHeader();
            _consoleUI.PrintMenu("РЕЕСТР WINDOWS", new[]
            {
                "Открыть regedit",
                "MuiCache (запущенные программы)",
                "AppSwitched (переключения Alt+Tab)",
                "ShowJumpView (JumpList история)"
            }, true);

            int choice = _consoleUI.GetChoice(4);
            if (choice == 0)
                break;

            _consoleUI.PrintHeader();
            switch (choice)
            {
                case 1:
                    await _processService.OpenUrlAsync("regedit.exe");
                    _consoleUI.Log("Regedit открыт", true);
                    break;
                case 2:
                    await OpenRegistryAsync(_registrySettings.ScanKeys[0].Path);
                    break;
                case 3:
                    await OpenRegistryAsync(_registrySettings.ScanKeys[1].Path);
                    break;
                case 4:
                    await OpenRegistryAsync(_registrySettings.ScanKeys[2].Path);
                    break;
            }
            _consoleUI.Pause();
        }
    }

    private async Task SteamCheckMenuAsync()
    {
        _consoleUI.PrintHeader();
        _consoleUI.PrintSectionHeader("ПРОВЕРКА STEAM АККАУНТОВ");

        var scanner = _scannerFactory.CreateSteamScanner();
        var result = await scanner.ScanAsync();

        if (result.Success && result.HasFindings)
        {
            foreach (var finding in result.Findings)
            {
                _consoleUI.PrintListItem(finding);
            }
            _consoleUI.Log($"Найдено аккаунтов Steam: {result.Count}", true);
        }
        else
        {
            _consoleUI.Log("Steam аккаунты не найдены", false);
        }

        _consoleUI.PrintSeparator();
        _consoleUI.PrintHint("ЧТО НУЖНО ПРОВЕРИТЬ:");
        _consoleUI.PrintListItem("Конфигурационные файлы Steam");
        _consoleUI.PrintListItem("Информация об аккаунтах");
        _consoleUI.PrintListItem("Логи и настройки");
        _consoleUI.Pause();
    }

    private async Task UnturnedMenuAsync()
    {
        _consoleUI.PrintHeader();
        _consoleUI.PrintSectionHeader("UNTURNED");

        var screenshotPaths = GetUnturnedScreenshotsPaths();
        var screenshots = screenshotPaths.FirstOrDefault(Directory.Exists);

        if (screenshots != null)
        {
            _consoleUI.PrintInfo($"Найдено: {screenshots}");
            _consoleUI.PrintEmptyLine();
            await OpenFolderAsync(screenshots, "Папка Screenshots Unturned");

            _consoleUI.PrintHint("ЧТО НУЖНО ПРОВЕРИТЬ:");
            _consoleUI.PrintListItem("UI читов на скриншотах");
            _consoleUI.PrintListItem("ESP/Wallhack индикаторы");
            _consoleUI.PrintListItem("Overlay меню");
            _consoleUI.PrintListItem("Необычные элементы интерфейса");
        }
        else
        {
            _consoleUI.Log(@"Папка Unturned\Screenshots не найдена", false);
            _consoleUI.PrintWarning("Unturned может быть не установлен");
        }

        _consoleUI.Pause();
    }

    private void CopyKeywordsToClipboard()
    {
        _consoleUI.PrintHeader();
        var keywords = _keywordMatcher.GetKeywordsString();
        _processService.CopyToClipboardAsync(keywords).Wait();
        _consoleUI.Log("Ключевые слова скопированы в буфер обмена!", true);
        _consoleUI.PrintWarning("Теперь можно вставить (Ctrl+V) в Everything и другие программы");
        _consoleUI.Pause();
    }

    private async Task OpenFolderAsync(string path, string description)
    {
        if (Directory.Exists(path))
        {
            await _processService.OpenFolderAsync(path);
            _consoleUI.Log($"Открыто: {description}", true);
        }
        else
        {
            _consoleUI.Log($"Папка не найдена: {path}", false);
        }
    }

    private async Task OpenRegistryAsync(string path)
    {
        await _processService.CopyToClipboardAsync(path);
        await _registryService.OpenRegistryEditorAsync(path);
        _consoleUI.Log($"Путь скопирован: {path}", true);
        _consoleUI.PrintWarning("Вставьте путь в regedit (Ctrl+V)");
    }

    private List<string> GetUnturnedScreenshotsPaths()
    {
        var paths = new List<string>();
        var relativePath = _pathSettings.Steam.UnturnedScreenshotsRelativePath;

        paths.Add(Path.Combine(_pathSettings.Windows.ProgramFilesX86, relativePath));
        paths.Add(Path.Combine(_pathSettings.Windows.ProgramFiles, relativePath));

        foreach (var drive in _pathSettings.Steam.AdditionalDrives)
        {
            paths.Add(Path.Combine(drive, relativePath));
            paths.Add(Path.Combine(drive, "SteamLibrary", "steamapps", "common", "Unturned", "Screenshots"));
        }

        return paths;
    }
}
