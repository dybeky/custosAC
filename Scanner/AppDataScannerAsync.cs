using CustosAC.Configuration;
using CustosAC.Models;
using CustosAC.Services;

namespace CustosAC.Scanner;

/// <summary>
/// Async сканер AppData
/// </summary>
public class AppDataScannerAsync : BaseScannerAsync
{
    public override string Name => "AppData Scanner";
    public override string Description => "Сканирование папок AppData (Roaming, Local, LocalLow)";

    public AppDataScannerAsync(
        KeywordMatcherService keywordMatcher,
        ConsoleUIService consoleUI,
        ScanSettings scanSettings)
        : base(keywordMatcher, consoleUI, scanSettings)
    {
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

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
                if (!Directory.Exists(folder.path))
                {
                    return new List<string>();
                }

                return await ScanFolderParallelAsync(
                    folder.path,
                    ScanSettings.ExecutableExtensions,
                    ScanSettings.AppDataScanDepth,
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
