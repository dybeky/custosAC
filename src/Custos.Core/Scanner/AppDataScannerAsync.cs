using Custos.Core.Abstractions;
using Custos.Core.Configuration;
using Custos.Core.Models;
using Custos.Core.Services;

namespace Custos.Core.Scanner;

/// <summary>
/// AppData folders scanner
/// </summary>
public class AppDataScannerAsync : BaseScannerAsync
{
    public override string Name => "AppData Scanner";
    public override string Description => "Scanning AppData folders by keywords";

    public AppDataScannerAsync(
        KeywordMatcherService keywordMatcher,
        IUIService uiService,
        ScanSettings scanSettings)
        : base(keywordMatcher, uiService, scanSettings)
    {
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
        {
            var results = await Task.Run(() =>
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var folders = new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Path.Combine(userProfile, "AppData", "LocalLow")
                };

                // Search by keywords only - no extension filtering
                return folders
                    .Where(Directory.Exists)
                    .SelectMany(path => ScanFolder(path, Array.Empty<string>(), ScanSettings.AppDataScanDepth, cancellationToken))
                    .ToList();
            }, cancellationToken);

            return CreateSuccessResult(results, startTime);
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
