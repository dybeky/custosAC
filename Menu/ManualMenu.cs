using System.Diagnostics;
using CustosAC.Scanner;
using CustosAC.UI;
using CustosAC.WinAPI;

namespace CustosAC.Menu;

public static class ManualMenu
{
    public static void Run()
    {
        while (true)
        {
            ConsoleUI.PrintHeader();
            ConsoleUI.PrintMenu("🔍 РУЧНАЯ ПРОВЕРКА", new[]
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
                "📋 Скопировать ключевые слова"
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
        Console.WriteLine($"  {ConsoleUI.ColorRed}►{ConsoleUI.ColorReset} Неизвестные .exe файлы с сетевой активностью");
        Console.WriteLine($"  {ConsoleUI.ColorRed}►{ConsoleUI.ColorReset} Подозрительные названия процессов");
        Console.WriteLine($"  {ConsoleUI.ColorRed}►{ConsoleUI.ColorReset} Большой объем переданных данных");
        ConsoleUI.Pause();
    }

    private static void DefenderMenu()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ЗАЩИТА WINDOWS ═══{ConsoleUI.ColorReset}\n");

        Common.RunCommand("windowsdefender://threat/", "Журнал защиты Windows Defender");

        Console.WriteLine($"\n{ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}КЛЮЧЕВЫЕ СЛОВА ДЛЯ ПОИСКА:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  {ConsoleUI.ColorRed}►{ConsoleUI.ColorReset} undead, melony, ancient, loader");
        Console.WriteLine($"  {ConsoleUI.ColorRed}►{ConsoleUI.ColorReset} hack, cheat, unturned, bypass");
        Console.WriteLine($"  {ConsoleUI.ColorRed}►{ConsoleUI.ColorReset} inject, overlay, esp, aimbot");
        ConsoleUI.Pause();
    }

    private static void UtilitiesMenu()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ УТИЛИТЫ ═══{ConsoleUI.ColorReset}\n");

        Console.WriteLine($"  {ConsoleUI.ColorBlue}[i]{ConsoleUI.ColorReset} Открываем ссылки на утилиты для проверки...\n");

        Common.RunCommand("https://www.voidtools.com/downloads/", "Everything (поиск файлов)");
        Common.RunCommand("https://www.nirsoft.net/utils/computer_activity_view.html", "ComputerActivityView");
        Common.RunCommand("https://www.nirsoft.net/utils/usb_devices_view.html", "USBDevicesView");
        Common.RunCommand("https://privazer.com/en/download-shellbag-analyzer-shellbag-cleaner.php", "ShellBag Analyzer");

        Console.WriteLine($"\n{ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}УТИЛИТЫ:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  {ConsoleUI.ColorCyan}►{ConsoleUI.ColorReset} Everything - быстрый поиск файлов на ПК");
        Console.WriteLine($"  {ConsoleUI.ColorCyan}►{ConsoleUI.ColorReset} ComputerActivityView - активность компьютера");
        Console.WriteLine($"  {ConsoleUI.ColorCyan}►{ConsoleUI.ColorReset} USBDevicesView - история USB устройств");
        Console.WriteLine($"  {ConsoleUI.ColorCyan}►{ConsoleUI.ColorReset} ShellBag Analyzer - анализ посещенных папок");
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
                    Common.OpenFolder(@"C:\Windows\Prefetch", "Prefetch");
                    ConsoleUI.Pause();
                    break;
                case 6:
                    Common.OpenFolder(appdata, "Roaming");
                    Common.OpenFolder(localappdata, "Local");
                    Common.OpenFolder(Path.Combine(userprofile, "AppData", "LocalLow"), "LocalLow");
                    Common.OpenFolder(Path.Combine(userprofile, "Videos"), "Videos");
                    Common.OpenFolder(@"C:\Windows\Prefetch", "Prefetch");
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
                    try
                    {
                        var process = Process.Start(new ProcessStartInfo
                        {
                            FileName = "regedit.exe",
                            UseShellExecute = true
                        });

                        if (process != null)
                        {
                            AdminHelper.TrackProcess(process);
                            Task.Run(() =>
                            {
                                process.WaitForExit();
                                AdminHelper.UntrackProcess(process);
                            });
                            ConsoleUI.Log("Regedit открыт", true);
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleUI.Log($"Ошибка: {ex.Message}", false);
                    }
                    ConsoleUI.Pause();
                    break;
                case 2:
                    Common.OpenRegistry(@"HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache");
                    ConsoleUI.Pause();
                    break;
                case 3:
                    Common.OpenRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched");
                    ConsoleUI.Pause();
                    break;
                case 4:
                    Common.OpenRegistry(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\ShowJumpView");
                    ConsoleUI.Pause();
                    break;
            }
        }
    }

