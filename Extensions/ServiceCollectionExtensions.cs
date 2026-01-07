using CustosAC.Abstractions;
using CustosAC.Configuration;
using CustosAC.Scanner;
using CustosAC.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CustosAC.Extensions;

/// <summary>
/// Расширения для регистрации сервисов в DI контейнере
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует все сервисы CustosAC
    /// </summary>
    public static IServiceCollection AddCustosACServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Конфигурация
        services.Configure<AppSettings>(configuration.GetSection("App"));
        services.Configure<ScanSettings>(configuration.GetSection("Scanning"));
        services.Configure<KeywordSettings>(configuration.GetSection("Keywords"));
        services.Configure<PathSettings>(configuration.GetSection("Paths"));
        services.Configure<RegistrySettings>(configuration.GetSection("Registry"));
        services.Configure<ExternalResourceSettings>(configuration.GetSection("ExternalResources"));

        // Core Services (Singleton - stateful)
        services.AddSingleton<IConsoleUI, ConsoleUIService>();
        services.AddSingleton<IAdminService, AdminService>();
        services.AddSingleton<IProcessService, ProcessService>();

        // Stateless Services (Transient)
        services.AddTransient<IKeywordMatcher, KeywordMatcherService>();
        services.AddTransient<IFileSystemService, FileSystemService>();
        services.AddTransient<IRegistryService, RegistryService>();
        services.AddTransient<IExternalCheckService, ExternalCheckService>();

        // Scanners (Transient)
        services.AddTransient<AppDataScannerAsync>();
        services.AddTransient<SystemScannerAsync>();
        services.AddTransient<PrefetchScannerAsync>();
        services.AddTransient<RegistryScannerAsync>();
        services.AddTransient<SteamScannerAsync>();
        services.AddTransient<IScannerFactory, ScannerFactory>();

        // Menus (Transient)
        services.AddTransient<Menu.MainMenu>();
        services.AddTransient<Menu.AutoMenu>();
        services.AddTransient<Menu.ManualMenu>();
        services.AddTransient<Menu.ExtraMenu>();

        return services;
    }
}
