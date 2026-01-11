using CustosAC.Core.Abstractions;
using CustosAC.Core.Configuration;
using CustosAC.Core.Models;
using CustosAC.Core.Services;

namespace CustosAC.Core.Scanner;

/// <summary>
/// Prefetch folder scanner
/// </summary>
public class PrefetchScannerAsync : BaseScannerAsync
{
    private readonly PathSettings _pathSettings;

    public override string Name => "Prefetch Scanner";
    public override string Description => "Scanning Prefetch folder for program execution traces";

    public PrefetchScannerAsync(
        KeywordMatcherService keywordMatcher,
        IUIService uiService,
        ScanSettings scanSettings,
        PathSettings pathSettings)
        : base(keywordMatcher, uiService, scanSettings)
    {
        _pathSettings = pathSettings;
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
        {
            var prefetchPath = _pathSettings.Windows.PrefetchPath;

            if (!Directory.Exists(prefetchPath))
                return CreateErrorResult("Prefetch folder not found or inaccessible", startTime);

            var findings = await Task.Run(() =>
            {
                return Directory.EnumerateFiles(prefetchPath, "*.pf")
                    .Where(file =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return KeywordMatcher.ContainsKeyword(Path.GetFileName(file));
                    })
                    .Select(file => $"{file} (Modified: {new FileInfo(file).LastWriteTime:dd.MM.yyyy HH:mm:ss})")
                    .ToList();
            }, cancellationToken);

            return CreateSuccessResult(findings, startTime);
        }
        catch (OperationCanceledException)
        {
            return CreateErrorResult("Scan cancelled", startTime);
        }
        catch (UnauthorizedAccessException)
        {
            return CreateErrorResult("No access to Prefetch folder", startTime);
        }
        catch (Exception ex)
        {
            return CreateErrorResult(ex.Message, startTime);
        }
    }
}
