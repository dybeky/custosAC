using System.Collections.Concurrent;
using CustosAC.Core.Abstractions;
using CustosAC.Core.Configuration;
using CustosAC.Core.Models;
using CustosAC.Core.Services;
using Microsoft.Data.Sqlite;

namespace CustosAC.Core.Scanner;

/// <summary>
/// Browser history scanner - checks for suspicious websites (oplata.info, funpay.com, etc.)
/// Enhanced with retry logic, proper error handling, and resource management
/// </summary>
public class BrowserHistoryScannerAsync : BaseScannerAsync
{
    private readonly ExternalResourceSettings _externalSettings;
    private readonly string[] _suspiciousUrls;
    private readonly EnhancedLogService? _enhancedLog;
    private readonly ScannerExceptionHandler _exceptionHandler;
    private readonly TempFileManager _tempFileManager;

    public override string Name => "Browser History Scanner";
    public override string Description => "Searches browser history for suspicious payment/cheat websites";

    public BrowserHistoryScannerAsync(
        KeywordMatcherService keywordMatcher,
        IUIService uiService,
        ScanSettings scanSettings,
        ExternalResourceSettings externalSettings,
        EnhancedLogService? enhancedLog = null,
        ScannerExceptionHandler? exceptionHandler = null,
        TempFileManager? tempFileManager = null)
        : base(keywordMatcher, uiService, scanSettings)
    {
        _externalSettings = externalSettings;
        _suspiciousUrls = externalSettings.WebsitesToCheck
            .Select(w => ExtractDomain(w.Url))
            .Where(d => !string.IsNullOrEmpty(d))
            .ToArray();

        _enhancedLog = enhancedLog;
        _exceptionHandler = exceptionHandler ?? new ScannerExceptionHandler(enhancedLog);
        _tempFileManager = tempFileManager ?? new TempFileManager(enhancedLog);
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
        var findings = new ConcurrentBag<string>();

        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // Scan all Chromium-based browsers in parallel
            var chromiumTasks = new List<Task>
            {
                ScanChromiumHistory(Path.Combine(localAppData, @"Google\Chrome\User Data\Default\History"), "Chrome", findings, cancellationToken),
                ScanChromiumHistory(Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\History"), "Edge", findings, cancellationToken),
                ScanChromiumHistory(Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\History"), "Brave", findings, cancellationToken),
                ScanChromiumHistory(Path.Combine(appData, @"Opera Software\Opera Stable\History"), "Opera", findings, cancellationToken),
                ScanChromiumHistory(Path.Combine(appData, @"Opera Software\Opera GX Stable\History"), "Opera GX", findings, cancellationToken),
                ScanChromiumHistory(Path.Combine(localAppData, @"Yandex\YandexBrowser\User Data\Default\History"), "Yandex", findings, cancellationToken)
            };

            await Task.WhenAll(chromiumTasks);

            // Firefox (synchronous, runs after Chromium browsers)
            await Task.Run(() =>
            {
                var firefoxProfilesPath = Path.Combine(appData, @"Mozilla\Firefox\Profiles");
                ScanFirefoxHistory(firefoxProfilesPath, findings, cancellationToken);
            }, cancellationToken);

            return CreateSuccessResult(findings.ToList(), startTime);
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

    private async Task ScanChromiumHistory(string historyPath, string browserName, ConcurrentBag<string> findings, CancellationToken cancellationToken)
    {
        if (!File.Exists(historyPath))
        {
            _enhancedLog?.LogTrace(EnhancedLogService.LogCategory.Scanner,
                $"History file does not exist: {historyPath}", browserName);
            return;
        }

        if (cancellationToken.IsCancellationRequested) return;

        _enhancedLog?.LogDebug(EnhancedLogService.LogCategory.Scanner,
            $"Scanning {browserName} history", browserName);

        string? tempPath = null;

        try
        {
            // Copy to temp file with retry logic
            tempPath = await _tempFileManager.CopyToTempAsync(historyPath, Name, cancellationToken);

            if (tempPath == null)
            {
                _enhancedLog?.LogWarning(EnhancedLogService.LogCategory.Scanner,
                    $"Failed to copy {browserName} history database", browserName);
                return;
            }

            // Try to open database with retry
            await _exceptionHandler.ExecuteWithRetryAsync(async () =>
            {
                using var connection = new SqliteConnection($"Data Source={tempPath};Mode=ReadOnly");

                // Try to enable WAL mode for concurrent access
                try
                {
                    await connection.OpenAsync(cancellationToken);
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == SQLitePCL.raw.SQLITE_BUSY)
                {
                    _enhancedLog?.LogDebug(EnhancedLogService.LogCategory.Scanner,
                        $"Database busy, will retry: {browserName}", browserName);
                    throw; // Will be retried
                }

                var query = "SELECT url, title, last_visit_time FROM urls ORDER BY last_visit_time DESC LIMIT 5000";
                using var command = new SqliteCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                var foundCount = 0;
                while (await reader.ReadAsync(cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    if (reader.IsDBNull(0)) continue;
                    var url = reader.GetString(0);
                    var title = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    var visitTime = reader.IsDBNull(2) ? 0L : reader.GetInt64(2);

                    foreach (var suspiciousDomain in _suspiciousUrls)
                    {
                        if (url.Contains(suspiciousDomain, StringComparison.OrdinalIgnoreCase))
                        {
                            var dateTime = ConvertChromeTimestamp(visitTime);
                            var siteName = _externalSettings.WebsitesToCheck
                                .FirstOrDefault(w => ExtractDomain(w.Url).Equals(suspiciousDomain, StringComparison.OrdinalIgnoreCase))?.Name ?? suspiciousDomain;

                            findings.Add($"[{browserName}] {siteName}: {url} | Visited: {dateTime:dd.MM.yyyy HH:mm}");
                            foundCount++;
                            break;
                        }
                    }
                }

                if (foundCount > 0)
                {
                    _enhancedLog?.LogWarning(EnhancedLogService.LogCategory.Security,
                        $"Found {foundCount} suspicious URL(s) in {browserName}", browserName);
                }

            }, Name, $"Scan {browserName} history", historyPath, maxRetries: 3, retryDelaysMs: new[] { 100, 500, 2000 });
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == SQLitePCL.raw.SQLITE_CORRUPT)
        {
            _enhancedLog?.LogWarning(EnhancedLogService.LogCategory.Scanner,
                $"Corrupted database: {browserName} history", browserName);
        }
        catch (Exception ex)
        {
            _exceptionHandler.HandleException(ex, Name, $"Scan {browserName} history", historyPath);
        }
        finally
        {
            // Cleanup temp file
            if (tempPath != null)
            {
                _tempFileManager.DeleteTempFile(tempPath, Name);
            }
        }
    }

    private void ScanFirefoxHistory(string profilesPath, ConcurrentBag<string> findings, CancellationToken cancellationToken)
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
                catch (SqliteException ex)
                {
                    _enhancedLog?.LogDebug(EnhancedLogService.LogCategory.Scanner,
                        $"Firefox database error: {ex.Message}", "Firefox");
                }
            }
        }
        catch (Exception ex)
        {
            _enhancedLog?.LogDebug(EnhancedLogService.LogCategory.Scanner,
                $"Firefox scan error: {ex.Message}", "Firefox");
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
