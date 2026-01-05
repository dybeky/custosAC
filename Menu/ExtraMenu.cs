using System.Diagnostics;
using CustosAC.UI;

namespace CustosAC.Menu;

public static class ExtraMenu
{
    public static void Run()
    {
        while (true)
        {
            ConsoleUI.PrintHeader();
            ConsoleUI.PrintMenu("ЭКСТРА", new[]
            {
                "Включить реестр",
                "Включить параметры системы и сеть"
            }, true);

            int choice = ConsoleUI.GetChoice(2);
            if (choice == 0)
                break;

            ConsoleUI.PrintHeader();
            switch (choice)
            {
                case 1:
                    EnableRegistry();
                    break;
                case 2:
                    EnableSystemSettings();
                    break;
            }
        }
    }

    private static void EnableRegistry()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "reg",
                Arguments = @"delete ""HKLM\Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\regedit.exe"" /f",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();

            if (process?.ExitCode == 0)
            {
                ConsoleUI.Log("Реестр успешно включен", true);
                Console.WriteLine($"\n{ConsoleUI.ColorGreen}+ Теперь вы можете открыть regedit{ConsoleUI.ColorReset}");
            }
            else
            {
                ConsoleUI.Log("Ошибка при включении реестра", false);
                Console.WriteLine($"\n{ConsoleUI.Warning} {ConsoleUI.ColorYellow}Возможно реестр уже включен или требуются права администратора{ConsoleUI.ColorReset}");
            }
        }
        catch (Exception ex)
        {
            ConsoleUI.Log($"Ошибка при включении реестра: {ex.Message}", false);
            Console.WriteLine($"\n{ConsoleUI.Warning} {ConsoleUI.ColorYellow}Возможно реестр уже включен или требуются права администратора{ConsoleUI.ColorReset}");
        }

        ConsoleUI.Pause();
    }

    private static void EnableSystemSettings()
    {
        ConsoleUI.Log("Разблокируем доступ к параметрам системы...", true);
        Console.WriteLine();

        bool success = true;

        // Команды для удаления блокировок
        var commands = new[]
        {
            (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoControlPanel", "HKCU"),
            (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoControlPanel", "HKLM"),
            (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Network", "NoNetSetup", "HKCU"),
            (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Network", "NoNetSetup", "HKLM"),
            (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "SettingsPageVisibility", "HKCU"),
            (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "SettingsPageVisibility", "HKLM")
        };

        foreach (var (key, value, location) in commands)
        {
            try
            {
                // Используем ArgumentList для безопасной передачи аргументов
                var psi = new ProcessStartInfo
                {
                    FileName = "reg",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };
                psi.ArgumentList.Add("delete");
                psi.ArgumentList.Add(key);
                psi.ArgumentList.Add("/v");
                psi.ArgumentList.Add(value);
                psi.ArgumentList.Add("/f");

                using var process = Process.Start(psi);
                process?.WaitForExit();

                if (process?.ExitCode == 0)
                {
                    ConsoleUI.Log($"+ Удалена блокировка {value} ({location})", true);
                }
            }
            catch (Exception ex)
            {
                // Ключ может не существовать - логируем для отладки
                ConsoleUI.Log($"Не удалось удалить {value}: {ex.Message}", false);
            }
        }

        // Проверяем доступность Settings
        Console.WriteLine();
        ConsoleUI.Log("Проверяем доступность параметров системы...", true);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = "/c start ms-settings:network",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();

            if (process?.ExitCode == 0)
            {
                ConsoleUI.Log("+ Параметры сети открыты успешно", true);
            }
            else
            {
                ConsoleUI.Log("Не удалось открыть параметры сети", false);
                success = false;
            }
        }
        catch (Exception ex)
        {
            ConsoleUI.Log($"Не удалось открыть параметры сети: {ex.Message}", false);
            success = false;
        }

        Console.WriteLine();
        if (success)
        {
            Console.WriteLine($"{ConsoleUI.ColorGreen}{ConsoleUI.ColorBold}╔════════════════════════════════════════════╗{ConsoleUI.ColorReset}");
            Console.WriteLine($"{ConsoleUI.ColorGreen}║  + ПАРАМЕТРЫ СИСТЕМЫ РАЗБЛОКИРОВАНЫ       ║{ConsoleUI.ColorReset}");
            Console.WriteLine($"{ConsoleUI.ColorGreen}{ConsoleUI.ColorBold}╚════════════════════════════════════════════╝{ConsoleUI.ColorReset}");
        }
        else
        {
            Console.WriteLine($"\n{ConsoleUI.Warning} {ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}Если параметры не открылись:{ConsoleUI.ColorReset}");
            Console.WriteLine($"  {ConsoleUI.Arrow} Запустите программу от имени администратора");
            Console.WriteLine($"  {ConsoleUI.Arrow} Проверьте групповые политики (gpedit.msc)");
        }

        ConsoleUI.Pause();
    }
}
