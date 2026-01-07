using CustosAC.Configuration;
using CustosAC.Models;
using CustosAC.Services;

namespace CustosAC.Scanner;

/// <summary>
/// Async сканер Prefetch
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

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
        {
            var prefetchPath = _pathSettings.Windows.PrefetchPath;

            if (!Directory.Exists(prefetchPath))
            {
                return CreateErrorResult("Папка Prefetch не найдена или недоступна", startTime);
            }

            var suspiciousFiles = new List<string>();

            await Task.Run(() =>
            {
                try
                {
                    var files = Directory.EnumerateFiles(prefetchPath, "*.pf");

                    foreach (var file in files)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        var fileName = Path.GetFileName(file);

                        if (KeywordMatcher.ContainsKeyword(fileName))
                        {
                            var fileInfo = new FileInfo(file);
                            suspiciousFiles.Add($"{file} (Modified: {fileInfo.LastWriteTime:dd.MM.yyyy HH:mm:ss})");
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Access denied
                }
                catch (IOException)
                {
                    // IO error
                }
            }, cancellationToken);

            return CreateSuccessResult(suspiciousFiles, startTime);
        }
        catch (OperationCanceledException)
        {
            return CreateErrorResult("Scan cancelled", startTime);
        }
        catch (Exception ex)
        {
            return CreateErrorResult(ex.Message, startTime);
        }
    }
}
