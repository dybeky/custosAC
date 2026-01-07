using CustosAC.Configuration;
using CustosAC.Models;
using CustosAC.Services;

namespace CustosAC.Scanner;

/// <summary>
/// Сканер Prefetch папки
/// </summary>
public class PrefetchScannerAsync : BaseScannerAsync
{
    private readonly PathSettings _pathSettings;

    public override string Name => "Prefetch Scanner";
    public override string Description => "Сканирование папки Prefetch для поиска следов запуска программ";

    public PrefetchScannerAsync(
        KeywordMatcherService keywordMatcher,
        ConsoleUIService consoleUI,
        ScanSettings scanSettings,
        PathSettings pathSettings)
        : base(keywordMatcher, consoleUI, scanSettings)
    {
        _pathSettings = pathSettings;
    }

    public override Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
        {
            var prefetchPath = _pathSettings.Windows.PrefetchPath;

            if (!Directory.Exists(prefetchPath))
                return Task.FromResult(CreateErrorResult("Папка Prefetch не найдена или недоступна", startTime));

            var findings = Directory.EnumerateFiles(prefetchPath, "*.pf")
                .Where(file => KeywordMatcher.ContainsKeyword(Path.GetFileName(file)))
                .Select(file => $"{file} (Modified: {new FileInfo(file).LastWriteTime:dd.MM.yyyy HH:mm:ss})")
                .ToList();

            return Task.FromResult(CreateSuccessResult(findings, startTime));
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(CreateErrorResult("Нет доступа к папке Prefetch", startTime));
        }
        catch (Exception ex)
        {
            return Task.FromResult(CreateErrorResult(ex.Message, startTime));
        }
    }
}
