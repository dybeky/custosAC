using CustosAC.Configuration;
using CustosAC.Models;
using CustosAC.Services;

namespace CustosAC.Scanner;

/// <summary>
/// Сканер реестра Windows
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

    public override Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
        {
            var findings = _registrySettings.ScanKeys
                .SelectMany(regKey => ScanRegistryKey(regKey))
                .ToList();

            return Task.FromResult(CreateSuccessResult(findings, startTime));
        }
        catch (Exception ex)
        {
            return Task.FromResult(CreateErrorResult(ex.Message, startTime));
        }
    }

    private IEnumerable<string> ScanRegistryKey(RegistryScanKey regKey)
    {
        if (!_registryService.ExportKeyToString(regKey.Path, out var content))
            yield break;

        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed) && KeywordMatcher.ContainsKeyword(line))
                yield return $"[{regKey.Name}] {trimmed}";
        }
    }
}
