using CustosAC.Core.Configuration;

namespace CustosAC.Core.Services;

/// <summary>
/// Keyword Matcher Service - searches by exact cheat names from the list
/// </summary>
public class KeywordMatcherService
{
    private readonly string[] _patterns;
    private readonly string[] _patternsLower;
    private readonly HashSet<string> _exactMatch;

    private static readonly HashSet<string> WhitelistedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        @"C:\Windows",
        @"C:\Program Files\Windows",
        @"C:\Program Files (x86)\Microsoft",
        @"C:\Program Files\Common Files",
        @"C:\Program Files (x86)\Common Files",
        @"C:\ProgramData\Microsoft",
        @"C:\Users\Default",
        @"C:\$Recycle.Bin"
    };

    private static readonly HashSet<string> WhitelistedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "services", "svchost", "csrss", "lsass", "smss", "wininit",
        "winlogon", "dwm", "explorer", "taskhostw", "sihost",
        "RuntimeBroker", "SearchHost", "StartMenuExperienceHost",
        "ShellExperienceHost", "SystemSettings", "ApplicationFrameHost",
        "ctfmon", "fontdrvhost", "dllhost", "conhost", "cmd", "powershell",
        "ancient", "mason", "midnight", "response", "desktop", "system"
    };

    private static readonly string[] WhitelistedPathParts = new[]
    {
        @"\Microsoft\", @"\Windows\", @"\Microsoft.NET\", @"\WindowsApps\",
        @"\Common Files\", @"\Temp\", @"\tmp\", @"\Local\Temp\",
        @"\AppData\Local\Temp\", @"\Google\", @"\Mozilla\", @"\Adobe\",
        @"\NVIDIA\", @"\AMD\", @"\Intel\", @"\Discord\", @"\Spotify\",
        @"\Steam\steamapps\workshop\", @"\Steam\steamapps\common\",
        @"\Cache\", @"\GPUCache\", @"\Code Cache\", @"\ShaderCache\",
        @"\Crashpad\", @"\CrashDumps\", @"\node_modules\", @"\packages\",
        @"\NuGet\", @"\.nuget\"
    };

    public KeywordMatcherService(KeywordSettings settings)
    {
        _patterns = settings.Patterns ?? Array.Empty<string>();
        _patternsLower = _patterns.Select(k => k.ToLowerInvariant()).ToArray();

        var exactPatterns = settings.ExactMatch ?? Array.Empty<string>();
        _exactMatch = new HashSet<string>(
            exactPatterns.Select(e => e.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsWhitelisted(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        foreach (var whitePath in WhitelistedPaths)
        {
            if (path.StartsWith(whitePath, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var part in WhitelistedPathParts)
        {
            if (path.Contains(part, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool IsWhitelistedName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        var baseName = Path.GetFileNameWithoutExtension(name);
        return WhitelistedNames.Contains(baseName);
    }

    public bool ContainsKeyword(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        var textLower = text.ToLowerInvariant();
        var baseName = Path.GetFileNameWithoutExtension(textLower);

        if (_exactMatch.Contains(baseName))
            return true;

        foreach (var pattern in _patternsLower)
        {
            if (HasWordBoundaryMatch(textLower, pattern))
                return true;
        }

        return false;
    }

    private static bool HasWordBoundaryMatch(string text, string pattern)
    {
        int index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            bool leftBoundary = (index == 0) || !char.IsLetterOrDigit(text[index - 1]);
            int endIndex = index + pattern.Length;
            bool rightBoundary = (endIndex >= text.Length) || !char.IsLetterOrDigit(text[endIndex]);

            if (leftBoundary && rightBoundary)
                return true;

            index++;
        }
        return false;
    }

    public bool ContainsKeywordWithWhitelist(string text, string? path = null)
    {
        if (!string.IsNullOrEmpty(path) && IsWhitelisted(path))
            return false;

        if (IsWhitelistedName(text))
            return false;

        return ContainsKeyword(text);
    }

    public string? FindKeyword(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        var textLower = text.ToLowerInvariant();
        var baseName = Path.GetFileNameWithoutExtension(textLower);

        if (_exactMatch.Contains(baseName))
            return baseName;

        for (int i = 0; i < _patternsLower.Length; i++)
        {
            if (HasWordBoundaryMatch(textLower, _patternsLower[i]))
                return _patterns[i];
        }

        return null;
    }

    public IReadOnlyList<string> GetKeywords() => _patterns;
    public string GetKeywordsString() => string.Join(" ", _patterns);
}
