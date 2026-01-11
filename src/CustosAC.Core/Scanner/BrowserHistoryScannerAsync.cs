using CustosAC.Core.Abstractions;
using CustosAC.Core.Configuration;
using CustosAC.Core.Models;
using CustosAC.Core.Services;
using Microsoft.Data.Sqlite;

namespace CustosAC.Core.Scanner;

/// <summary>
/// Browser history scanner - checks for suspicious websites (oplata.info, funpay.com, etc.)
/// </summary>
public class BrowserHistoryScannerAsync : BaseScannerAsync
{
    private readonly ExternalResourceSettings _externalSettings;
    private readonly string[] _suspiciousUrls;

    public override string Name => "Browser History Scanner";
    public override string Description => "Searches browser history for suspicious payment/cheat websites";

    public BrowserHistoryScannerAsync(
        KeywordMatcherService keywordMatcher,
        IUIService uiService,
        ScanSettings scanSettings,
        ExternalResourceSettings externalSettings)
        : base(keywordMatcher, uiService, scanSettings)
    {
        _externalSettings = externalSettings;
        _suspiciousUrls = externalSettings.WebsitesToCheck
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
            await Task.Run(() =>
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                // Chrome
                var chromePath = Path.Combine(localAppData, @"Google\Chrome\User Data\Default\History");
                ScanChromiumHistory(chromePath, "Chrome", findings, cancellationToken);

                // Edge
                var edgePath = Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\History");
                ScanChromiumHistory(edgePath, "Edge", findings, cancellationToken);

                // Brave
                var bravePath = Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\History");
                ScanChromiumHistory(bravePath, "Brave", findings, cancellationToken);

                // Opera
                var operaPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Opera Software\Opera Stable\History");
                ScanChromiumHistory(operaPath, "Opera", findings, cancellationToken);

                // Opera GX
                var operaGxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Opera Software\Opera GX Stable\History");
                ScanChromiumHistory(operaGxPath, "Opera GX", findings, cancellationToken);

                // Yandex Browser
                var yandexPath = Path.Combine(localAppData, @"Yandex\YandexBrowser\User Data\Default\History");
                ScanChromiumHistory(yandexPath, "Yandex", findings, cancellationToken);

                // Firefox
                var firefoxProfilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Mozilla\Firefox\Profiles");
                ScanFirefoxHistory(firefoxProfilesPath, findings, cancellationToken);

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

    private void ScanChromiumHistory(string historyPath, string browserName, List<string> findings, CancellationToken cancellationToken)
    {
        if (!File.Exists(historyPath)) return;
        if (cancellationToken.IsCancellationRequested) return;

        try
        {
            // Copy to temp file because browser might lock the database
            var tempPath = Path.Combine(Path.GetTempPath(), $"custosac_history_{Guid.NewGuid()}.db");
            File.Copy(historyPath, tempPath, true);

            try
            {
                using var connection = new SqliteConnection($"Data Source={tempPath};Mode=ReadOnly");
                connection.Open();

                var query = "SELECT url, title, last_visit_time FROM urls ORDER BY last_visit_time DESC LIMIT 5000";
                using var command = new SqliteCommand(query, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var url = reader.GetString(0);
                    var title = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    var visitTime = reader.GetInt64(2);

                    foreach (var suspiciousDomain in _suspiciousUrls)
                    {
                        if (url.Contains(suspiciousDomain, StringComparison.OrdinalIgnoreCase))
                        {
                            var dateTime = ConvertChromeTimestamp(visitTime);
                            var siteName = _externalSettings.WebsitesToCheck
                                .FirstOrDefault(w => ExtractDomain(w.Url).Equals(suspiciousDomain, StringComparison.OrdinalIgnoreCase))?.Name ?? suspiciousDomain;

                            findings.Add($"[{browserName}] {siteName}: {url} | Visited: {dateTime:dd.MM.yyyy HH:mm}");
                            break;
                        }
                    }
                }
            }
            finally
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
        catch (SqliteException)
        {
            // Database might be locked or corrupted
        }
        catch (Exception)
        {
            // Silently ignore errors for individual browsers
        }
    }

    private void ScanFirefoxHistory(string profilesPath, List<string> findings, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(profilesPath)) return;

        try
        {
            foreach (var profileDir in Directory.GetDirectories(profilesPath))
            {
                if (cancellationToken.IsCancellationRequested) break;

                var placesPath = Path.Combine(profileDir, "places.sqlite");
                if (!File.Exists(placesPath)) continue;

                try
                {
                    // Copy to temp file because browser might lock the database
                    var tempPath = Path.Combine(Path.GetTempPath(), $"custosac_firefox_{Guid.NewGuid()}.db");
                    File.Copy(placesPath, tempPath, true);

                    try
                    {
                        using var connection = new SqliteConnection($"Data Source={tempPath};Mode=ReadOnly");
                        connection.Open();

                        var query = @"SELECT p.url, p.title, h.visit_date
                                      FROM moz_places p
                                      LEFT JOIN moz_historyvisits h ON p.id = h.place_id
                                      ORDER BY h.visit_date DESC LIMIT 5000";
                        using var command = new SqliteCommand(query, connection);
                        using var reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            if (cancellationToken.IsCancellationRequested) break;

                            var url = reader.GetString(0);
                            var title = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            var visitTime = reader.IsDBNull(2) ? 0L : reader.GetInt64(2);

                            foreach (var suspiciousDomain in _suspiciousUrls)
                            {
                                if (url.Contains(suspiciousDomain, StringComparison.OrdinalIgnoreCase))
                                {
                                    var dateTime = ConvertFirefoxTimestamp(visitTime);
                                    var siteName = _externalSettings.WebsitesToCheck
                                        .FirstOrDefault(w => ExtractDomain(w.Url).Equals(suspiciousDomain, StringComparison.OrdinalIgnoreCase))?.Name ?? suspiciousDomain;

                                    findings.Add($"[Firefox] {siteName}: {url} | Visited: {dateTime:dd.MM.yyyy HH:mm}");
                                    break;
                                }
                            }
                        }
                    }
                    finally
                    {
                        try { File.Delete(tempPath); } catch { }
                    }
                }
                catch (SqliteException)
                {
                    // Database might be locked or corrupted
                }
            }
        }
        catch (Exception)
        {
            // Silently ignore errors
        }
    }

    private static DateTime ConvertChromeTimestamp(long chromeTimestamp)
    {
        // Chrome timestamp is microseconds since January 1, 1601 UTC
        try
        {
            if (chromeTimestamp <= 0) return DateTime.MinValue;
            return DateTime.FromFileTimeUtc(chromeTimestamp * 10).ToLocalTime();
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    private static DateTime ConvertFirefoxTimestamp(long firefoxTimestamp)
    {
        // Firefox timestamp is microseconds since Unix epoch
        try
        {
            if (firefoxTimestamp <= 0) return DateTime.MinValue;
            return DateTimeOffset.FromUnixTimeMilliseconds(firefoxTimestamp / 1000).LocalDateTime;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}
