using Custos.Core.Abstractions;
using Custos.Core.Configuration;
using Custos.Core.Models;
using Custos.Core.Services;

namespace Custos.Core.Scanner;

/// <summary>
/// Windows Registry scanner
/// </summary>
public class RegistryScannerAsync : BaseScannerAsync
{
    private readonly RegistryService _registryService;
    private readonly RegistrySettings _registrySettings;

    public override string Name => "Registry Scanner";
    public override string Description => "Registry search by keywords (MuiCache, AppSwitched, ShowJumpView)";

    public RegistryScannerAsync(
        KeywordMatcherService keywordMatcher,
        IUIService uiService,
        RegistryService registryService,
        ScanSettings scanSettings,
        RegistrySettings registrySettings)
        : base(keywordMatcher, uiService, scanSettings)
    {
        _registryService = registryService;
        _registrySettings = registrySettings;
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
        {
            var findings = await Task.Run(() =>
            {
                return _registrySettings.ScanKeys
                    .SelectMany(regKey =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return ScanRegistryKey(regKey);
                    })
                    .ToList();
            }, cancellationToken);

            return CreateSuccessResult(findings, startTime);
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
