using CustosAC.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CustosAC.Scanner;

/// <summary>
/// Фабрика сканеров
/// </summary>
public class ScannerFactory : IScannerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ScannerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IScanner CreateAppDataScanner()
    {
        return _serviceProvider.GetRequiredService<AppDataScannerAsync>();
    }

    public IScanner CreateSystemScanner()
    {
        return _serviceProvider.GetRequiredService<SystemScannerAsync>();
    }

    public IScanner CreatePrefetchScanner()
    {
        return _serviceProvider.GetRequiredService<PrefetchScannerAsync>();
    }

    public IScanner CreateRegistryScanner()
    {
        return _serviceProvider.GetRequiredService<RegistryScannerAsync>();
    }

    public IScanner CreateSteamScanner()
    {
        return _serviceProvider.GetRequiredService<SteamScannerAsync>();
    }

    public IEnumerable<IScanner> CreateAllScanners()
    {
        yield return CreateAppDataScanner();
        yield return CreateSystemScanner();
        yield return CreatePrefetchScanner();
        yield return CreateRegistryScanner();
        yield return CreateSteamScanner();
    }
}
