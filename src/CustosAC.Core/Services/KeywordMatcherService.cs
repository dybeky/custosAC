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

        try
        {
            // Normalize path for comparison
            var normalizedPath = Path.GetFullPath(path).ToLowerInvariant();

            // Exact match check
            if (WhitelistedPaths.Contains(normalizedPath))
                return true;

            // Subdirectory check with proper separator
            foreach (var whitePath in WhitelistedPaths)
            {
                try
                {
                    var normalizedWhitePath = Path.GetFullPath(whitePath).ToLowerInvariant();

                    // Ensure it's a subdirectory, not just a prefix match
                    if (normalizedPath.StartsWith(normalizedWhitePath + Path.DirectorySeparatorChar) ||
                        normalizedPath.StartsWith(normalizedWhitePath + Path.AltDirectorySeparatorChar))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Skip invalid paths
                }
            }

            // Path parts check
            foreach (var part in WhitelistedPathParts)
            {
                if (normalizedPath.Contains(part.ToLowerInvariant()))
                    return true;
            }
        }
        catch
        {
            // If path normalization fails, fall back to simple check
            foreach (var whitePath in WhitelistedPaths)
            {
                if (path.StartsWith(whitePath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
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

    /// <summary>
    /// Evaluates a file/path and returns confidence score with detailed reasoning
    /// </summary>
    public Models.MatchScore EvaluateMatch(string filePath, string? fileName = null)
    {
        fileName ??= Path.GetFileName(filePath);

        var score = new Models.MatchScore
        {
            FilePath = filePath,
            FileName = fileName
        };

        // Check whitelist first (high negative score)
        if (IsWhitelisted(filePath))
        {
            score.IsWhitelisted = true;
            score.AddFactor("Whitelisted path", -50);
            score.Severity = Models.SeverityLevel.Low;
            return score;
        }

        // Check for keyword matches (high positive score)
        var keyword = FindKeyword(fileName);
        if (keyword != null)
        {
            score.MatchedKeywords.Add(keyword);
            score.AddFactor($"Keyword match: {keyword}", 30);
        }

        // Check whitelisted name
        if (IsWhitelistedName(fileName))
        {
            score.AddFactor("Whitelisted name", -15);
        }

        // Determine severity based on score
        if (score.ConfidenceScore >= 80)
            score.Severity = Models.SeverityLevel.Critical;
        else if (score.ConfidenceScore >= 60)
            score.Severity = Models.SeverityLevel.High;
        else if (score.ConfidenceScore >= 30)
            score.Severity = Models.SeverityLevel.Medium;
        else
            score.Severity = Models.SeverityLevel.Low;

        return score;
    }

    /// <summary>
    /// Evaluates with additional context like file size, attributes, location
    /// </summary>
    public Models.MatchScore EvaluateMatchWithContext(
        string filePath,
        FileInfo? fileInfo = null,
        bool hasValidSignature = false)
    {
        var score = EvaluateMatch(filePath);

        // Digital signature check (strong negative indicator)
        if (hasValidSignature)
        {
            score.HasValidSignature = true;
            score.AddFactor("Valid digital signature", -25);
        }

        if (fileInfo != null)
        {
            // Check file attributes
            if ((fileInfo.Attributes & FileAttributes.Hidden) != 0)
            {
                score.AddFactor("Hidden file", 15);
            }

            // Check file size for executables
            var ext = fileInfo.Extension.ToLowerInvariant();
            if (ext == ".exe" || ext == ".dll")
            {
                if (fileInfo.Length < 10 * 1024) // < 10KB
                {
                    score.AddFactor("Suspiciously small executable", 10);
                }
                else if (fileInfo.Length > 100 * 1024 * 1024) // > 100MB
                {
                    score.AddFactor("Unusually large executable", 5);
                }
            }

            // Check location risk
            var locationScore = EvaluateLocationRisk(filePath);
            if (locationScore > 0)
            {
                score.AddFactor($"High-risk location", locationScore);
            }

            // Check if recently created/modified
            if ((DateTime.Now - fileInfo.LastWriteTime).TotalDays < 1)
            {
                score.AddFactor("Recently modified (< 24h)", 5);
            }
        }

        // Re-evaluate severity
        if (score.ConfidenceScore >= 80)
            score.Severity = Models.SeverityLevel.Critical;
        else if (score.ConfidenceScore >= 60)
            score.Severity = Models.SeverityLevel.High;
        else if (score.ConfidenceScore >= 30)
            score.Severity = Models.SeverityLevel.Medium;
        else
            score.Severity = Models.SeverityLevel.Low;

        return score;
    }

    /// <summary>
    /// Evaluates location risk level
    /// </summary>
    private static int EvaluateLocationRisk(string path)
    {
        var normalizedPath = path.ToLowerInvariant();

        // High risk locations
        if (normalizedPath.Contains("\\temp\\") ||
            normalizedPath.Contains("\\tmp\\") ||
            normalizedPath.Contains("\\downloads\\") ||
            normalizedPath.Contains("\\desktop\\"))
        {
            return 15;
        }

        // Medium risk locations
        if (normalizedPath.Contains("\\appdata\\local\\") ||
            normalizedPath.Contains("\\documents\\"))
        {
            return 8;
        }

        // Low risk locations
        if (normalizedPath.Contains("\\program files\\") ||
            normalizedPath.Contains("\\windows\\system32\\"))
        {
            return 0;
        }

        return 5; // Default moderate risk
    }
}
