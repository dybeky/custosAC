using System.Net.Http;
using System.Text.Json;

namespace CustosAC.Core.Services;

/// <summary>
/// Centralized service for fetching version from GitHub
/// </summary>
public class VersionService
{
    private const string GitHubRepo = "dybeky/custosAC";
    private static string? _cachedVersion;
    private static string? _cachedDate;
    private static string? _lastError;
    private static bool _isLoaded;
    private static readonly object _lock = new();

    public string Version => _cachedVersion ?? "...";
    public string ReleaseDate => _cachedDate ?? "";
    public string? LastError => _lastError;
    public bool IsLoaded => _isLoaded;

    public async Task LoadVersionAsync()
    {
        if (_isLoaded) return;

        lock (_lock)
        {
            if (_isLoaded) return;
        }

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            client.Timeout = TimeSpan.FromSeconds(10);

            var url = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";
            var response = await client.GetAsync(url);

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
                _cachedVersion = $"Ошибка ({(int)response.StatusCode})";
            }
        }
        catch (HttpRequestException ex)
        {
            _lastError = $"Network: {ex.Message}";
            _cachedVersion = "Сеть недоступна";
        }
        catch (TaskCanceledException)
        {
            _lastError = "Timeout";
            _cachedVersion = "Таймаут";
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            _cachedVersion = "Ошибка";
        }
        finally
        {
            _isLoaded = true;
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
