using CustosAC.Core.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace CustosAC.Core.Services;

/// <summary>
/// Robust parser for Valve Data Format (VDF) files
/// </summary>
public class VdfParser
{
    private readonly EnhancedLogService? _logService;

    public class VdfParseException : Exception
    {
        public int LineNumber { get; set; }
        public VdfParseException(string message, int lineNumber) : base(message)
        {
            LineNumber = lineNumber;
        }
    }

    public VdfParser(EnhancedLogService? logService = null)
    {
        _logService = logService;
    }

    /// <summary>
    /// Parses Steam loginusers.vdf file and extracts account information
    /// </summary>
    public List<SteamAccount> ParseSteamAccounts(string vdfContent)
    {
        var accounts = new List<SteamAccount>();
        var lines = vdfContent.Split('\n');
        var currentAccount = (SteamAccount?)null;
        var lineNumber = 0;
        var inUsersSection = false;

        try
        {
            foreach (var rawLine in lines)
            {
                lineNumber++;
                var line = rawLine.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    continue;

                // Remove UTF-8 BOM if present
                if (lineNumber == 1 && line.Length > 0 && line[0] == '\uFEFF')
                    line = line.Substring(1).Trim();

                // Check for "users" section
                if (line.Contains("\"users\"", StringComparison.OrdinalIgnoreCase))
                {
                    inUsersSection = true;
                    continue;
                }

                // Skip lines before users section
                if (!inUsersSection)
                    continue;

                // Try to parse as a Steam ID (starts with "7656" and is quoted)
                if (line.StartsWith("\"7656"))
                {
                    var steamId = ExtractQuotedValue(line, lineNumber);
                    if (ValidateSteamId(steamId))
                    {
                        // Save previous account if exists
                        if (currentAccount != null && !string.IsNullOrEmpty(currentAccount.AccountName))
                        {
                            accounts.Add(currentAccount);
                        }

                        currentAccount = new SteamAccount
                        {
                            SteamId = steamId
                        };
                    }
                    else
                    {
                        _logService?.LogWarning(EnhancedLogService.LogCategory.Validation,
                            $"Invalid SteamID format at line {lineNumber}: {steamId}");
                    }
                }
                else if (currentAccount != null)
                {
                    // Parse key-value pairs within account
                    if (line.Contains("\"AccountName\"", StringComparison.OrdinalIgnoreCase))
                    {
                        currentAccount.AccountName = ExtractKeyValue(line, lineNumber);
                    }
                    else if (line.Contains("\"PersonaName\"", StringComparison.OrdinalIgnoreCase))
                    {
                        currentAccount.PersonaName = ExtractKeyValue(line, lineNumber);
                    }
                    else if (line.Contains("\"RememberPassword\"", StringComparison.OrdinalIgnoreCase))
                    {
                        var value = ExtractKeyValue(line, lineNumber);
                        currentAccount.RememberPassword = value == "1";
                    }
                    else if (line.Contains("\"Timestamp\"", StringComparison.OrdinalIgnoreCase))
                    {
                        var value = ExtractKeyValue(line, lineNumber);
                        if (long.TryParse(value, out var timestamp))
                        {
                            currentAccount.Timestamp = timestamp;
                        }
                    }
                    else if (line == "}")
                    {
                        // End of account object
                        if (currentAccount != null && !string.IsNullOrEmpty(currentAccount.AccountName))
                        {
                            accounts.Add(currentAccount);
                        }
                        currentAccount = null;
                    }
                }
            }

            // Add last account if not already added
            if (currentAccount != null && !string.IsNullOrEmpty(currentAccount.AccountName))
            {
                accounts.Add(currentAccount);
            }

            _logService?.LogInfo(EnhancedLogService.LogCategory.Validation,
                $"Successfully parsed {accounts.Count} Steam account(s)");

            return accounts;
        }
        catch (Exception ex)
        {
            throw new VdfParseException($"Parse error: {ex.Message}", lineNumber);
        }
    }

    /// <summary>
    /// Extracts a quoted value from a line (for Steam ID)
    /// </summary>
    private string ExtractQuotedValue(string line, int lineNumber)
    {
        // Match pattern: "value"
        var match = Regex.Match(line, "\"([^\"]+)\"");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        throw new VdfParseException($"Failed to extract quoted value from: {line}", lineNumber);
    }

