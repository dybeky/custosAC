using CustosAC.Core.Abstractions;
using CustosAC.Core.Configuration;
using CustosAC.Core.Models;
using CustosAC.Core.Services;

namespace CustosAC.Core.Scanner;

/// <summary>
/// Steam accounts scanner
/// </summary>
public class SteamScannerAsync : BaseScannerAsync
{
    private const string SteamIdPrefix = "\"7656";
    private readonly PathSettings _pathSettings;
    private readonly LogService? _logService;

    public override string Name => "Steam Scanner";
    public override string Description => "Parsing Steam accounts from loginusers.vdf";

    public SteamScannerAsync(
        KeywordMatcherService keywordMatcher,
        IUIService uiService,
        ScanSettings scanSettings,
        PathSettings pathSettings,
        LogService? logService = null)
        : base(keywordMatcher, uiService, scanSettings)
    {
        _pathSettings = pathSettings;
        _logService = logService;
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
        {
            var possiblePaths = GetSteamLoginUsersPaths();
            var vdfPath = possiblePaths.FirstOrDefault(File.Exists);

            if (vdfPath == null)
                return CreateErrorResult("loginusers.vdf file not found", startTime);

            var accounts = await Task.Run(() => ParseSteamAccounts(vdfPath), cancellationToken);
            var findings = accounts.Select(a => $"SteamID: {a.SteamId} | Account: {a.AccountName} | Name: {a.PersonaName}").ToList();

            return CreateSuccessResult(findings, startTime);
        }
        catch (OperationCanceledException)
        {
            return CreateErrorResult("Scan cancelled", startTime);
        }
        catch (Exception ex)
        {
            return CreateErrorResult(ex.Message, startTime);
        }
    }

    private List<string> GetSteamLoginUsersPaths()
    {
        var paths = new List<string>();
        var programFilesX86 = _pathSettings.Windows.ProgramFilesX86;
        var programFiles = _pathSettings.Windows.ProgramFiles;
        var relativePath = _pathSettings.Steam.LoginUsersRelativePath;

        paths.Add(Path.Combine(programFilesX86, relativePath));
        paths.Add(Path.Combine(programFiles, relativePath));

        foreach (var drive in _pathSettings.Steam.AdditionalDrives)
        {
            paths.Add(Path.Combine(drive, relativePath));
            paths.Add(Path.Combine(drive, "SteamLibrary", "config", "loginusers.vdf"));
        }

        return paths;
    }

    private List<SteamAccount> ParseSteamAccounts(string vdfPath)
    {
        var accounts = new List<SteamAccount>();

        try
        {
            var content = File.ReadAllText(vdfPath);
            var lines = content.Split('\n');
            SteamAccount? currentAccount = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith(SteamIdPrefix) && trimmedLine.Contains("\""))
                {
                    var parts = trimmedLine.Split('"');
                    if (parts.Length >= 2)
                        currentAccount = new SteamAccount { SteamId = parts[1] };
                }

                if (currentAccount != null)
                {
                    if (trimmedLine.Contains("\"AccountName\""))
                        currentAccount.AccountName = ExtractValue(trimmedLine) ?? "";

                    if (trimmedLine.Contains("\"PersonaName\""))
                        currentAccount.PersonaName = ExtractValue(trimmedLine) ?? "";

                    if (trimmedLine.Contains("\"RememberPassword\""))
                        currentAccount.RememberPassword = ExtractValue(trimmedLine) == "1";

                    if (trimmedLine == "}" && !string.IsNullOrEmpty(currentAccount.AccountName))
                    {
                        accounts.Add(currentAccount);
                        currentAccount = null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logService?.LogWarning($"VDF parsing error: {ex.Message}");
        }

        return accounts;
    }

    private static string? ExtractValue(string line)
    {
        var parts = line.Split('"');
        return parts.Length >= 4 ? parts[3] : null;
    }
}
