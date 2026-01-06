using CustosAC.Constants;
using CustosAC.Helpers;
using CustosAC.Scanner;
using CustosAC.UI;

namespace CustosAC.Menu;

public static class ManualMenu
{
    public static void Run()
    {
        while (true)
        {
            ConsoleUI.PrintHeader();
            ConsoleUI.PrintMenu("РУЧНАЯ ПРОВЕРКА", new[]
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

            int choice = ConsoleUI.GetChoice(10);

            switch (choice)
            {
                case 0:
                    return;
                case 1:
                    NetworkMenu();
                    break;
                case 2:
                    DefenderMenu();
                    break;
                case 3:
                    UtilitiesMenu();
                    break;
                case 4:
                    FoldersMenu();
                    break;
                case 5:
                    RegistryMenu();
                    break;
                case 6:
                    SteamCheckMenu();
                    break;
                case 7:
                    UnturnedMenu();
                    break;
                case 8:
                    Common.CheckWebsites();
                    break;
                case 9:
                    Common.CheckTelegram();
                    break;
                case 10:
                    ConsoleUI.PrintHeader();
                    Common.CopyKeywordsToClipboard();
                    ConsoleUI.Pause();
                    break;
            }
        }
    }

    private static void NetworkMenu()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ СЕТЬ И ИНТЕРНЕТ ═══{ConsoleUI.ColorReset}\n");

        Common.RunCommand("ms-settings:datausage", "Использование данных");

        Console.WriteLine($"\n{ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}ЧТО НУЖНО ПРОВЕРИТЬ:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  {ConsoleUI.Arrow} Неизвестные .exe файлы с сетевой активностью");
        Console.WriteLine($"  {ConsoleUI.Arrow} Подозрительные названия процессов");
        Console.WriteLine($"  {ConsoleUI.Arrow} Большой объем переданных данных");
        ConsoleUI.Pause();
    }

    private static void DefenderMenu()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ЗАЩИТА WINDOWS ═══{ConsoleUI.ColorReset}\n");

        Common.RunCommand("windowsdefender://threat/", "Журнал защиты Windows Defender");

