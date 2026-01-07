using CustosAC.Abstractions;
using CustosAC.Configuration;
using CustosAC.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        IFileSystemService fileSystem,
        IKeywordMatcher keywordMatcher,
        IConsoleUI consoleUI,
        ILogger<PrefetchScannerAsync> logger,
        IOptions<ScanSettings> scanSettings,
        IOptions<PathSettings> pathSettings)
        : base(fileSystem, keywordMatcher, consoleUI, logger, scanSettings)
    {
        _pathSettings = pathSettings.Value;
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        Logger.LogInformation("Starting Prefetch scan");

        try
        {
            var prefetchPath = _pathSettings.Windows.PrefetchPath;

            if (!FileSystem.DirectoryExists(prefetchPath))
            {
                Logger.LogWarning("Prefetch folder not found: {Path}", prefetchPath);
                return CreateErrorResult("Папка Prefetch не найдена или недоступна", startTime);
            }

            var suspiciousFiles = new List<string>();

            await Task.Run(() =>
            {
                try
                {
                    var files = FileSystem.EnumerateFiles(prefetchPath, "*.pf");

                    foreach (var file in files)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        var fileName = FileSystem.GetFileName(file);

                        if (KeywordMatcher.ContainsKeyword(fileName))
                        {
                            var fileInfo = FileSystem.GetFileInfo(file);
                            suspiciousFiles.Add($"{file} (Modified: {fileInfo.LastWriteTime:dd.MM.yyyy HH:mm:ss})");
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Logger.LogWarning(ex, "Access denied to Prefetch folder");
                }
                catch (IOException ex)
                {
                    Logger.LogWarning(ex, "IO error reading Prefetch folder");
                }
            }, cancellationToken);

            Logger.LogInformation("Prefetch scan completed. Found {Count} suspicious items", suspiciousFiles.Count);

            return CreateSuccessResult(suspiciousFiles, startTime);
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Prefetch scan was cancelled");
            return CreateErrorResult("Scan cancelled", startTime);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Prefetch scan failed");
            return CreateErrorResult(ex.Message, startTime);
        }
    }
}
