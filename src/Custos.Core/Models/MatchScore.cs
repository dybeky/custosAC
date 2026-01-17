namespace Custos.Core.Models;

/// <summary>
/// Represents confidence score for threat detection with detailed reasoning
/// </summary>
public class MatchScore
{
    /// <summary>Confidence score from 0 to 100</summary>
    public int ConfidenceScore { get; set; }

    /// <summary>List of reasons contributing to the score</summary>
    public List<string> Reasons { get; set; } = new();

    /// <summary>Severity level of the threat</summary>
    public SeverityLevel Severity { get; set; } = SeverityLevel.Low;

    /// <summary>Whether the item is whitelisted</summary>
    public bool IsWhitelisted { get; set; }

    /// <summary>Whether the item has a valid digital signature</summary>
    public bool HasValidSignature { get; set; }

    /// <summary>File path being evaluated</summary>
    public string? FilePath { get; set; }

    /// <summary>File name being evaluated</summary>
    public string? FileName { get; set; }

    /// <summary>Matched keywords if any</summary>
    public List<string> MatchedKeywords { get; set; } = new();

    /// <summary>Whether this should be reported as suspicious</summary>
    public bool ShouldReport => ConfidenceScore >= 50 && !IsWhitelisted;

    /// <summary>Whether this is high confidence threat</summary>
    public bool IsHighConfidence => ConfidenceScore >= 80;

    /// <summary>
    /// Adds a scoring factor to the confidence score
    /// </summary>
    public void AddFactor(string reason, int points)
    {
        ConfidenceScore += points;
        if (!string.IsNullOrEmpty(reason))
        {
            Reasons.Add($"{reason} ({points:+0;-0;0} points)");
        }
    }

    /// <summary>
    /// Gets a summary description of the match
    /// </summary>
    public string GetSummary()
    {
        var summary = $"Confidence: {ConfidenceScore}% | Severity: {Severity}";

        if (IsWhitelisted)
            summary += " | WHITELISTED";

        if (HasValidSignature)
            summary += " | Signed";

        if (MatchedKeywords.Count > 0)
            summary += $" | Keywords: {string.Join(", ", MatchedKeywords)}";

        return summary;
    }

    /// <summary>
    /// Gets detailed breakdown of the score
    /// </summary>
    public string GetDetailedBreakdown()
    {
        var breakdown = new System.Text.StringBuilder();
        breakdown.AppendLine($"File: {FileName ?? "Unknown"}");
        breakdown.AppendLine($"Path: {FilePath ?? "Unknown"}");
        breakdown.AppendLine($"Confidence Score: {ConfidenceScore}%");
        breakdown.AppendLine($"Severity: {Severity}");
        breakdown.AppendLine($"Should Report: {ShouldReport}");

        if (MatchedKeywords.Count > 0)
        {
            breakdown.AppendLine($"Matched Keywords: {string.Join(", ", MatchedKeywords)}");
        }

        breakdown.AppendLine("\nScoring Breakdown:");
        foreach (var reason in Reasons)
        {
            breakdown.AppendLine($"  - {reason}");
        }

        if (IsWhitelisted)
        {
            breakdown.AppendLine("\n⚠ Item is WHITELISTED and will not be reported");
        }

        return breakdown.ToString();
    }
}

/// <summary>
/// Severity levels for threat classification
/// </summary>
public enum SeverityLevel
{
    /// <summary>Low severity - unlikely to be a threat</summary>
    Low = 0,

    /// <summary>Medium severity - potentially suspicious</summary>
    Medium = 1,

    /// <summary>High severity - likely a threat</summary>
    High = 2,

    /// <summary>Critical severity - confirmed threat</summary>
    Critical = 3
}

/// <summary>
/// Factors that contribute to confidence scoring
/// </summary>
public enum SuspicionFactor
{
    /// <summary>Keyword match in file name</summary>
    KeywordMatch,

    /// <summary>Known cheat hash match</summary>
    KnownHash,

    /// <summary>Suspicious file name pattern</summary>
    SuspiciousName,

    /// <summary>Hidden file attribute</summary>
    HiddenAttribute,

    /// <summary>Located in high-risk location</summary>
    HighRiskLocation,

    /// <summary>Suspicious file size</summary>
    SuspiciousSize,

    /// <summary>File has valid digital signature (negative points)</summary>
    ValidSignature,

    /// <summary>Path is whitelisted (negative points)</summary>
    WhitelistedPath,

    /// <summary>Recent file creation/modification</summary>
    RecentlyModified,

    /// <summary>Executable in temp directory</summary>
    TempExecutable,

    /// <summary>Obfuscated or packed binary</summary>
    Obfuscated,

    /// <summary>Suspicious registry entry</summary>
    RegistryEntry,

    /// <summary>Process injection detected</summary>
    ProcessInjection,

    /// <summary>Network connection to known cheat server</summary>
    SuspiciousConnection
}
