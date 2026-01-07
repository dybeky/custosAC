using CustosAC.Abstractions;
using CustosAC.Configuration;
using Microsoft.Extensions.Options;

namespace CustosAC.Menu;

/// <summary>
/// Главное меню с DI
/// </summary>
public class MainMenu
{
    private readonly IConsoleUI _consoleUI;
    private readonly IAdminService _adminService;
    private readonly IProcessService _processService;
    private readonly AutoMenu _autoMenu;
    private readonly ManualMenu _manualMenu;
    private readonly ExtraMenu _extraMenu;
    private readonly AppSettings _settings;

    public MainMenu(
        IConsoleUI consoleUI,
        IAdminService adminService,
        IProcessService processService,
        AutoMenu autoMenu,
        ManualMenu manualMenu,
        ExtraMenu extraMenu,
        IOptions<AppSettings> settings)
    {
        _consoleUI = consoleUI;
        _adminService = adminService;
        _processService = processService;
        _autoMenu = autoMenu;
        _manualMenu = manualMenu;
        _extraMenu = extraMenu;
        _settings = settings.Value;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            _consoleUI.PrintHeader();
            _consoleUI.PrintMenu("ГЛАВНОЕ МЕНЮ", new[]
            {
                "Ручная проверка",
                "Автоматическая проверка",
                "Экстра"
            }, false);

            int choice = _consoleUI.GetChoice(3);

            switch (choice)
            {
                case 0:
                    await ExitApplicationAsync();
                    return;
                case 1:
                    await _manualMenu.RunAsync();
                    break;
                case 2:
                    await _autoMenu.RunAsync();
                    break;
                case 3:
                    await _extraMenu.RunAsync();
                    break;
            }
        }
    }

    private async Task ExitApplicationAsync()
    {
        _consoleUI.ClearScreen();
        _consoleUI.PrintEmptyLine();
        _consoleUI.PrintEmptyLine();
        _consoleUI.PrintBoxOrange(new[] { "Закрываем открытые процессы..." });

        await Task.Delay(_settings.Timeouts.ExitDelayMs);
        _processService.KillAllTrackedProcesses();
        _consoleUI.PrintCleanupMessage();
        await Task.Delay(_settings.Timeouts.CleanupDelayMs);
    }
}
