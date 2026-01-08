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

