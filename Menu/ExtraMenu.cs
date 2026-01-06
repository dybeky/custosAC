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
            if (choice == 0) break;

            ConsoleUI.PrintHeader();
            switch (choice)
            {
                case 1: EnableRegistry(); break;
                case 2: EnableSystemSettings(); break;
            }
        }
    }

    private static void EnableRegistry()
    {
        bool success = RunRegCommand(@"delete ""HKLM\Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\regedit.exe"" /f");

        if (success)
        {
            ConsoleUI.Log("Реестр успешно включен", true);
            Console.WriteLine($"\n{ConsoleUI.ColorGreen}+ Теперь вы можете открыть regedit{ConsoleUI.ColorReset}");
        }
        else
        {
            ConsoleUI.Log("Ошибка при включении реестра", false);
            Console.WriteLine($"\n{ConsoleUI.Warning} {ConsoleUI.ColorYellow}Возможно реестр уже включен или требуются права администратора{ConsoleUI.ColorReset}");
        }
        ConsoleUI.Pause();
    }

    private static void EnableSystemSettings()
    {
        ConsoleUI.Log("Разблокируем доступ к параметрам системы и сети...", true);
        Console.WriteLine();

        int unlockedCount = 0;

        // 1. Удаление отдельных значений реестра
        var valuesToDelete = new (string key, string value)[]
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
            // Network Connections
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
            // Internet Explorer
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
            // Windows Settings
            (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\System", "NoDispCPL"),
            (@"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\System", "NoDispCPL"),
            (@"HKCU\Software\Policies\Microsoft\Windows\System", "SettingsPageVisibility"),
            (@"HKLM\Software\Policies\Microsoft\Windows\System", "SettingsPageVisibility"),
            // Wi-Fi
            (@"HKCU\Software\Policies\Microsoft\Windows\System", "DenyDeviceIDs"),
            (@"HKLM\Software\Policies\Microsoft\Windows\System", "DenyDeviceIDs"),
            (@"HKLM\Software\Policies\Microsoft\Windows\WcmSvc\GroupPolicy", "fBlockNonDomain"),
            (@"HKLM\Software\Policies\Microsoft\Windows\WcmSvc\GroupPolicy", "fMinimizeConnections"),
        };

        ConsoleUI.Log("Удаляем блокировки реестра...", true);
        foreach (var (key, value) in valuesToDelete)
        {
            if (DeleteRegistry(key, value)) unlockedCount++;
        }

        // 2. Удаление целых веток реестра
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
            if (DeleteRegistry(key)) unlockedCount++;
        }

        // 3. Перезапуск сетевых служб
        Console.WriteLine();
        ConsoleUI.Log("Перезапускаем сетевые службы...", true);
        foreach (var svc in new[] { "netprofm", "NlaSvc", "Dhcp", "Dnscache" })
            RestartService(svc);

        // 4. Сброс сетевых настроек
        Console.WriteLine();
        ConsoleUI.Log("Сбрасываем сетевые настройки...", true);
        RunSilent("ipconfig", "/release");
        RunSilent("ipconfig", "/renew");
        RunSilent("ipconfig", "/flushdns");
        RunSilent("netsh", "winsock reset");
        RunSilent("netsh", "int ip reset");

        // 5. Включение сетевых адаптеров
        Console.WriteLine();
        ConsoleUI.Log("Проверяем сетевые адаптеры...", true);
        RunSilent("powershell", "-Command \"Get-NetAdapter -Physical | Where-Object {$_.Status -eq 'Disabled'} | Enable-NetAdapter -Confirm:$false\"", 15000);
        ConsoleUI.Log("  + Сетевые адаптеры проверены", true);

        // 6. Открываем параметры сети
        Console.WriteLine();
        ConsoleUI.Log("Открываем параметры сети...", true);
        RunSilent("cmd", "/c start ms-settings:network");

        // Итог
        Console.WriteLine();
        if (unlockedCount > 0)
        {
            Console.WriteLine($"{ConsoleUI.ColorGreen}{ConsoleUI.ColorBold}╔══════════════════════════════════════════════════╗{ConsoleUI.ColorReset}");
            Console.WriteLine($"{ConsoleUI.ColorGreen}║  + СЕТЬ И ПАРАМЕТРЫ СИСТЕМЫ РАЗБЛОКИРОВАНЫ       ║{ConsoleUI.ColorReset}");
            Console.WriteLine($"{ConsoleUI.ColorGreen}║    Удалено блокировок: {unlockedCount,-3}                        ║{ConsoleUI.ColorReset}");
            Console.WriteLine($"{ConsoleUI.ColorGreen}{ConsoleUI.ColorBold}╚══════════════════════════════════════════════════╝{ConsoleUI.ColorReset}");
            Console.WriteLine();
            Console.WriteLine($"{ConsoleUI.ColorYellow}{ConsoleUI.Warning} Рекомендуется перезагрузить компьютер{ConsoleUI.ColorReset}");
        }
        else
        {
            Console.WriteLine($"{ConsoleUI.ColorGreen}+ Блокировки не найдены, сеть уже разблокирована{ConsoleUI.ColorReset}");
        }
        ConsoleUI.Pause();
    }

    private static bool RunRegCommand(string args)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "reg",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            });
            p?.WaitForExit();
            return p?.ExitCode == 0;
        }
        catch { return false; }
    }

    private static bool DeleteRegistry(string key, string? value = null)
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
            if (value != null)
            {
                psi.ArgumentList.Add("/v");
                psi.ArgumentList.Add(value);
            }
            psi.ArgumentList.Add("/f");

            using var p = Process.Start(psi);
            p?.WaitForExit();

            if (p?.ExitCode == 0)
            {
                string loc = key.StartsWith("HKCU") ? "HKCU" : "HKLM";
                ConsoleUI.Log(value != null ? $"  + Удалено: {value} ({loc})" : $"  + Удалена ветка: {key}", true);
                return true;
            }
        }
        catch { }
        return false;
    }

    private static void RestartService(string name)
    {
        try
        {
            RunSilent("net", $"stop {name}", 5000);
            if (RunSilent("net", $"start {name}", 5000))
                ConsoleUI.Log($"  + Перезапущена служба: {name}", true);
        }
        catch { }
    }

    private static bool RunSilent(string cmd, string args, int timeout = 10000)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            p?.WaitForExit(timeout);
            return p?.ExitCode == 0;
        }
        catch { return false; }
    }
}
