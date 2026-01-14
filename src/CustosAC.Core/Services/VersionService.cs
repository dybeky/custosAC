using System.Text.Json;

namespace CustosAC.Core.Services;

/// <summary>
/// Centralized service for fetching version from GitHub
/// </summary>
public class VersionService
{
    private const string GitHubRepo = "dybeky/custosAC";
    private static volatile string? _cachedVersion;
    private static volatile string? _cachedDate;
    private static volatile string? _lastError;
    private static volatile bool _isLoaded;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public string Version => _cachedVersion ?? "...";
    public string ReleaseDate => _cachedDate ?? "";
    public string? LastError => _lastError;
    public bool IsLoaded => _isLoaded;

    public async Task LoadVersionAsync()
    {
        if (_isLoaded) return;

        await _semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_isLoaded) return;

            try
            {
                var client = HttpClientService.Instance;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                var url = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";
                var response = await client.GetAsync(url, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    // Use "name" field for version (e.g., "2.0.0"), fallback to "tag_name"
                    _cachedVersion = doc.RootElement.GetProperty("name").GetString()
                        ?? doc.RootElement.GetProperty("tag_name").GetString()
                        ?? "N/A";

                    var publishedAt = doc.RootElement.GetProperty("published_at").GetString();
                    if (DateTime.TryParse(publishedAt, out var date))
                    {
                        _cachedDate = date.ToString("dd.MM.yyyy");
                    }
                    _lastError = null;
                }
                else
                {
                    _lastError = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                    _cachedVersion = LocalizationService.Instance.Strings.Error + $" ({(int)response.StatusCode})";
                }
            }
            catch (HttpRequestException ex)
            {
                _lastError = $"Network: {ex.Message}";
                _cachedVersion = LocalizationService.Instance.Strings.NetworkUnavailable;
            }
            catch (OperationCanceledException)
            {
                _lastError = "Timeout";
                _cachedVersion = LocalizationService.Instance.Strings.Timeout;
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                _cachedVersion = LocalizationService.Instance.Strings.Error;
            }
            finally
            {
                _isLoaded = true;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public static void Reset()
    {
        _cachedVersion = null;
        _cachedDate = null;
        _lastError = null;
        _isLoaded = false;
    }
}
