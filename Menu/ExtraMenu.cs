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
        ConsoleUI.Log("Разблокируем доступ к параметрам системы и сети...", true);
        Console.WriteLine();

        int unlockedCount = 0;

        // === 1. Удаление отдельных значений реестра ===
        var valuesToDelete = new[]
        {
            // Панель управления и параметры
            (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoControlPanel"),
            (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoControlPanel"),
            (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "SettingsPageVisibility"),
            (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "SettingsPageVisibility"),
            (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "DisallowCpl"),
            (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "DisallowCpl"),

            // Устаревшие ключи сети
            (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Network", "NoNetSetup"),
            (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Network", "NoNetSetup"),
            (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Network", "NoNetSetupSecurityPage"),
            (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Network", "NoNetSetupSecurityPage"),
            (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Network", "NoNetSetupIDPage"),
            (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Network", "NoNetSetupIDPage"),

            // Современные ключи сетевых подключений (Network Connections)
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_AllowAdvancedTCPIPConfig"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_AllowAdvancedTCPIPConfig"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_LanConnect"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_LanConnect"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_LanProperties"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_LanProperties"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_LanChangeProperties"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_LanChangeProperties"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_NewConnectionWizard"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_NewConnectionWizard"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_DialupPrefs"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_DialupPrefs"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_ChangeBindState"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_ChangeBindState"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_AddRemoveComponents"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_AddRemoveComponents"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_Statistics"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_Statistics"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_EnableAdminProhibits"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_EnableAdminProhibits"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_ShowSharedAccessUI"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_ShowSharedAccessUI"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_PersonalFirewallConfig"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_PersonalFirewallConfig"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_ICSEnable"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_ICSEnable"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_RenameConnection"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_RenameConnection"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_DeleteConnection"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_DeleteConnection"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_RasAllUserProperties"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_RasAllUserProperties"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_RasMyProperties"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_RasMyProperties"),
            (@"HKCU\Software\Policies\Microsoft\Windows\Network Connections", "NC_RasConnect"),
            (@"HKLM\Software\Policies\Microsoft\Windows\Network Connections", "NC_RasConnect"),

            // Internet Explorer / Свойства интернета
            (@"HKCU\Software\Policies\Microsoft\Internet Explorer\Control Panel", "ConnectionsTab"),
            (@"HKLM\Software\Policies\Microsoft\Internet Explorer\Control Panel", "ConnectionsTab"),
            (@"HKCU\Software\Policies\Microsoft\Internet Explorer\Control Panel", "Connwiz Admin Lock"),
            (@"HKLM\Software\Policies\Microsoft\Internet Explorer\Control Panel", "Connwiz Admin Lock"),
            (@"HKCU\Software\Policies\Microsoft\Internet Explorer\Control Panel", "Connection Settings"),
            (@"HKLM\Software\Policies\Microsoft\Internet Explorer\Control Panel", "Connection Settings"),
            (@"HKCU\Software\Policies\Microsoft\Internet Explorer\Control Panel", "Proxy"),
            (@"HKLM\Software\Policies\Microsoft\Internet Explorer\Control Panel", "Proxy"),
            (@"HKCU\Software\Policies\Microsoft\Internet Explorer\Control Panel", "AutoConfig"),
            (@"HKLM\Software\Policies\Microsoft\Internet Explorer\Control Panel", "AutoConfig"),
            (@"HKCU\Software\Policies\Microsoft\Internet Explorer\Control Panel", "LAN Settings"),
            (@"HKLM\Software\Policies\Microsoft\Internet Explorer\Control Panel", "LAN Settings"),

            // Дополнительные блокировки Windows Settings
            (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\System", "NoDispCPL"),
            (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\System", "NoDispCPL"),
            (@"HKCU\Software\Policies\Microsoft\Windows\System", "SettingsPageVisibility"),
            (@"HKLM\Software\Policies\Microsoft\Windows\System", "SettingsPageVisibility"),

            // Wi-Fi блокировки
            (@"HKCU\Software\Policies\Microsoft\Windows\System", "DenyDeviceIDs"),
            (@"HKLM\Software\Policies\Microsoft\Windows\System", "DenyDeviceIDs"),
            (@"HKLM\Software\Policies\Microsoft\Windows\WcmSvc\GroupPolicy", "fBlockNonDomain"),
            (@"HKLM\Software\Policies\Microsoft\Windows\WcmSvc\GroupPolicy", "fMinimizeConnections"),
        };

        ConsoleUI.Log("Удаляем блокировки реестра...", true);
        foreach (var (key, value) in valuesToDelete)
        {
            if (DeleteRegistryValue(key, value))
                unlockedCount++;
        }

        // === 2. Удаление целых веток реестра (политики) ===
        Console.WriteLine();
        ConsoleUI.Log("Удаляем политики полностью...", true);

        var keysToDelete = new[]
        {
            @"HKCU\Software\Policies\Microsoft\Windows\Network Connections",
            @"HKLM\Software\Policies\Microsoft\Windows\Network Connections",
            @"HKCU\Software\Policies\Microsoft\Internet Explorer\Control Panel",
            @"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Network",
            @"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Network",
        };

        foreach (var key in keysToDelete)
        {
            if (DeleteRegistryKey(key))
                unlockedCount++;
        }

        // === 3. Перезапуск сетевых служб ===
        Console.WriteLine();
        ConsoleUI.Log("Перезапускаем сетевые службы...", true);

        RestartService("netprofm");  // Network List Service
        RestartService("NlaSvc");    // Network Location Awareness
        RestartService("Dhcp");      // DHCP Client
        RestartService("Dnscache");  // DNS Client

        // === 4. Сброс сетевых настроек ===
        Console.WriteLine();
        ConsoleUI.Log("Сбрасываем сетевые настройки...", true);

        RunCommand("ipconfig", "/release");
        RunCommand("ipconfig", "/renew");
        RunCommand("ipconfig", "/flushdns");
        RunCommand("netsh", "winsock reset");
        RunCommand("netsh", "int ip reset");

        // === 5. Включение сетевых адаптеров ===
        Console.WriteLine();
        ConsoleUI.Log("Проверяем сетевые адаптеры...", true);
        EnableNetworkAdapters();

        // === 6. Открываем параметры сети ===
        Console.WriteLine();
        ConsoleUI.Log("Открываем параметры сети...", true);

        bool settingsOpened = false;
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
            settingsOpened = process?.ExitCode == 0;
        }
        catch { }

        // === Итоговый результат ===
        Console.WriteLine();
        if (unlockedCount > 0 || settingsOpened)
        {
            Console.WriteLine($"{ConsoleUI.ColorGreen}{ConsoleUI.ColorBold}╔══════════════════════════════════════════════════╗{ConsoleUI.ColorReset}");
            Console.WriteLine($"{ConsoleUI.ColorGreen}║  + СЕТЬ И ПАРАМЕТРЫ СИСТЕМЫ РАЗБЛОКИРОВАНЫ       ║{ConsoleUI.ColorReset}");
            Console.WriteLine($"{ConsoleUI.ColorGreen}║    Удалено блокировок: {unlockedCount,-3}                        ║{ConsoleUI.ColorReset}");
            Console.WriteLine($"{ConsoleUI.ColorGreen}{ConsoleUI.ColorBold}╚══════════════════════════════════════════════════╝{ConsoleUI.ColorReset}");
            Console.WriteLine();
            Console.WriteLine($"{ConsoleUI.ColorYellow}{ConsoleUI.Warning} Рекомендуется перезагрузить компьютер для применения всех изменений{ConsoleUI.ColorReset}");
        }
        else
        {
            Console.WriteLine($"{ConsoleUI.ColorGreen}+ Блокировки не найдены, сеть уже разблокирована{ConsoleUI.ColorReset}");
        }

        ConsoleUI.Pause();
    }

    private static bool DeleteRegistryValue(string key, string value)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "reg",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
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
                string location = key.StartsWith("HKCU") ? "HKCU" : "HKLM";
                ConsoleUI.Log($"  + Удалено: {value} ({location})", true);
                return true;
            }
        }
        catch { }
        return false;
    }

    private static bool DeleteRegistryKey(string key)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "reg",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            psi.ArgumentList.Add("delete");
            psi.ArgumentList.Add(key);
            psi.ArgumentList.Add("/f");

            using var process = Process.Start(psi);
            process?.WaitForExit();

            if (process?.ExitCode == 0)
            {
                ConsoleUI.Log($"  + Удалена ветка: {key}", true);
                return true;
            }
        }
        catch { }
        return false;
    }

    private static void RestartService(string serviceName)
    {
        try
        {
            // Остановка
            var stopPsi = new ProcessStartInfo
            {
                FileName = "net",
                Arguments = $"stop {serviceName}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var stopProcess = Process.Start(stopPsi);
            stopProcess?.WaitForExit(5000);

            // Запуск
            var startPsi = new ProcessStartInfo
            {
                FileName = "net",
                Arguments = $"start {serviceName}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var startProcess = Process.Start(startPsi);
            startProcess?.WaitForExit(5000);

            if (startProcess?.ExitCode == 0)
            {
                ConsoleUI.Log($"  + Перезапущена служба: {serviceName}", true);
            }
        }
        catch { }
    }

    private static void RunCommand(string command, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            process?.WaitForExit(10000);
        }
        catch { }
    }

    private static void EnableNetworkAdapters()
    {
        try
        {
            // Получаем список отключенных адаптеров и включаем их
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-Command \"Get-NetAdapter -Physical | Where-Object {$_.Status -eq 'Disabled'} | Enable-NetAdapter -Confirm:$false\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            process?.WaitForExit(15000);

            if (process?.ExitCode == 0)
            {
                ConsoleUI.Log("  + Сетевые адаптеры проверены и включены", true);
            }
        }
        catch { }
    }
}
