using CustosAC.Core.Models;

namespace CustosAC.Core.Abstractions;

/// <summary>
/// Scanner interface for all scanning modules
/// </summary>
public interface IScanner
{
    /// <summary>Scanner name</summary>
    string Name { get; }

    /// <summary>Scanner description</summary>
    string Description { get; }

    /// <summary>Execute scan</summary>
    Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default);

    /// <summary>Execute scan with progress reporting</summary>
    Task<ScanResult> ScanAsync(IProgress<ScanProgress>? progress, CancellationToken cancellationToken = default);
}
