using System.Diagnostics;
using CustosAC.Constants;
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

    // ═══════════════════════════════════════════════════════════════
    // ВКЛЮЧЕНИЕ РЕЕСТРА
    // ═══════════════════════════════════════════════════════════════

    private static void EnableRegistry()
    {
        bool success = RunRegDelete(RegistryConstants.RegeditBlockPath);

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

    // ═══════════════════════════════════════════════════════════════
    // ВКЛЮЧЕНИЕ ПАРАМЕТРОВ СИСТЕМЫ И СЕТИ
    // ═══════════════════════════════════════════════════════════════

    private static void EnableSystemSettings()
    {
        ConsoleUI.Log("Разблокируем доступ к параметрам системы и сети...", true);
        Console.WriteLine();

        int unlockedCount = 0;

        // 1. Удаление отдельных значений реестра
        unlockedCount += DeleteRegistryValues();

        // 2. Удаление целых веток реестра
        unlockedCount += DeleteRegistryKeys();

        // 3. Перезапуск сетевых служб
        RestartNetworkServices();

        // 4. Сброс сетевых настроек
        ResetNetworkSettings();

        // 5. Включение сетевых адаптеров
        EnableNetworkAdapters();

        // 6. Открываем параметры сети
        OpenNetworkSettings();

        // Итог
        PrintUnlockResult(unlockedCount);
        ConsoleUI.Pause();
    }

    /// <summary>Удаляет отдельные значения реестра для разблокировки</summary>
    private static int DeleteRegistryValues()
    {
        ConsoleUI.Log("Удаляем блокировки реестра...", true);

        int count = 0;
        foreach (var (key, value) in RegistryConstants.ValuesToDelete)
        {
            if (DeleteRegistryValue(key, value))
                count++;
        }
        return count;
    }

    /// <summary>Удаляет целые ветки реестра</summary>
    private static int DeleteRegistryKeys()
    {
        Console.WriteLine();
        ConsoleUI.Log("Удаляем политики полностью...", true);

        int count = 0;
        foreach (var key in RegistryConstants.KeysToDelete)
        {
            if (DeleteRegistryKey(key))
                count++;
        }
        return count;
    }

    /// <summary>Перезапускает сетевые службы</summary>
    private static void RestartNetworkServices()
    {
        Console.WriteLine();
        ConsoleUI.Log("Перезапускаем сетевые службы...", true);

        foreach (var service in AppConstants.NetworkServices)
        {
            RestartService(service);
        }
    }

    /// <summary>Сбрасывает сетевые настройки</summary>
    private static void ResetNetworkSettings()
    {
        Console.WriteLine();
        ConsoleUI.Log("Сбрасываем сетевые настройки...", true);

        RunSilentWithArgs("ipconfig", new[] { "/release" });
        RunSilentWithArgs("ipconfig", new[] { "/renew" });
        RunSilentWithArgs("ipconfig", new[] { "/flushdns" });
        RunSilentWithArgs("netsh", new[] { "winsock", "reset" });
        RunSilentWithArgs("netsh", new[] { "int", "ip", "reset" });
    }

    /// <summary>Включает отключенные сетевые адаптеры</summary>
    private static void EnableNetworkAdapters()
    {
        Console.WriteLine();
        ConsoleUI.Log("Проверяем сетевые адаптеры...", true);

        RunSilent("powershell", "-Command \"Get-NetAdapter -Physical | Where-Object {$_.Status -eq 'Disabled'} | Enable-NetAdapter -Confirm:$false\"", AppConstants.PowerShellTimeout);
        ConsoleUI.Log("  + Сетевые адаптеры проверены", true);
    }

    /// <summary>Открывает параметры сети в Windows Settings</summary>
    private static void OpenNetworkSettings()
    {
        Console.WriteLine();
        ConsoleUI.Log("Открываем параметры сети...", true);
        RunSilent("cmd", "/c start ms-settings:network");
    }

    /// <summary>Выводит результат разблокировки</summary>
    private static void PrintUnlockResult(int unlockedCount)
    {
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
    }

    // ═══════════════════════════════════════════════════════════════
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Удаляет ключ реестра целиком</summary>
    private static bool RunRegDelete(string key)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "reg",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            psi.ArgumentList.Add("delete");
            psi.ArgumentList.Add(key);
            psi.ArgumentList.Add("/f");

            using var p = Process.Start(psi);
            p?.WaitForExit();
            return p?.ExitCode == 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RunRegDelete error for {key}: {ex.Message}");
            return false;
        }
    }

    /// <summary>Удаляет конкретное значение из ключа реестра</summary>
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

            using var p = Process.Start(psi);
            p?.WaitForExit();

            if (p?.ExitCode == 0)
            {
                string loc = key.StartsWith("HKCU") ? "HKCU" : "HKLM";
                ConsoleUI.Log($"  + Удалено: {value} ({loc})", true);
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteRegistryValue error for {key}\\{value}: {ex.Message}");
        }
        return false;
    }

    /// <summary>Удаляет ветку реестра целиком</summary>
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

            using var p = Process.Start(psi);
            p?.WaitForExit();

            if (p?.ExitCode == 0)
            {
                ConsoleUI.Log($"  + Удалена ветка: {key}", true);
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteRegistryKey error for {key}: {ex.Message}");
        }
        return false;
    }

    /// <summary>Перезапускает службу Windows</summary>
    private static void RestartService(string name)
    {
        try
        {
            RunSilentWithArgs("net", new[] { "stop", name }, AppConstants.ServiceTimeout);
            if (RunSilentWithArgs("net", new[] { "start", name }, AppConstants.ServiceTimeout))
                ConsoleUI.Log($"  + Перезапущена служба: {name}", true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RestartService error for {name}: {ex.Message}");
        }
    }

    /// <summary>Запускает команду без окна с использованием ArgumentList (безопасно)</summary>
    private static bool RunSilentWithArgs(string cmd, string[] args, int timeout = 0)
    {
        if (timeout == 0)
            timeout = AppConstants.DefaultProcessTimeout;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = cmd,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            foreach (var arg in args)
                psi.ArgumentList.Add(arg);

            using var p = Process.Start(psi);
            p?.WaitForExit(timeout);
            return p?.ExitCode == 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RunSilentWithArgs error for {cmd}: {ex.Message}");
            return false;
        }
    }

    /// <summary>Запускает команду без окна (для строковых аргументов)</summary>
    private static bool RunSilent(string cmd, string args, int timeout = 0)
    {
        if (timeout == 0)
            timeout = AppConstants.DefaultProcessTimeout;

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RunSilent error for {cmd} {args}: {ex.Message}");
            return false;
        }
    }
}
