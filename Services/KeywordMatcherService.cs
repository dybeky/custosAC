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

    public KeywordMatcherService(IOptions<KeywordSettings> settings)
    {
        _keywords = settings.Value.Patterns;
    }

    public bool ContainsKeyword(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        string textLower = text.ToLowerInvariant();
        return _keywords.Any(keyword => textLower.Contains(keyword.ToLowerInvariant()));
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
