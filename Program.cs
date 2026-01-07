using CustosAC.Abstractions;
using CustosAC.Extensions;
using CustosAC.Menu;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CustosAC;

class Program
{
    static async Task Main(string[] args)
    {
        // Создаём хост с DI контейнером
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddCustosACServices(context.Configuration);
            })
            .ConfigureLogging(logging =>
            {
                // Можно добавить файловое логирование при необходимости
                logging.ClearProviders();
            })
            .Build();

        // Получаем сервисы
        var adminService = host.Services.GetRequiredService<IAdminService>();
        var consoleUI = host.Services.GetRequiredService<IConsoleUI>();
        var processService = host.Services.GetRequiredService<IProcessService>();

        // Проверка и запрос прав администратора
        adminService.RunAsAdmin();

        // Установка статуса администратора для UI
        consoleUI.SetAdminStatus(adminService.IsAdmin());

        // Настройка обработчика закрытия консоли
        adminService.SetupCloseHandler(() =>
        {
            processService.KillAllTrackedProcesses();
            consoleUI.PrintCleanupMessage();
        });

        // Настройка консоли (цвета, фиксированный размер)
        consoleUI.SetupConsole();

        // Запуск главного меню
        var mainMenu = host.Services.GetRequiredService<MainMenu>();
        await mainMenu.RunAsync();
    }
}
