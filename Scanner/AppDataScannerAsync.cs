using CustosAC.Configuration;
using CustosAC.Models;
using CustosAC.Services;

namespace CustosAC.Scanner;

/// <summary>
/// Сканер AppData папок
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

    public override Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var folders = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Path.Combine(userProfile, "AppData", "LocalLow")
            };

            var results = folders
                .Where(Directory.Exists)
                .SelectMany(path => ScanFolder(path, ScanSettings.ExecutableExtensions, ScanSettings.AppDataScanDepth))
                .ToList();

            return Task.FromResult(CreateSuccessResult(results, startTime));
        }
        catch (Exception ex)
        {
            return Task.FromResult(CreateErrorResult(ex.Message, startTime));
        }
    }
}
