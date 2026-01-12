using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using CustosAC.Core.Abstractions;
using CustosAC.Core.Configuration;
using CustosAC.Core.Services;
using CustosAC.WPF.Services;
using CustosAC.WPF.ViewModels;

namespace CustosAC.WPF;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load configuration
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var config = ConfigRoot.Load(configPath);

        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services, config);
        Services = services.BuildServiceProvider();

        // Create main window
        var mainWindow = new MainWindow
        {
            DataContext = Services.GetRequiredService<MainViewModel>()
        };
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Dispose MainViewModel to stop timer and clean up resources
        if (Services.GetService<MainViewModel>() is IDisposable disposable)
        {
            disposable.Dispose();
        }

        // Dispose service provider if it supports disposal
        if (Services is IDisposable serviceDisposable)
        {
            serviceDisposable.Dispose();
        }

        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services, ConfigRoot config)
    {
        // Configuration
        services.AddSingleton(config);
        services.AddSingleton(config.App);
        services.AddSingleton(config.Scanning);
        services.AddSingleton(config.Keywords);
        services.AddSingleton(config.Paths);
        services.AddSingleton(config.Registry);
        services.AddSingleton(config.ExternalResources);

        // Core services
        services.AddSingleton<LogService>();
        services.AddSingleton<AdminService>();
        services.AddSingleton<RegistryService>();
        services.AddSingleton<CheatHashDatabase>();
        services.AddSingleton<KeywordMatcherService>();
        services.AddSingleton<ScannerFactory>();
        services.AddSingleton<VersionService>();
        services.AddSingleton<GamePathFinderService>();

        // UI Service
        services.AddSingleton<IUIService, WpfUIService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ScanViewModel>();
        services.AddTransient<ResultsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ManualViewModel>();
        services.AddTransient<UtilitiesViewModel>();
    }
}
