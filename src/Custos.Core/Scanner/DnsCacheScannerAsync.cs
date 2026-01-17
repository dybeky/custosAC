using System.Diagnostics;
using Custos.Core.Abstractions;
using Custos.Core.Configuration;
using Custos.Core.Models;
using Custos.Core.Services;

namespace Custos.Core.Scanner;

/// <summary>
/// DNS Cache scanner - checks for suspicious domains in Windows DNS cache
/// </summary>
public class DnsCacheScannerAsync : BaseScannerAsync
{
    private readonly ExternalResourceSettings _externalSettings;
    private readonly string[] _suspiciousDomains;

    public override string Name => "DNS Cache Scanner";
    public override string Description => "Scanning DNS cache for suspicious domains";

    public DnsCacheScannerAsync(
        KeywordMatcherService keywordMatcher,
        IUIService uiService,
        ScanSettings scanSettings,
        ExternalResourceSettings externalSettings)
        : base(keywordMatcher, uiService, scanSettings)
    {
        _externalSettings = externalSettings;
        _suspiciousDomains = externalSettings.WebsitesToCheck
            .Select(w => ExtractDomain(w.Url))
            .Where(d => !string.IsNullOrEmpty(d))
            .ToArray();
    }

    private static string ExtractDomain(string url)
    {
        try
        {
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url;
            var uri = new Uri(url);
            return uri.Host.ToLowerInvariant();
        }
        catch
        {
            return url.Replace("https://", "").Replace("http://", "").Split('/')[0].ToLowerInvariant();
        }
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var findings = new List<string>();

        try
        {
            var dnsOutput = await GetDnsCacheAsync(cancellationToken);

            if (string.IsNullOrEmpty(dnsOutput))
                return CreateErrorResult("Failed to retrieve DNS cache", startTime);

            await Task.Run(() =>
            {
                var lines = dnsOutput.Split('\n');
                string? currentRecord = null;

                foreach (var line in lines)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var trimmed = line.Trim();

                    // Record Name line (contains the domain)
                    if (trimmed.StartsWith("Record Name", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.Contains(": ") && !trimmed.StartsWith("Record Type") &&
                        !trimmed.StartsWith("Time To Live") && !trimmed.StartsWith("Data Length") &&
                        !trimmed.StartsWith("Section") && !trimmed.StartsWith("A (Host)") &&
                        !trimmed.StartsWith("AAAA") && !trimmed.StartsWith("CNAME"))
                    {
                        var parts = trimmed.Split(new[] { ": " }, 2, StringSplitOptions.None);
                        if (parts.Length == 2)
                        {
                            currentRecord = parts[1].Trim().ToLowerInvariant();
                        }
                    }
                    // Also check lines that look like domain entries
                    else if (!string.IsNullOrEmpty(trimmed) && trimmed.Contains(".") &&
                             !trimmed.Contains(" ") && !trimmed.StartsWith("-"))
                    {
                        currentRecord = trimmed.ToLowerInvariant();
                    }

                    if (!string.IsNullOrEmpty(currentRecord))
                    {
                        // Check against suspicious domains from config
                        foreach (var suspiciousDomain in _suspiciousDomains)
                        {
                            if (currentRecord.Contains(suspiciousDomain))
                            {
                                var siteName = _externalSettings.WebsitesToCheck
                                    .FirstOrDefault(w => ExtractDomain(w.Url).Equals(suspiciousDomain, StringComparison.OrdinalIgnoreCase))?.Name ?? suspiciousDomain;

                                var finding = $"[DNS Cache] {siteName}: {currentRecord}";
                                if (!findings.Contains(finding))
                                    findings.Add(finding);
                                break;
                            }
                        }

                        // Check against keywords
                        if (KeywordMatcher.ContainsKeyword(currentRecord))
                        {
                            var finding = $"[DNS Cache] Keyword match: {currentRecord}";
                            if (!findings.Contains(finding))
                                findings.Add(finding);
                        }
                    }
                }
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

    private static async Task<string?> GetDnsCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "ipconfig",
                Arguments = "/displaydns",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            try
            {
                await process.WaitForExitAsync(cts.Token);
                return await outputTask;
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(); } catch { }
                throw;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }
}
