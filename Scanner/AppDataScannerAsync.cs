using CustosAC.Abstractions;
using CustosAC.Configuration;
using CustosAC.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustosAC.Scanner;

/// <summary>
/// Async сканер AppData
/// </summary>
public class AppDataScannerAsync : BaseScannerAsync
{
    public override string Name => "AppData Scanner";
    public override string Description => "Сканирование папок AppData (Roaming, Local, LocalLow)";

    public AppDataScannerAsync(
        IFileSystemService fileSystem,
        IKeywordMatcher keywordMatcher,
        IConsoleUI consoleUI,
        ILogger<AppDataScannerAsync> logger,
        IOptions<ScanSettings> scanSettings)
        : base(fileSystem, keywordMatcher, consoleUI, logger, scanSettings)
    {
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        Logger.LogInformation("Starting AppData scan");

        try
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localappdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var locallow = Path.Combine(userprofile, "AppData", "LocalLow");

            var folders = new[]
            {
                (path: appdata, name: "AppData\\Roaming"),
                (path: localappdata, name: "AppData\\Local"),
                (path: locallow, name: "AppData\\LocalLow")
            };

            var allResults = new List<string>();

            // Параллельное сканирование всех папок AppData
            var scanTasks = folders.Select(async folder =>
            {
                if (!FileSystem.DirectoryExists(folder.path))
                {
                    Logger.LogDebug("Folder does not exist: {Path}", folder.path);
                    return new List<string>();
                }

                Logger.LogDebug("Scanning: {Folder}", folder.name);
                return await ScanFolderParallelAsync(
                    folder.path,
                    ScanSettings.ExecutableExtensions,
                    ScanSettings.AppDataScanDepth,
                    cancellationToken);
            });

            var results = await Task.WhenAll(scanTasks);
            allResults = results.SelectMany(r => r).ToList();

            Logger.LogInformation("AppData scan completed. Found {Count} suspicious items", allResults.Count);

            return CreateSuccessResult(allResults, startTime);
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("AppData scan was cancelled");
            return CreateErrorResult("Scan cancelled", startTime);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AppData scan failed");
            return CreateErrorResult(ex.Message, startTime);
        }
    }
}