    /// <summary>
    /// Extracts value from a key-value pair
    /// </summary>
    private string ExtractKeyValue(string line, int lineNumber)
    {
        // Match pattern: "key" "value"
        var matches = Regex.Matches(line, "\"([^\"]+)\"");
        if (matches.Count >= 2)
        {
            return matches[1].Groups[1].Value;
        }

        // Try alternative format: "key"\t\t"value"
        var parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
        {
            return parts[1];
        }

        _logService?.LogWarning(EnhancedLogService.LogCategory.Validation,
            $"Failed to extract key-value from line {lineNumber}: {line}");

        return string.Empty;
    }

    /// <summary>
    /// Validates Steam ID format (should start with 7656 and be 17 digits)
    /// </summary>
    private bool ValidateSteamId(string steamId)
    {
        if (string.IsNullOrWhiteSpace(steamId))
            return false;

        // Steam ID 64 format: starts with 7656 and is exactly 17 digits
        if (!steamId.StartsWith("7656"))
            return false;

        if (steamId.Length != 17)
            return false;

        return steamId.All(char.IsDigit);
    }

    /// <summary>
    /// Validates Steam account has required fields
    /// </summary>
    public static (bool IsValid, List<string> Errors) ValidateAccount(SteamAccount account)
    {
        var errors = new List<string>();

        // Validate SteamID
        if (string.IsNullOrWhiteSpace(account.SteamId))
        {
            errors.Add("SteamID is empty");
        }
        else if (!account.SteamId.StartsWith("7656") || account.SteamId.Length != 17)
        {
            errors.Add($"Invalid SteamID format: {account.SteamId}");
        }
        else if (!account.SteamId.All(char.IsDigit))
        {
            errors.Add($"SteamID contains non-digit characters: {account.SteamId}");
        }

        // Validate AccountName
        if (string.IsNullOrWhiteSpace(account.AccountName))
        {
            errors.Add("AccountName is empty");
        }
        else if (account.AccountName.Length > 64)
        {
            errors.Add($"AccountName too long: {account.AccountName.Length} characters");
        }
        else if (!IsValidAccountName(account.AccountName))
        {
            errors.Add($"AccountName contains invalid characters: {account.AccountName}");
        }

        // Validate PersonaName (optional but check if present)
        if (!string.IsNullOrWhiteSpace(account.PersonaName))
        {
            if (account.PersonaName.Length > 128)
            {
                errors.Add($"PersonaName too long: {account.PersonaName.Length} characters");
            }
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Checks if account name contains only valid characters
    /// </summary>
    private static bool IsValidAccountName(string accountName)
    {
        // Account names should be alphanumeric with limited special characters
        // Allow: letters, digits, underscore, hyphen, period
        return Regex.IsMatch(accountName, @"^[a-zA-Z0-9_\-\.]+$");
    }

    /// <summary>
    /// Parses VDF content into a generic dictionary structure
    /// </summary>
    public Dictionary<string, object> ParseGenericVdf(string vdfContent)
    {
        var result = new Dictionary<string, object>();
        var stack = new Stack<Dictionary<string, object>>();
        stack.Push(result);

        var lines = vdfContent.Split('\n');
        var lineNumber = 0;

        foreach (var rawLine in lines)
        {
            lineNumber++;
            var line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                continue;

            if (line == "{")
            {
                continue;
            }
            else if (line == "}")
            {
                if (stack.Count > 1)
                    stack.Pop();
                continue;
            }

            // Parse key-value pair
            var matches = Regex.Matches(line, "\"([^\"]+)\"");
            if (matches.Count == 2)
            {
                var key = matches[0].Groups[1].Value;
                var value = matches[1].Groups[1].Value;

                var current = stack.Peek();
                current[key] = value;
            }
            else if (matches.Count == 1)
            {
                // Just a key, expecting an object
                var key = matches[0].Groups[1].Value;
                var newDict = new Dictionary<string, object>();

                var current = stack.Peek();
                current[key] = newDict;
                stack.Push(newDict);
            }
        }

        return result;
    }
}
