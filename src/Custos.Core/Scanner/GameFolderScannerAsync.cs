using Custos.Core.Abstractions;
using Custos.Core.Configuration;
using Custos.Core.Models;
using Custos.Core.Services;

namespace Custos.Core.Scanner;

/// <summary>
/// Game folders scanner - scans game directories for cheats
/// Uses only keyword matching from KeywordSettings
/// </summary>
public class GameFolderScannerAsync : BaseScannerAsync
{
    private readonly PathSettings _pathSettings;
    private readonly GamePathFinderService _gamePathFinder;

    public override string Name => "Game Folder Scanner";
    public override string Description => "Scanning game folders by keywords";

    public GameFolderScannerAsync(
        KeywordMatcherService keywordMatcher,
        IUIService uiService,
        ScanSettings scanSettings,
        PathSettings pathSettings,
        GamePathFinderService gamePathFinder)
        : base(keywordMatcher, uiService, scanSettings)
    {
        _pathSettings = pathSettings;
        _gamePathFinder = gamePathFinder;
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
        {
            var results = await Task.Run(() =>
            {
                var findings = new List<string>();

                // Get all possible game paths
                var gamePaths = GetGamePaths();

                foreach (var gamePath in gamePaths)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    if (!Directory.Exists(gamePath)) continue;

                    // Scan by keywords only
                    var keywordResults = ScanFolder(gamePath, Array.Empty<string>(), ScanSettings.ProgramFilesScanDepth, cancellationToken);
                    findings.AddRange(keywordResults);
                }

                return findings.Distinct().ToList();
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

    private List<string> GetGamePaths()
    {
        var paths = new List<string>();

        // Standard Steam paths for Unturned
        var steamPaths = new[]
        {
            _pathSettings.Windows.ProgramFilesX86,
            _pathSettings.Windows.ProgramFiles
        };

        foreach (var steamPath in steamPaths)
        {
            paths.Add(Path.Combine(steamPath, "Steam", "steamapps", "common", "Unturned"));
        }

        // Additional drives
        foreach (var drive in _pathSettings.Steam.AdditionalDrives)
        {
            paths.Add(Path.Combine(drive, "Steam", "steamapps", "common", "Unturned"));
            paths.Add(Path.Combine(drive, "SteamLibrary", "steamapps", "common", "Unturned"));
            paths.Add(Path.Combine(drive, "Games", "Steam", "steamapps", "common", "Unturned"));
        }

        // Try to find via GamePathFinderService
        var foundPaths = _gamePathFinder.FindUnturnedPaths();
        paths.AddRange(foundPaths);

        return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
