using CustosAC.Abstractions;
using CustosAC.Configuration;
using CustosAC.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustosAC.Scanner;

/// <summary>
/// Async сканер системных папок
/// </summary>
public class SystemScannerAsync : BaseScannerAsync
{
    private readonly PathSettings _pathSettings;

    public override string Name => "System Scanner";
    public override string Description => "Сканирование системных папок (Windows, Program Files, Downloads)";

    public SystemScannerAsync(
        IFileSystemService fileSystem,
        IKeywordMatcher keywordMatcher,
        IConsoleUI consoleUI,
        ILogger<SystemScannerAsync> logger,
        IOptions<ScanSettings> scanSettings,
        IOptions<PathSettings> pathSettings)
        : base(fileSystem, keywordMatcher, consoleUI, logger, scanSettings)
    {
        _pathSettings = pathSettings.Value;
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        Logger.LogInformation("Starting System folders scan");

        try
        {
            var userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var folders = new[]
            {
                (path: _pathSettings.Windows.WindowsPath, maxDepth: ScanSettings.WindowsScanDepth),
                (path: _pathSettings.Windows.ProgramFilesX86, maxDepth: ScanSettings.ProgramFilesScanDepth),
                (path: _pathSettings.Windows.ProgramFiles, maxDepth: ScanSettings.ProgramFilesScanDepth),
                (path: Path.Combine(userprofile, "Downloads"), maxDepth: ScanSettings.UserFoldersScanDepth),
                (path: Path.Combine(userprofile, "OneDrive"), maxDepth: ScanSettings.UserFoldersScanDepth)
            };

            var allResults = new List<string>();

            // Параллельное сканирование всех системных папок
            var scanTasks = folders
                .Where(f => FileSystem.DirectoryExists(f.path))
                .Select(async folder =>
                {
                    Logger.LogDebug("Scanning: {Path}", folder.path);
                    return await ScanFolderParallelAsync(
                        folder.path,
                        ScanSettings.ExecutableExtensions,
                        folder.maxDepth,
                        cancellationToken);
                });

            var results = await Task.WhenAll(scanTasks);
            allResults = results.SelectMany(r => r).ToList();

            Logger.LogInformation("System scan completed. Found {Count} suspicious items", allResults.Count);

            return CreateSuccessResult(allResults, startTime);
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("System scan was cancelled");
            return CreateErrorResult("Scan cancelled", startTime);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "System scan failed");
            return CreateErrorResult(ex.Message, startTime);
        }
    }
}
