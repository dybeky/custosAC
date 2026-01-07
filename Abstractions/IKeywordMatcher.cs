namespace CustosAC.Abstractions;

/// <summary>
/// Интерфейс для сопоставления ключевых слов
/// </summary>
public interface IKeywordMatcher
{
    /// <summary>Проверить, содержит ли текст ключевое слово</summary>
    bool ContainsKeyword(string text);

    /// <summary>Получить список ключевых слов</summary>
    IReadOnlyList<string> GetKeywords();

    /// <summary>Получить ключевые слова в виде строки</summary>
    string GetKeywordsString();
}
