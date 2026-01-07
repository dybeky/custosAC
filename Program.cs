using CustosAC.Configuration;
using CustosAC.Menu;
using CustosAC.Services;

namespace CustosAC;

class Program
{
    static async Task Main(string[] args)
    {
        // Загрузить конфигурацию из JSON
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var config = ConfigRoot.Load(configPath);

        // Создать основные сервисы (singleton)
        var consoleUI = new ConsoleUIService(config.App);
        var adminService = new AdminService();
        var processService = new ProcessService(config.App);

        // Проверка прав администратора
        adminService.RunAsAdmin();
        consoleUI.SetAdminStatus(adminService.IsAdmin());

        // Настройка обработчика закрытия
        adminService.SetupCloseHandler(() =>
        {
            processService.KillAllTrackedProcesses();
            processService.Dispose();
            consoleUI.PrintCleanupMessage();
        });

        // Настройка консоли
        consoleUI.SetupConsole();

        // Создать stateless сервисы
        var keywordMatcher = new KeywordMatcherService(config.Keywords);
        var registryService = new RegistryService();
        var externalCheckService = new ExternalCheckService(
            consoleUI,
            processService,
            config.ExternalResources);

        // Создать меню
        var autoMenu = new AutoMenu(
            consoleUI,
            externalCheckService,
            config.App,
            keywordMatcher,
            config.Scanning,
            config.Paths,
            config.Registry,
            registryService);

        var manualMenu = new ManualMenu(
            consoleUI,
            processService,
            keywordMatcher,
            registryService,
            externalCheckService,
            config.Paths,
            config.Registry,
            config.Scanning);

        var extraMenu = new ExtraMenu(
            consoleUI,
            processService,
            registryService,
            config.Registry,
            config.App,
            config.ExternalResources);

        var mainMenu = new MainMenu(
            consoleUI,
            adminService,
            processService,
            autoMenu,
            manualMenu,
            extraMenu,
            config.App);

        // Запустить приложение
        await mainMenu.RunAsync();

        // Очистка при нормальном выходе
        processService.Dispose();
    }
}
