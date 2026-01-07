using System.Diagnostics;
using CustosAC.Abstractions;
using CustosAC.Configuration;
using CustosAC.Constants;
using Microsoft.Extensions.Options;

namespace CustosAC.Menu;

/// <summary>
/// Меню экстра функций с DI
/// </summary>
public class ExtraMenu
{
    private readonly IConsoleUI _consoleUI;
    private readonly IProcessService _processService;
    private readonly IRegistryService _registryService;
    private readonly RegistrySettings _registrySettings;
    private readonly AppSettings _appSettings;

    public ExtraMenu(
        IConsoleUI consoleUI,
        IProcessService processService,
        IRegistryService registryService,
        IOptions<RegistrySettings> registrySettings,
        IOptions<AppSettings> appSettings)
    {
        _consoleUI = consoleUI;
        _processService = processService;
        _registryService = registryService;
        _registrySettings = registrySettings.Value;
        _appSettings = appSettings.Value;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            _consoleUI.PrintHeader();
            _consoleUI.PrintMenu("ЭКСТРА", new[]
            {
                "Включить реестр",
                "Включить параметры системы и сеть"
            }, true);

            int choice = _consoleUI.GetChoice(2);
            if (choice == 0) break;

            _consoleUI.PrintHeader();
            switch (choice)
            {
                case 1:
                    await EnableRegistryAsync();
                    break;
                case 2:
                    await EnableSystemSettingsAsync();
                    break;
            }
        }
    }

    private async Task EnableRegistryAsync()
    {
        bool success = await _registryService.DeleteKeyAsync(_registrySettings.RegeditBlockPath);

        if (success)
        {
            _consoleUI.Log("Реестр успешно включен", true);
            Console.WriteLine("\n\x1b[32m+ Теперь вы можете открыть regedit\x1b[0m");
        }
        else
        {
            _consoleUI.Log("Ошибка при включении реестра", false);
            Console.WriteLine("\n\x1b[33m[!]\x1b[0m \x1b[33mВозможно реестр уже включен или требуются права администратора\x1b[0m");
        }
        _consoleUI.Pause();
    }

    private async Task EnableSystemSettingsAsync()
    {
        _consoleUI.Log("Разблокируем доступ к параметрам системы и сети...", true);
        Console.WriteLine();

        int unlockedCount = 0;

        // 1. Удаление отдельных значений реестра
        _consoleUI.Log("Удаляем блокировки реестра...", true);
        foreach (var (key, value) in RegistryConstants.ValuesToDelete)
        {
            if (await DeleteRegistryValueAsync(key, value))
                unlockedCount++;
        }

        // 2. Удаление целых веток реестра
        Console.WriteLine();
        _consoleUI.Log("Удаляем политики полностью...", true);
        foreach (var key in RegistryConstants.KeysToDelete)
        {
            if (await _registryService.DeleteKeyAsync(key))
            {
                _consoleUI.Log($"  + Удалена ветка: {key}", true);
                unlockedCount++;
            }
        }

        // 3. Перезапуск сетевых служб
        Console.WriteLine();
        _consoleUI.Log("Перезапускаем сетевые службы...", true);
        foreach (var service in new[] { "netprofm", "NlaSvc", "Dhcp", "Dnscache" })
        {
            await RestartServiceAsync(service);
        }

        // 4. Сброс сетевых настроек
        Console.WriteLine();
        _consoleUI.Log("Сбрасываем сетевые настройки...", true);
        await RunCommandSilentAsync("ipconfig", "/release");
        await RunCommandSilentAsync("ipconfig", "/renew");
        await RunCommandSilentAsync("ipconfig", "/flushdns");
        await RunCommandSilentAsync("netsh", "winsock reset");
        await RunCommandSilentAsync("netsh", "int ip reset");

        // 5. Включение сетевых адаптеров
        Console.WriteLine();
        _consoleUI.Log("Проверяем сетевые адаптеры...", true);
        await RunCommandSilentAsync("powershell", "-Command \"Get-NetAdapter -Physical | Where-Object {$_.Status -eq 'Disabled'} | Enable-NetAdapter -Confirm:$false\"");
        _consoleUI.Log("  + Сетевые адаптеры проверены", true);

        // 6. Открываем параметры сети
        Console.WriteLine();
        _consoleUI.Log("Открываем параметры сети...", true);
        await _processService.OpenUrlAsync("ms-settings:network");

        // Итог
        Console.WriteLine();
        PrintUnlockResult(unlockedCount);
        _consoleUI.Pause();
    }

    private async Task<bool> DeleteRegistryValueAsync(string key, string value)
    {
        var result = await _registryService.DeleteValueAsync(key, value);
        if (result)
        {
            string loc = key.StartsWith("HKCU") ? "HKCU" : "HKLM";
            _consoleUI.Log($"  + Удалено: {value} ({loc})", true);
        }
        return result;
    }

    private async Task RestartServiceAsync(string name)
    {
        try
        {
            await RunCommandSilentAsync("net", $"stop {name}");
            if (await RunCommandSilentAsync("net", $"start {name}"))
            {
                _consoleUI.Log($"  + Перезапущена служба: {name}", true);
            }
        }
        catch
        {
            // Игнорируем ошибки служб
        }
    }

    private async Task<bool> RunCommandSilentAsync(string command, string args)
    {
        return await _processService.RunCommandAsync(command, args, _appSettings.Timeouts.DefaultProcessTimeoutMs);
    }

    private void PrintUnlockResult(int unlockedCount)
    {
        if (unlockedCount > 0)
        {
            Console.WriteLine("\x1b[32m\x1b[1m╔══════════════════════════════════════════════════╗\x1b[0m");
            Console.WriteLine($"\x1b[32m║  + СЕТЬ И ПАРАМЕТРЫ СИСТЕМЫ РАЗБЛОКИРОВАНЫ       ║\x1b[0m");
            Console.WriteLine($"\x1b[32m║    Удалено блокировок: {unlockedCount,-3}                        ║\x1b[0m");
            Console.WriteLine("\x1b[32m\x1b[1m╚══════════════════════════════════════════════════╝\x1b[0m");
            Console.WriteLine();
            Console.WriteLine("\x1b[33m[!]\x1b[0m \x1b[33mРекомендуется перезагрузить компьютер\x1b[0m");
        }
        else
        {
            Console.WriteLine("\x1b[32m+ Блокировки не найдены, сеть уже разблокирована\x1b[0m");
        }
    }
}
