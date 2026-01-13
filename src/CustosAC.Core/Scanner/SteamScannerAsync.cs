using CustosAC.Core.Abstractions;
using CustosAC.Core.Configuration;
using CustosAC.Core.Models;
using CustosAC.Core.Services;

namespace CustosAC.Core.Scanner;

/// <summary>
/// Steam accounts scanner with robust VDF parsing and validation
/// </summary>
public class SteamScannerAsync : BaseScannerAsync
{
    private readonly PathSettings _pathSettings;
    private readonly LogService? _logService;
    private readonly EnhancedLogService? _enhancedLog;
    private readonly VdfParser _vdfParser;

    public override string Name => "Steam Scanner";
    public override string Description => "Parsing Steam accounts from loginusers.vdf";

    public SteamScannerAsync(
        KeywordMatcherService keywordMatcher,
        IUIService uiService,
        ScanSettings scanSettings,
        PathSettings pathSettings,
        LogService? logService = null,
        EnhancedLogService? enhancedLog = null,
        VdfParser? vdfParser = null)
        : base(keywordMatcher, uiService, scanSettings)
    {
        _pathSettings = pathSettings;
        _logService = logService;
        _enhancedLog = enhancedLog;
        _vdfParser = vdfParser ?? new VdfParser(enhancedLog);
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

            _enhancedLog?.LogDebug(EnhancedLogService.LogCategory.Scanner,
                $"Parsing VDF file: {vdfPath}", Name);

            // Use robust VDF parser
            var parsedAccounts = _vdfParser.ParseSteamAccounts(content);

            // Validate each account
            foreach (var account in parsedAccounts)
            {
                var (isValid, errors) = VdfParser.ValidateAccount(account);

                if (isValid)
                {
                    accounts.Add(account);
                    _enhancedLog?.LogDebug(EnhancedLogService.LogCategory.Validation,
                        $"Valid Steam account: {account.AccountName} ({account.SteamId})", Name);
                }
                else
                {
                    _enhancedLog?.LogWarning(EnhancedLogService.LogCategory.Validation,
                        $"Invalid Steam account skipped: {string.Join(", ", errors)}", Name);
                    _logService?.LogWarning($"Invalid Steam account: {string.Join(", ", errors)}");
                }
            }

            _enhancedLog?.LogInfo(EnhancedLogService.LogCategory.Scanner,
                $"Successfully parsed {accounts.Count} valid Steam account(s)", Name);
        }
        catch (VdfParser.VdfParseException ex)
        {
            var errorMsg = $"VDF parsing error at line {ex.LineNumber}: {ex.Message}";
            _enhancedLog?.LogError(EnhancedLogService.LogCategory.Scanner, errorMsg, ex, Name);
            _logService?.LogWarning(errorMsg);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Unexpected error parsing VDF: {ex.Message}";
            _enhancedLog?.LogError(EnhancedLogService.LogCategory.Scanner, errorMsg, ex, Name);
            _logService?.LogWarning(errorMsg);
        }

        return accounts;
    }
}
