using CustosAC.Configuration;
using CustosAC.Models;
using CustosAC.Services;

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
                .Where(f => Directory.Exists(f.path))
                .Select(async folder =>
                {
                    return await ScanFolderParallelAsync(
                        folder.path,
                        ScanSettings.ExecutableExtensions,
                        folder.maxDepth,
                        cancellationToken);
                });

            var results = await Task.WhenAll(scanTasks);
            allResults = results.SelectMany(r => r).ToList();

            return CreateSuccessResult(allResults, startTime);
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
