using System.Text;
using Custos.Core.Abstractions;
using Custos.Core.Configuration;
using Custos.Core.Models;
using Custos.Core.Services;

namespace Custos.Core.Scanner;

/// <summary>
/// Amcache scanner - scans Windows Amcache.hve for program execution history.
/// Uses only keyword matching from KeywordSettings.
/// </summary>
public class AmcacheScannerAsync : BaseScannerAsync
{
    private readonly PathSettings _pathSettings;
    private static readonly string AmcachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        "AppCompat", "Programs", "Amcache.hve");

    public override string Name => "Amcache Scanner";
    public override string Description => "Scanning program execution history by keywords";

    public AmcacheScannerAsync(
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
            var results = await Task.Run(() => ScanAmcache(cancellationToken), cancellationToken);
            return CreateSuccessResult(results, startTime);
        }
        catch (OperationCanceledException)
        {
            return CreateErrorResult("Scan cancelled", startTime);
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"Amcache scan error: {ex.Message}", startTime);
        }
    }

    private List<string> ScanAmcache(CancellationToken cancellationToken)
    {
        var findings = new List<string>();

        // Method 1: Try to extract strings from Amcache.hve copy
        var amcacheFindings = ExtractFromAmcacheFile(cancellationToken);
        findings.AddRange(amcacheFindings);

        // Method 2: Scan Amcache-related registry keys
        var registryFindings = ScanAmcacheRegistry(cancellationToken);
        findings.AddRange(registryFindings);

        // Method 3: Check RecentFileCache.bcf (older Windows)
        var recentCacheFindings = ScanRecentFileCache(cancellationToken);
        findings.AddRange(recentCacheFindings);

        return findings.Distinct().ToList();
    }

    private List<string> ExtractFromAmcacheFile(CancellationToken cancellationToken)
    {
        var findings = new List<string>();

        if (!File.Exists(AmcachePath))
            return findings;

        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"amcache_scan_{Guid.NewGuid():N}.tmp");

            try
            {
                if (TryCopyAmcache(AmcachePath, tempPath))
                {
                    var strings = ExtractStringsFromFile(tempPath, cancellationToken);

                    foreach (var str in strings)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        // Check by keywords only
                        if (KeywordMatcher.ContainsKeywordWithWhitelist(str, str))
                        {
                            findings.Add($"[Amcache] {str}");
                        }
                    }
                }
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            }
        }
        catch { }

        return findings;
    }

    private bool TryCopyAmcache(string source, string destination)
    {
        try
        {
            File.Copy(source, destination, true);
            return true;
        }
        catch
        {
            try
            {
                using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var destStream = new FileStream(destination, FileMode.Create, FileAccess.Write);
                sourceStream.CopyTo(destStream);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private List<string> ExtractStringsFromFile(string filePath, CancellationToken cancellationToken)
    {
        var strings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var minLength = 4;

        try
        {
            var bytes = File.ReadAllBytes(filePath);
            var sb = new StringBuilder();

            // Extract ASCII strings
            foreach (var b in bytes)
            {
                if (cancellationToken.IsCancellationRequested) break;

                if (b >= 32 && b < 127)
                {
                    sb.Append((char)b);
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

            // Extract Unicode strings
            sb.Clear();
            for (int i = 0; i < bytes.Length - 1; i += 2)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var c = BitConverter.ToChar(bytes, i);
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
        }
        catch { }

        return strings.ToList();
    }

    private List<string> ScanAmcacheRegistry(CancellationToken cancellationToken)
    {
        var findings = new List<string>();

        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModel\StateRepository\Cache\Application\Data");

            if (key != null)
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        using var subKey = key.OpenSubKey(subKeyName);
                        var displayName = subKey?.GetValue("DisplayName")?.ToString();
                        var packageName = subKey?.GetValue("PackageFullName")?.ToString();

                        var combined = $"{displayName} {packageName}";
                        if (!string.IsNullOrEmpty(combined) &&
                            KeywordMatcher.ContainsKeywordWithWhitelist(combined, combined))
                        {
                            findings.Add($"[AppModel] {combined.Trim()}");
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        return findings;
    }

    private List<string> ScanRecentFileCache(CancellationToken cancellationToken)
    {
        var findings = new List<string>();

        var recentFileCachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "AppCompat", "Programs", "RecentFileCache.bcf");

        if (!File.Exists(recentFileCachePath))
            return findings;

        try
        {
            var content = File.ReadAllText(recentFileCachePath, Encoding.Unicode);
            var entries = content.Split('\0').Where(s => !string.IsNullOrWhiteSpace(s));

            foreach (var entry in entries)
            {
                if (cancellationToken.IsCancellationRequested) break;

                if (KeywordMatcher.ContainsKeywordWithWhitelist(entry, entry))
                {
                    findings.Add($"[RecentFileCache] {entry}");
                }
            }
        }
        catch { }

        return findings;
    }
}
