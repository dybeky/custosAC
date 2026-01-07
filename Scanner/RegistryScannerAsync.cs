using CustosAC.Configuration;
using CustosAC.Models;
using CustosAC.Services;

namespace CustosAC.Scanner;

/// <summary>
/// Async сканер реестра
/// </summary>
public class RegistryScannerAsync : BaseScannerAsync
{
    private readonly RegistryService _registryService;
    private readonly RegistrySettings _registrySettings;

    public override string Name => "Registry Scanner";
    public override string Description => "Поиск в реестре по ключевым словам (MuiCache, AppSwitched, ShowJumpView)";

    public RegistryScannerAsync(
        KeywordMatcherService keywordMatcher,
        ConsoleUIService consoleUI,
        RegistryService registryService,
        ScanSettings scanSettings,
        RegistrySettings registrySettings)
        : base(keywordMatcher, consoleUI, scanSettings)
    {
        _registryService = registryService;
        _registrySettings = registrySettings;
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        var allFindings = new List<string>();

        try
        {
            foreach (var regKey in _registrySettings.ScanKeys)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    if (_registryService.ExportKeyToString(regKey.Path, out var content))
                    {
                        var lines = content.Split('\n');

                        foreach (var line in lines)
                        {
                            var trimmedLine = line.Trim();
                            if (string.IsNullOrEmpty(trimmedLine))
                            {
                                continue;
                            }

                            if (KeywordMatcher.ContainsKeyword(line))
                            {
                                allFindings.Add($"[{regKey.Name}] {trimmedLine}");
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore errors for individual keys
                }
            }

            return CreateSuccessResult(allFindings, startTime);
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
