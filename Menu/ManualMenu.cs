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
    private readonly PathSettings _pathSettings;
    private readonly RegistrySettings _registrySettings;
    private readonly ExternalResourceSettings _externalSettings;

    public ManualMenu(
        IConsoleUI consoleUI,
        IProcessService processService,
        IKeywordMatcher keywordMatcher,
        IRegistryService registryService,
        IScannerFactory scannerFactory,
        IOptions<PathSettings> pathSettings,
        IOptions<RegistrySettings> registrySettings,
        IOptions<ExternalResourceSettings> externalSettings)
    {
        _consoleUI = consoleUI;
        _processService = processService;
        _keywordMatcher = keywordMatcher;
        _registryService = registryService;
        _scannerFactory = scannerFactory;
        _pathSettings = pathSettings.Value;
        _registrySettings = registrySettings.Value;
        _externalSettings = externalSettings.Value;
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
                    await CheckWebsitesAsync();
                    break;
                case 9:
                    await CheckTelegramAsync();
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
        Console.WriteLine("\n\x1b[36m\x1b[1m═══ СЕТЬ И ИНТЕРНЕТ ═══\x1b[0m\n");

        await _processService.OpenUrlAsync("ms-settings:datausage");

        Console.WriteLine("\n\x1b[33m\x1b[1mЧТО НУЖНО ПРОВЕРИТЬ:\x1b[0m");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m Неизвестные .exe файлы с сетевой активностью");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m Подозрительные названия процессов");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m Большой объем переданных данных");
        _consoleUI.Pause();
    }

    private async Task DefenderMenuAsync()
    {
        _consoleUI.PrintHeader();
        Console.WriteLine("\n\x1b[36m\x1b[1m═══ ЗАЩИТА WINDOWS ═══\x1b[0m\n");

        await _processService.OpenUrlAsync("windowsdefender://threat/");

        Console.WriteLine("\n\x1b[33m\x1b[1mКЛЮЧЕВЫЕ СЛОВА ДЛЯ ПОИСКА:\x1b[0m");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m undead, melony, ancient, loader, xnor");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m hack, cheat, unturned, bypass");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m inject, overlay, esp, aimbot");
        _consoleUI.Pause();
    }

    private async Task UtilitiesMenuAsync()
    {
        _consoleUI.PrintHeader();
        Console.WriteLine("\n\x1b[36m\x1b[1m═══ УТИЛИТЫ ═══\x1b[0m\n");
        Console.WriteLine("  \x1b[34m[i]\x1b[0m Открываем ссылки на утилиты для проверки...\n");

        await _processService.OpenUrlAsync("https://www.voidtools.com/downloads/");
        _consoleUI.Log("Everything (поиск файлов)", true);

        await _processService.OpenUrlAsync("https://www.nirsoft.net/utils/computer_activity_view.html");
        _consoleUI.Log("ComputerActivityView", true);

        await _processService.OpenUrlAsync("https://www.nirsoft.net/utils/usb_devices_view.html");
        _consoleUI.Log("USBDevicesView", true);

        await _processService.OpenUrlAsync("https://privazer.com/en/download-shellbag-analyzer-shellbag-cleaner.php");
        _consoleUI.Log("ShellBag Analyzer", true);

        Console.WriteLine("\n\x1b[33m\x1b[1mУТИЛИТЫ:\x1b[0m");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m Everything - быстрый поиск файлов на ПК");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m ComputerActivityView - активность компьютера");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m USBDevicesView - история USB устройств");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m ShellBag Analyzer - анализ посещенных папок");
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
        Console.WriteLine("\n\x1b[36m\x1b[1m═══ ПРОВЕРКА STEAM АККАУНТОВ ═══\x1b[0m\n");

        var scanner = _scannerFactory.CreateSteamScanner();
        var result = await scanner.ScanAsync();

        if (result.Success && result.HasFindings)
        {
            foreach (var finding in result.Findings)
            {
                Console.WriteLine($"  \x1b[36m[>]\x1b[0m {finding}");
            }
            _consoleUI.Log($"Найдено аккаунтов Steam: {result.Count}", true);
        }
        else
        {
            _consoleUI.Log("Steam аккаунты не найдены", false);
        }

        Console.WriteLine("\n\x1b[36m─────────────────────────────────────────\x1b[0m");
        Console.WriteLine("\n\x1b[33m\x1b[1mЧТО НУЖНО ПРОВЕРИТЬ:\x1b[0m");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m Конфигурационные файлы Steam");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m Информация об аккаунтах");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m Логи и настройки");
        _consoleUI.Pause();
    }

    private async Task UnturnedMenuAsync()
    {
        _consoleUI.PrintHeader();
        Console.WriteLine("\n\x1b[36m\x1b[1m═══ UNTURNED ═══\x1b[0m\n");

        var screenshotPaths = GetUnturnedScreenshotsPaths();
        var screenshots = screenshotPaths.FirstOrDefault(Directory.Exists);

        if (screenshots != null)
        {
            Console.WriteLine($"  \x1b[34m[i]\x1b[0m Найдено: \x1b[36m{screenshots}\x1b[0m\n");
            await OpenFolderAsync(screenshots, "Папка Screenshots Unturned");

            Console.WriteLine("\n\x1b[33m\x1b[1mЧТО НУЖНО ПРОВЕРИТЬ:\x1b[0m");
            Console.WriteLine("  \x1b[36m[>]\x1b[0m UI читов на скриншотах");
            Console.WriteLine("  \x1b[36m[>]\x1b[0m ESP/Wallhack индикаторы");
            Console.WriteLine("  \x1b[36m[>]\x1b[0m Overlay меню");
            Console.WriteLine("  \x1b[36m[>]\x1b[0m Необычные элементы интерфейса");
        }
        else
        {
            _consoleUI.Log(@"Папка Unturned\Screenshots не найдена", false);
            Console.WriteLine("\n\x1b[33m[!]\x1b[0m \x1b[33mUnturned может быть не установлен\x1b[0m");
        }

        _consoleUI.Pause();
    }

    private async Task CheckWebsitesAsync()
    {
        _consoleUI.PrintHeader();
        Console.WriteLine("\n\x1b[36m\x1b[1m═══ ПРОВЕРКА САЙТОВ ═══\x1b[0m\n");
        Console.WriteLine("  \x1b[34m[i]\x1b[0m Открываем сайты для проверки доступности...\n");

        foreach (var website in _externalSettings.WebsitesToCheck)
        {
            await _processService.OpenUrlAsync(website.Url);
            _consoleUI.Log($"Открыт: {website.Name}", true);
        }

        Console.WriteLine("\n\x1b[33m\x1b[1mЧТО ПРОВЕРИТЬ:\x1b[0m");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m Доступность сайтов");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m Нет ли редиректов на подозрительные домены");
        _consoleUI.Pause();
    }

    private async Task CheckTelegramAsync()
    {
        _consoleUI.PrintHeader();
        Console.WriteLine("\n\x1b[36m\x1b[1m═══ ПРОВЕРКА TELEGRAM ═══\x1b[0m\n");
        Console.WriteLine("  \x1b[34m[i]\x1b[0m Открываем Telegram ботов...\n");

        foreach (var bot in _externalSettings.TelegramBots)
        {
            var url = $"tg://resolve?domain={bot.Username.TrimStart('@')}";
            await _processService.OpenUrlAsync(url);
            _consoleUI.Log($"Открыт: {bot.Name} ({bot.Username})", true);
        }

        Console.WriteLine("\n\x1b[33m\x1b[1mЧТО ПРОВЕРИТЬ:\x1b[0m");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m Историю переписки с ботами");
        Console.WriteLine("  \x1b[36m[>]\x1b[0m Загруженные файлы");
        _consoleUI.Pause();
    }

    private void CopyKeywordsToClipboard()
    {
        _consoleUI.PrintHeader();
        var keywords = _keywordMatcher.GetKeywordsString();
        _processService.CopyToClipboardAsync(keywords).Wait();
        _consoleUI.Log("Ключевые слова скопированы в буфер обмена!", true);
        Console.WriteLine("\n\x1b[33mТеперь можно вставить (Ctrl+V) в Everything и другие программы\x1b[0m");
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
        Console.WriteLine("\x1b[33mВставьте путь в regedit (Ctrl+V)\x1b[0m");
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
