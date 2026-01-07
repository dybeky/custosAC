using CustosAC.Configuration;
using CustosAC.Models;
using CustosAC.Services;

namespace CustosAC.Scanner;

/// <summary>
/// Сканер системных папок
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

    public override Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
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

            var results = folders
                .Where(f => Directory.Exists(f.path))
                .SelectMany(f => ScanFolder(f.path, ScanSettings.ExecutableExtensions, f.depth))
                .ToList();

            return Task.FromResult(CreateSuccessResult(results, startTime));
        }
        catch (Exception ex)
        {
            return Task.FromResult(CreateErrorResult(ex.Message, startTime));
        }
    }
}
