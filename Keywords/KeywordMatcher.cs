namespace CustosAC.Keywords;

public static class KeywordMatcher
{
    private static readonly string[] Keywords = new[]
    {
        "undead", "melony", "fecurity", "ancient", "hack", "cheat", "чит",
        "софт", "loader", "inject", "bypass", "overlay", "esp", "speedhack",
        "лоадер", "hwid", "medusa", "mason", "mas", "smg",
        "midnight", "fatality", "memesense"
    };

    public static bool ContainsKeyword(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        string nameLower = name.ToLower();
        return Keywords.Any(keyword => nameLower.Contains(keyword.ToLower()));
    }

    public static string GetKeywordsString()
    {
        return string.Join(" ", Keywords);
    }
}
