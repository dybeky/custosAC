using CustosAC.Models;

namespace CustosAC.Abstractions;

/// <summary>
/// Интерфейс сканера
/// </summary>
public interface IScanner
{
    /// <summary>Имя сканера</summary>
    string Name { get; }

    /// <summary>Описание сканера</summary>
    string Description { get; }

    /// <summary>Выполнить сканирование</summary>
    Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default);

    /// <summary>Выполнить сканирование с прогрессом</summary>
    Task<ScanResult> ScanAsync(IProgress<ScanProgress>? progress, CancellationToken cancellationToken = default);
}

/// <summary>
/// Фабрика сканеров
/// </summary>
public interface IScannerFactory
{
    /// <summary>Создать сканер AppData</summary>
    IScanner CreateAppDataScanner();

    /// <summary>Создать сканер системных папок</summary>
    IScanner CreateSystemScanner();

    /// <summary>Создать сканер Prefetch</summary>
    IScanner CreatePrefetchScanner();

    /// <summary>Создать сканер реестра</summary>
    IScanner CreateRegistryScanner();

    /// <summary>Создать сканер Steam</summary>
    IScanner CreateSteamScanner();

    /// <summary>Создать все сканеры</summary>
    IEnumerable<IScanner> CreateAllScanners();
}