    private static void SteamCheckMenu()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ПРОВЕРКА STEAM АККАУНТОВ ═══{ConsoleUI.ColorReset}\n");

        var vdfPaths = new List<string>
        {
            @"C:\Program Files (x86)\Steam\config\loginusers.vdf",
            @"C:\Program Files\Steam\config\loginusers.vdf"
        };

        var drives = new[] { "D:", "E:", "F:" };
        foreach (var drive in drives)
        {
            vdfPaths.Add(Path.Combine(drive, "Steam", "config", "loginusers.vdf"));
            vdfPaths.Add(Path.Combine(drive, "Program Files (x86)", "Steam", "config", "loginusers.vdf"));
            vdfPaths.Add(Path.Combine(drive, "Program Files", "Steam", "config", "loginusers.vdf"));
        }

        string? vdfPath = null;
        foreach (var path in vdfPaths)
        {
            if (File.Exists(path))
            {
                vdfPath = path;
                break;
            }
        }

        if (vdfPath == null)
        {
            ConsoleUI.Log("Файл loginusers.vdf не найден", false);
            Console.WriteLine($"\n{ConsoleUI.ColorYellow}⚠ Steam может быть не установлен или находится в нестандартной директории{ConsoleUI.ColorReset}");
            ConsoleUI.Pause();
            return;
        }

        ConsoleUI.Log($"Найден файл: {vdfPath}", true);
        Console.WriteLine();

        SteamScanner.ParseSteamAccountsFromPath(vdfPath);

        Console.WriteLine($"\n{ConsoleUI.ColorCyan}─────────────────────────────────────────{ConsoleUI.ColorReset}");

        Console.WriteLine($"\n{ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}ЧТО НУЖНО ПРОВЕРИТЬ:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  {ConsoleUI.ColorRed}►{ConsoleUI.ColorReset} Конфигурационные файлы Steam");
        Console.WriteLine($"  {ConsoleUI.ColorRed}►{ConsoleUI.ColorReset} Информация об аккаунтах");
        Console.WriteLine($"  {ConsoleUI.ColorRed}►{ConsoleUI.ColorReset} Логи и настройки");

        ConsoleUI.Pause();
    }

    private static void UnturnedMenu()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ UNTURNED ═══{ConsoleUI.ColorReset}\n");

        var possiblePaths = new List<string>
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\Unturned\Screenshots",
            @"C:\Program Files\Steam\steamapps\common\Unturned\Screenshots"
        };

        var drives = new[] { "D:", "E:", "F:" };
        foreach (var drive in drives)
        {
            possiblePaths.Add(Path.Combine(drive, "Steam", "steamapps", "common", "Unturned", "Screenshots"));
            possiblePaths.Add(Path.Combine(drive, "Program Files (x86)", "Steam", "steamapps", "common", "Unturned", "Screenshots"));
            possiblePaths.Add(Path.Combine(drive, "Program Files", "Steam", "steamapps", "common", "Unturned", "Screenshots"));
        }

        bool found = false;
        foreach (var screenshots in possiblePaths)
        {
            if (Directory.Exists(screenshots))
            {
                found = true;
                Console.WriteLine($"  {ConsoleUI.ColorBlue}[i]{ConsoleUI.ColorReset} Найдено: {ConsoleUI.ColorCyan}{screenshots}{ConsoleUI.ColorReset}\n");
                if (Common.OpenFolder(screenshots, "Папка Screenshots Unturned"))
                {
                    Console.WriteLine($"\n{ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}ЧТО НУЖНО ПРОВЕРИТЬ:{ConsoleUI.ColorReset}");
                    Console.WriteLine($"  {ConsoleUI.ColorRed}►{ConsoleUI.ColorReset} UI читов на скриншотах");
                    Console.WriteLine($"  {ConsoleUI.ColorRed}►{ConsoleUI.ColorReset} ESP/Wallhack индикаторы");
                    Console.WriteLine($"  {ConsoleUI.ColorRed}►{ConsoleUI.ColorReset} Overlay меню");
                    Console.WriteLine($"  {ConsoleUI.ColorRed}►{ConsoleUI.ColorReset} Необычные элементы интерфейса");
                }
                break;
            }
        }

        if (!found)
        {
            ConsoleUI.Log(@"Папка Steam\steamapps\common\Unturned\Screenshots не найдена в системе", false);
            Console.WriteLine($"\n{ConsoleUI.ColorYellow}⚠ Unturned может быть не установлен или находится в нестандартной директории{ConsoleUI.ColorReset}");
        }

        ConsoleUI.Pause();
    }
}
