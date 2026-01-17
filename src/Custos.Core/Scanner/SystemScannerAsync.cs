using Custos.Core.Abstractions;
using Custos.Core.Configuration;
using Custos.Core.Models;
using Custos.Core.Services;

namespace Custos.Core.Scanner;

/// <summary>
/// System folders scanner
/// </summary>
public class SystemScannerAsync : BaseScannerAsync
{
    private readonly PathSettings _pathSettings;

    public override string Name => "Pow pow";
    public override string Description => "Scanning system folders by keywords";

    public SystemScannerAsync(
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
            var results = await Task.Run(() =>
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                var folders = new (string path, int depth)[]
                {
                    (_pathSettings.Windows.WindowsPath, ScanSettings.WindowsScanDepth),
                    (_pathSettings.Windows.ProgramFilesX86, ScanSettings.ProgramFilesScanDepth),
                    (_pathSettings.Windows.ProgramFiles, ScanSettings.ProgramFilesScanDepth),
                    (Path.Combine(userProfile, "Downloads"), ScanSettings.UserFoldersScanDepth),
                    (Path.Combine(userProfile, "OneDrive"), ScanSettings.UserFoldersScanDepth)
                };

                // Search by keywords only - no extension filtering
                return folders
                    .Where(f => Directory.Exists(f.path))
                    .SelectMany(f => ScanFolder(f.path, Array.Empty<string>(), f.depth, cancellationToken))
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
