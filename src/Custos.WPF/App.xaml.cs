using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Custos.Core.Abstractions;
using Custos.Core.Configuration;
using Custos.Core.Services;
using Custos.WPF.Services;
using Custos.WPF.ViewModels;

namespace Custos.WPF;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Load configuration
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var config = ConfigRoot.Load(configPath);

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services, config);
            Services = services.BuildServiceProvider();

            // Show Splash Screen first
            var versionService = Services.GetRequiredService<VersionService>();
            var splashViewModel = new SplashViewModel(versionService);
            var splashWindow = new SplashWindow(splashViewModel);

            var dialogResult = splashWindow.ShowDialog();

            // If user chose to update, exit the app
            if (splashWindow.ShouldExitApp)
            {
                Shutdown();
                return;
            }

            // Create main window
            var mainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
            mainWindow.Closed += (_, _) => Shutdown();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup error: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
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
        services.AddSingleton<KeywordMatcherService>();
        services.AddSingleton<GamePathFinderService>();
        services.AddSingleton<ScannerFactory>();
        services.AddSingleton<VersionService>();

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
