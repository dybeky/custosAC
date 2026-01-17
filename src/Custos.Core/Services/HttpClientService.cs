using System.Net.Http;

namespace Custos.Core.Services;

/// <summary>
/// Shared HttpClient service to prevent socket exhaustion.
/// HttpClient is designed to be instantiated once and reused throughout the application lifecycle.
/// </summary>
public static class HttpClientService
{
    private static readonly Lazy<HttpClient> _instance = new(() =>
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        return client;
    });

    public static HttpClient Instance => _instance.Value;
}
