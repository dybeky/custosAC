using CustosAC.Abstractions;
using CustosAC.Configuration;
using CustosAC.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustosAC.Scanner;

/// <summary>
/// Async сканер реестра
/// </summary>
public class RegistryScannerAsync : BaseScannerAsync
{
    private readonly IRegistryService _registryService;
    private readonly RegistrySettings _registrySettings;

    public override string Name => "Registry Scanner";
    public override string Description => "Поиск в реестре по ключевым словам (MuiCache, AppSwitched, ShowJumpView)";

    public RegistryScannerAsync(
        IFileSystemService fileSystem,
        IKeywordMatcher keywordMatcher,
        IConsoleUI consoleUI,
        IRegistryService registryService,
        ILogger<RegistryScannerAsync> logger,
        IOptions<ScanSettings> scanSettings,
        IOptions<RegistrySettings> registrySettings)
        : base(fileSystem, keywordMatcher, consoleUI, logger, scanSettings)
    {
        _registryService = registryService;
        _registrySettings = registrySettings.Value;
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        Logger.LogInformation("Starting Registry scan");

        var allFindings = new List<string>();
        var tempDir = Path.Combine(Path.GetTempPath(), "custosAC_temp");

        try
        {
            Directory.CreateDirectory(tempDir);

            foreach (var regKey in _registrySettings.ScanKeys)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                Logger.LogDebug("Scanning registry key: {Key}", regKey.Name);

                var outputFile = Path.Combine(tempDir, regKey.Name + ".reg");

                try
                {
                    var exported = await _registryService.ExportKeyAsync(regKey.Path, outputFile, cancellationToken);

                    if (!exported || !FileSystem.FileExists(outputFile))
                    {
                        Logger.LogDebug("Registry key not found or inaccessible: {Key}", regKey.Name);
                        continue;
                    }

                    var content = FileSystem.ReadAllText(outputFile);
                    var lines = content.Split('\n');

                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (string.IsNullOrEmpty(trimmedLine) ||
                            trimmedLine.StartsWith("Windows Registry") ||
                            trimmedLine.StartsWith("[HKEY"))
                        {
                            continue;
                        }

                        if (KeywordMatcher.ContainsKeyword(line))
                        {
                            allFindings.Add($"[{regKey.Name}] {trimmedLine}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error scanning registry key: {Key}", regKey.Name);
                }
            }

            Logger.LogInformation("Registry scan completed. Found {Count} suspicious entries", allFindings.Count);

            return CreateSuccessResult(allFindings, startTime);
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Registry scan was cancelled");
            return CreateErrorResult("Scan cancelled", startTime);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Registry scan failed");
            return CreateErrorResult(ex.Message, startTime);
        }
        finally
        {
            // Очистка временной папки
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to delete temp folder");
            }
        }
    }
}