        Console.WriteLine($"\n{ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}КЛЮЧЕВЫЕ СЛОВА ДЛЯ ПОИСКА:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  {ConsoleUI.Arrow} undead, melony, ancient, loader, xnor");
        Console.WriteLine($"  {ConsoleUI.Arrow} hack, cheat, unturned, bypass");
        Console.WriteLine($"  {ConsoleUI.Arrow} inject, overlay, esp, aimbot");
        ConsoleUI.Pause();
    }

    private static void UtilitiesMenu()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ УТИЛИТЫ ═══{ConsoleUI.ColorReset}\n");

        Console.WriteLine($"  {ConsoleUI.Info} Открываем ссылки на утилиты для проверки...\n");

        Common.RunCommand("https://www.voidtools.com/downloads/", "Everything (поиск файлов)");
        Common.RunCommand("https://www.nirsoft.net/utils/computer_activity_view.html", "ComputerActivityView");
        Common.RunCommand("https://www.nirsoft.net/utils/usb_devices_view.html", "USBDevicesView");
        Common.RunCommand("https://privazer.com/en/download-shellbag-analyzer-shellbag-cleaner.php", "ShellBag Analyzer");

        Console.WriteLine($"\n{ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}УТИЛИТЫ:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  {ConsoleUI.Arrow} Everything - быстрый поиск файлов на ПК");
        Console.WriteLine($"  {ConsoleUI.Arrow} ComputerActivityView - активность компьютера");
        Console.WriteLine($"  {ConsoleUI.Arrow} USBDevicesView - история USB устройств");
        Console.WriteLine($"  {ConsoleUI.Arrow} ShellBag Analyzer - анализ посещенных папок");
        ConsoleUI.Pause();
    }

    private static void FoldersMenu()
    {
        while (true)
        {
            ConsoleUI.PrintHeader();
            ConsoleUI.PrintMenu("СИСТЕМНЫЕ ПАПКИ", new[]
            {
                @"AppData\Roaming",
                @"AppData\Local",
                @"AppData\LocalLow",
                "Videos (видео)",
                "Prefetch (запущенные .exe)",
                "Открыть все"
            }, true);

            int choice = ConsoleUI.GetChoice(6);
            if (choice == 0)
                break;

            ConsoleUI.PrintHeader();

            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localappdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            switch (choice)
            {
                case 1:
                    Common.OpenFolder(appdata, @"AppData\Roaming");
                    ConsoleUI.Pause();
                    break;
                case 2:
                    Common.OpenFolder(localappdata, @"AppData\Local");
                    ConsoleUI.Pause();
                    break;
                case 3:
                    Common.OpenFolder(Path.Combine(userprofile, "AppData", "LocalLow"), @"AppData\LocalLow");
                    ConsoleUI.Pause();
                    break;
                case 4:
                    Common.OpenFolder(Path.Combine(userprofile, "Videos"), "Videos");
                    ConsoleUI.Pause();
                    break;
                case 5:
                    Common.OpenFolder(AppConstants.PrefetchPath, "Prefetch");
                    ConsoleUI.Pause();
                    break;
                case 6:
                    Common.OpenFolder(appdata, "Roaming");
                    Common.OpenFolder(localappdata, "Local");
                    Common.OpenFolder(Path.Combine(userprofile, "AppData", "LocalLow"), "LocalLow");
                    Common.OpenFolder(Path.Combine(userprofile, "Videos"), "Videos");
                    Common.OpenFolder(AppConstants.PrefetchPath, "Prefetch");
                    ConsoleUI.Pause();
                    break;
            }
        }
    }

    private static void RegistryMenu()
    {
        while (true)
        {
            ConsoleUI.PrintHeader();
            ConsoleUI.PrintMenu("РЕЕСТР WINDOWS", new[]
            {
                "Открыть regedit",
                "MuiCache (запущенные программы)",
                "AppSwitched (переключения Alt+Tab)",
                "ShowJumpView (JumpList история)"
            }, true);

            int choice = ConsoleUI.GetChoice(4);
            if (choice == 0)
                break;

            ConsoleUI.PrintHeader();
            switch (choice)
            {
                case 1:
                    Common.RunCommand("regedit.exe", "Regedit открыт");
                    break;
                case 2:
                    Common.OpenRegistry(RegistryConstants.MuiCachePath);
                    break;
                case 3:
                    Common.OpenRegistry(RegistryConstants.AppSwitchedPath);
                    break;
                case 4:
                    Common.OpenRegistry(RegistryConstants.ShowJumpViewPath);
                    break;
            }
            ConsoleUI.Pause();
        }
    }

    private static void SteamCheckMenu()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ПРОВЕРКА STEAM АККАУНТОВ ═══{ConsoleUI.ColorReset}\n");

        var vdfPaths = DriveHelper.GetSteamLoginUsersPaths();
        var vdfPath = DriveHelper.FindFirstExisting(vdfPaths);

        if (vdfPath == null)
        {
            ConsoleUI.Log("Файл loginusers.vdf не найден", false);
            Console.WriteLine($"\n{ConsoleUI.Warning} {ConsoleUI.ColorYellow}Steam может быть не установлен или находится в нестандартной директории{ConsoleUI.ColorReset}");
            ConsoleUI.Pause();
            return;
        }

        ConsoleUI.Log($"Найден файл: {vdfPath}", true);
        Console.WriteLine();

        SteamScanner.ParseSteamAccountsFromPath(vdfPath);

        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.SeparatorShort}{ConsoleUI.ColorReset}");

        Console.WriteLine($"\n{ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}ЧТО НУЖНО ПРОВЕРИТЬ:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  {ConsoleUI.Arrow} Конфигурационные файлы Steam");
        Console.WriteLine($"  {ConsoleUI.Arrow} Информация об аккаунтах");
        Console.WriteLine($"  {ConsoleUI.Arrow} Логи и настройки");

        ConsoleUI.Pause();
    }

    private static void UnturnedMenu()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ UNTURNED ═══{ConsoleUI.ColorReset}\n");

        var possiblePaths = DriveHelper.GetUnturnedScreenshotsPaths();
        var screenshots = DriveHelper.FindFirstExisting(possiblePaths, isFile: false);

        if (screenshots != null)
        {
            Console.WriteLine($"  {ConsoleUI.Info} Найдено: {ConsoleUI.ColorCyan}{screenshots}{ConsoleUI.ColorReset}\n");
            if (Common.OpenFolder(screenshots, "Папка Screenshots Unturned"))
            {
                Console.WriteLine($"\n{ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}ЧТО НУЖНО ПРОВЕРИТЬ:{ConsoleUI.ColorReset}");
                Console.WriteLine($"  {ConsoleUI.Arrow} UI читов на скриншотах");
                Console.WriteLine($"  {ConsoleUI.Arrow} ESP/Wallhack индикаторы");
                Console.WriteLine($"  {ConsoleUI.Arrow} Overlay меню");
                Console.WriteLine($"  {ConsoleUI.Arrow} Необычные элементы интерфейса");
            }
        }
        else
        {
            ConsoleUI.Log(@"Папка Steam\steamapps\common\Unturned\Screenshots не найдена в системе", false);
            Console.WriteLine($"\n{ConsoleUI.Warning} {ConsoleUI.ColorYellow}Unturned может быть не установлен или находится в нестандартной директории{ConsoleUI.ColorReset}");
        }

        ConsoleUI.Pause();
    }
}
