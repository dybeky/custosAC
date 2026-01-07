using CustosAC.Abstractions;
using CustosAC.Configuration;
using Microsoft.Extensions.Options;

namespace CustosAC.Services;

/// <summary>
/// Реализация сервиса сопоставления ключевых слов
/// </summary>
public class KeywordMatcherService : IKeywordMatcher
{
    private readonly string[] _keywords;
    private readonly string[] _keywordsLower;

    public KeywordMatcherService(IOptions<KeywordSettings> settings)
    {
        _keywords = settings.Value.Patterns;
        // Pre-compute lowercase versions ONCE for performance
        _keywordsLower = _keywords.Select(k => k.ToLowerInvariant()).ToArray();
    }

    public bool ContainsKeyword(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        string textLower = text.ToLowerInvariant();
        // Use pre-computed lowercase keywords for better performance
        return _keywordsLower.Any(keyword => textLower.Contains(keyword));
    }

    public IReadOnlyList<string> GetKeywords()
    {
        return _keywords;
    }

    public string GetKeywordsString()
    {
        return string.Join(" ", _keywords);
    }
}
