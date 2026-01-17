using System.Text.RegularExpressions;

namespace Custos.Core.Services;

/// <summary>
/// Robust parser for ipconfig /displaydns output
/// </summary>
public class DnsOutputParser
{
    private readonly EnhancedLogService? _logService;

    // Regex patterns for parsing DNS output
    private static readonly Regex RecordNamePattern = new(
        @"Record Name[:\s]+\.+\s*(.+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RecordNameAlternatePattern = new(
        @"^\s*(.+\.[a-z]{2,})\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // RFC 1123 domain validation pattern
    private static readonly Regex DomainPattern = new(
        @"^([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$",
        RegexOptions.Compiled);

    // IP address pattern (to exclude these from domain list)
    private static readonly Regex IpAddressPattern = new(
        @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$",
        RegexOptions.Compiled);

    public DnsOutputParser(EnhancedLogService? logService = null)
    {
        _logService = logService;
    }

    /// <summary>
    /// Parses ipconfig /displaydns output and extracts domain names
    /// </summary>
    public List<string> ParseDnsCache(string dnsOutput)
    {
        var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = dnsOutput.Split('\n');
        var lineNumber = 0;

        foreach (var rawLine in lines)
        {
            lineNumber++;
            var line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Try primary pattern: "Record Name . . . . . . : domain.com"
            var match = RecordNamePattern.Match(line);
            if (match.Success)
            {
                var domain = match.Groups[1].Value.Trim();
                if (ValidateAndAddDomain(domain, domains, lineNumber))
                {
                    continue;
                }
            }

            // Try alternate pattern for lines that might just contain a domain
            if (line.Contains('.') && !line.Contains(':') && !line.Contains("----"))
            {
                var altMatch = RecordNameAlternatePattern.Match(line);
                if (altMatch.Success)
                {
                    var domain = altMatch.Groups[1].Value.Trim();
                    ValidateAndAddDomain(domain, domains, lineNumber);
                }
            }
        }

        _logService?.LogInfo(EnhancedLogService.LogCategory.Scanner,
            $"Extracted {domains.Count} unique domain(s) from DNS cache");

        return domains.ToList();
    }

    /// <summary>
    /// Validates domain format and adds to collection
    /// </summary>
    private bool ValidateAndAddDomain(string domain, HashSet<string> domains, int lineNumber)
    {
        // Clean up domain
        domain = CleanDomain(domain);

        if (string.IsNullOrWhiteSpace(domain))
            return false;

        // Skip IP addresses
        if (IpAddressPattern.IsMatch(domain))
        {
            _logService?.LogTrace(EnhancedLogService.LogCategory.Validation,
                $"Skipping IP address: {domain}", "DnsParser");
            return false;
        }

        // Skip localhost entries
        if (domain.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            domain.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Skip very short domains (likely invalid)
        if (domain.Length < 3)
        {
            return false;
        }

        // Validate domain format
        if (!DomainPattern.IsMatch(domain))
        {
            _logService?.LogTrace(EnhancedLogService.LogCategory.Validation,
                $"Invalid domain format at line {lineNumber}: {domain}", "DnsParser");
            return false;
        }

        // Validate Internationalized Domain Names (IDN)
        if (domain.StartsWith("xn--", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var idn = new System.Globalization.IdnMapping();
                var unicode = idn.GetUnicode(domain);
                _logService?.LogTrace(EnhancedLogService.LogCategory.Validation,
                    $"IDN domain: {domain} -> {unicode}", "DnsParser");
            }
            catch
            {
                _logService?.LogWarning(EnhancedLogService.LogCategory.Validation,
                    $"Invalid IDN format: {domain}", "DnsParser");
                return false;
            }
        }

        domains.Add(domain);
        return true;
    }

    /// <summary>
    /// Cleans up domain string by removing common artifacts
    /// </summary>
    private static string CleanDomain(string domain)
    {
        // Remove trailing dots
        domain = domain.TrimEnd('.');

        // Remove leading dots
        domain = domain.TrimStart('.');

        // Remove common prefixes/suffixes
        if (domain.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            // Keep www. as it might be significant
        }

        // Remove parentheses
        domain = domain.Trim('(', ')');

        // Remove quotes
        domain = domain.Trim('"', '\'');

        return domain.Trim();
    }

    /// <summary>
    /// Extracts only domains matching specific keywords (for targeted search)
    /// </summary>
    public List<string> ParseDnsCacheWithKeywords(string dnsOutput, IEnumerable<string> keywords)
    {
        var allDomains = ParseDnsCache(dnsOutput);
        var matchingDomains = new List<string>();

        foreach (var domain in allDomains)
        {
            foreach (var keyword in keywords)
            {
                if (domain.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    matchingDomains.Add(domain);
                    _logService?.LogDebug(EnhancedLogService.LogCategory.Scanner,
                        $"Domain matches keyword '{keyword}': {domain}", "DnsParser");
                    break;
                }
            }
        }

        return matchingDomains;
    }

    /// <summary>
    /// Checks if domain is in a suspicious TLD
    /// </summary>
    public static bool IsSuspiciousTld(string domain)
    {
        var suspiciousTlds = new[]
        {
            ".tk", ".ml", ".ga", ".cf", ".gq", // Free TLDs often used by malicious actors
            ".top", ".work", ".click", ".link", ".download"
        };

        return suspiciousTlds.Any(tld =>
            domain.EndsWith(tld, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if domain contains suspicious patterns
    /// </summary>
    public static bool HasSuspiciousPattern(string domain)
    {
        // Check for excessive numbers
        var digitCount = domain.Count(char.IsDigit);
        if (digitCount > domain.Length / 2)
        {
            return true;
        }

        // Check for excessive hyphens
        var hyphenCount = domain.Count(c => c == '-');
        if (hyphenCount > 3)
        {
            return true;
        }

        // Check for suspicious keywords in domain
        var suspiciousKeywords = new[]
        {
            "hack", "cheat", "crack", "keygen", "warez", "torrent",
            "download-free", "get-free", "premium-free"
        };

        return suspiciousKeywords.Any(keyword =>
            domain.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
