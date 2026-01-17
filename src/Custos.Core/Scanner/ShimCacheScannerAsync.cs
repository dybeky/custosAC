using System.Text;
using Microsoft.Win32;
using Custos.Core.Abstractions;
using Custos.Core.Configuration;
using Custos.Core.Models;
using Custos.Core.Services;

namespace Custos.Core.Scanner;

/// <summary>
/// ShimCache (AppCompatCache) scanner - scans Application Compatibility Cache
/// Uses only keyword matching from KeywordSettings.
/// </summary>
public class ShimCacheScannerAsync : BaseScannerAsync
{
    private const string AppCompatCacheKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\AppCompatCache";
    private const string AppCompatFlagsKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";

    public override string Name => "ShimCache Scanner";
    public override string Description => "Scanning Application Compatibility Cache by keywords";

    public ShimCacheScannerAsync(
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
            var results = await Task.Run(() => ScanShimCache(cancellationToken), cancellationToken);
            return CreateSuccessResult(results, startTime);
        }
        catch (OperationCanceledException)
        {
            return CreateErrorResult("Scan cancelled", startTime);
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"ShimCache scan error: {ex.Message}", startTime);
        }
    }

    private List<string> ScanShimCache(CancellationToken cancellationToken)
    {
        var findings = new List<string>();

        // Method 1: Parse AppCompatCache binary data
        var cacheFindings = ParseAppCompatCache(cancellationToken);
        findings.AddRange(cacheFindings);

        // Method 2: Scan AppCompatFlags (compatibility settings)
        var flagsFindings = ScanAppCompatFlags(cancellationToken);
        findings.AddRange(flagsFindings);

        // Method 3: Scan MUICache (recently used programs)
        var muiFindings = ScanMUICache(cancellationToken);
        findings.AddRange(muiFindings);

        // Method 4: Scan UserAssist (program launch count)
        var userAssistFindings = ScanUserAssist(cancellationToken);
        findings.AddRange(userAssistFindings);

        return findings.Distinct().ToList();
    }

    private List<string> ParseAppCompatCache(CancellationToken cancellationToken)
    {
        var findings = new List<string>();

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(AppCompatCacheKey);
            if (key == null) return findings;

            var data = key.GetValue("AppCompatCache") as byte[];
            if (data == null || data.Length < 100) return findings;

            var strings = ExtractStringsFromBinary(data, cancellationToken);

            foreach (var str in strings)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // Check by keywords only
                if (KeywordMatcher.ContainsKeywordWithWhitelist(str, str))
                {
                    findings.Add($"[ShimCache] {str}");
                }
            }
        }
        catch { }

        return findings;
    }

    private List<string> ExtractStringsFromBinary(byte[] data, CancellationToken cancellationToken)
    {
        var strings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder();
        var minLength = 4;

        try
        {
            for (int i = 0; i < data.Length - 1; i += 2)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var c = (char)(data[i] | (data[i + 1] << 8));

                if (c >= 32 && c < 127)
                {
                    sb.Append(c);
                }
                else if (sb.Length >= minLength)
                {
                    strings.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Clear();
                }
            }

            if (sb.Length >= minLength)
            {
                strings.Add(sb.ToString());
            }
        }
        catch { }

        return strings.ToList();
    }

    private List<string> ScanAppCompatFlags(CancellationToken cancellationToken)
    {
        var findings = new List<string>();

        try
        {
            using (var key = Registry.LocalMachine.OpenSubKey(AppCompatFlagsKey))
            {
                if (key != null)
                    findings.AddRange(ScanRegistryKeyValues(key, "[AppCompatFlags-LM]", cancellationToken));
            }

            using (var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"))
            {
                if (key != null)
                    findings.AddRange(ScanRegistryKeyValues(key, "[AppCompatFlags-CU]", cancellationToken));
            }

            using (var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store"))
            {
                if (key != null)
                    findings.AddRange(ScanRegistryKeyValues(key, "[CompatAssistant]", cancellationToken));
            }
        }
        catch { }

        return findings;
    }

    private List<string> ScanMUICache(CancellationToken cancellationToken)
    {
        var findings = new List<string>();

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache");

            if (key == null) return findings;

            foreach (var valueName in key.GetValueNames())
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    var path = valueName;
                    var displayName = key.GetValue(valueName)?.ToString() ?? "";

                    if (path.Contains(".FriendlyAppName"))
                        path = path.Replace(".FriendlyAppName", "");
                    if (path.Contains(".ApplicationCompany"))
                        continue;

                    var combined = $"{path} {displayName}";
                    if (KeywordMatcher.ContainsKeywordWithWhitelist(combined, path))
                    {
                        findings.Add($"[MUICache] {path}");
                    }
                }
                catch { }
            }
        }
        catch { }

        return findings;
    }

    private List<string> ScanUserAssist(CancellationToken cancellationToken)
    {
        var findings = new List<string>();

        try
        {
            using var baseKey = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\UserAssist");

            if (baseKey == null) return findings;

            foreach (var guidKey in baseKey.GetSubKeyNames())
            {
                if (cancellationToken.IsCancellationRequested) break;

                using var countKey = baseKey.OpenSubKey($"{guidKey}\\Count");
                if (countKey == null) continue;

                foreach (var valueName in countKey.GetValueNames())
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var decoded = DecodeRot13(valueName);

                        // Check by keywords only
                        if (KeywordMatcher.ContainsKeywordWithWhitelist(decoded, decoded))
                        {
                            findings.Add($"[UserAssist] {decoded}");
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        return findings;
    }

    private List<string> ScanRegistryKeyValues(RegistryKey key, string prefix, CancellationToken cancellationToken)
    {
        var findings = new List<string>();

        foreach (var valueName in key.GetValueNames())
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var value = key.GetValue(valueName)?.ToString() ?? "";
                var combined = $"{valueName} {value}";

                if (KeywordMatcher.ContainsKeywordWithWhitelist(combined, valueName))
                {
                    findings.Add($"{prefix} {valueName}");
                }
            }
            catch { }
        }

        return findings;
    }

    private static string DecodeRot13(string input)
    {
        var result = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            if (c >= 'a' && c <= 'z')
                result.Append((char)((c - 'a' + 13) % 26 + 'a'));
            else if (c >= 'A' && c <= 'Z')
                result.Append((char)((c - 'A' + 13) % 26 + 'A'));
            else
                result.Append(c);
        }

        return result.ToString();
    }
}
