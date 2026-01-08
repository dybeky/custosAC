using CustosAC.Configuration;
using CustosAC.Models;
using CustosAC.Services;

namespace CustosAC.Scanner;

/// <summary>
/// Async сканер Steam аккаунтов
/// </summary>
public class SteamScannerAsync : BaseScannerAsync
{
    private const string SteamIdPrefix = "\"7656";
    private readonly PathSettings _pathSettings;

    public override string Name => "Steam Scanner";
    public override string Description => "Парсинг Steam аккаунтов из loginusers.vdf";

    public SteamScannerAsync(
        KeywordMatcherService keywordMatcher,
        ConsoleUIService consoleUI,
        ScanSettings scanSettings,
        PathSettings pathSettings)
        : base(keywordMatcher, consoleUI, scanSettings)
    {
        _pathSettings = pathSettings;
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
        {
            var possiblePaths = GetSteamLoginUsersPaths();
            var vdfPath = FindFirstExisting(possiblePaths);

            if (vdfPath == null)
            {
                return CreateErrorResult("Файл loginusers.vdf не найден", startTime);
            }

            var accounts = await Task.Run(() => ParseSteamAccounts(vdfPath), cancellationToken);

            var findings = accounts.Select(a => $"SteamID: {a.SteamId} | Account: {a.AccountName} | Name: {a.PersonaName}").ToList();

            return CreateSuccessResult(findings, startTime);
        }
        catch (OperationCanceledException)
        {
            return CreateErrorResult("Сканирование отменено", startTime);
        }
        catch (Exception ex)
        {
            return CreateErrorResult(ex.Message, startTime);
        }
    }

    private List<string> GetSteamLoginUsersPaths()
    {
        var paths = new List<string>();

        // Основные пути Steam
        var programFilesX86 = _pathSettings.Windows.ProgramFilesX86;
        var programFiles = _pathSettings.Windows.ProgramFiles;
        var relativePath = _pathSettings.Steam.LoginUsersRelativePath;

        // C:\Program Files (x86)\Steam
        paths.Add(Path.Combine(programFilesX86, relativePath));
        paths.Add(Path.Combine(programFiles, relativePath));

        // Дополнительные диски
        foreach (var drive in _pathSettings.Steam.AdditionalDrives)
        {
            paths.Add(Path.Combine(drive, relativePath));
            paths.Add(Path.Combine(drive, "SteamLibrary", "config", "loginusers.vdf"));
            paths.Add(Path.Combine(drive, "Games", "Steam", "config", "loginusers.vdf"));
        }

        return paths;
    }

    private static string? FindFirstExisting(List<string> paths)
        => paths.FirstOrDefault(File.Exists);

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

                // Ищем SteamID (начинается с "7656...")
                if (trimmedLine.StartsWith(SteamIdPrefix) && trimmedLine.Contains("\""))
                {
                    var parts = trimmedLine.Split('"');
                    if (parts.Length >= 2)
                    {
                        currentAccount = new SteamAccount { SteamId = parts[1] };
                    }
                }

                if (currentAccount != null)
                {
                    // Ищем AccountName
                    if (trimmedLine.Contains("\"AccountName\""))
                    {
                        var value = ExtractValue(trimmedLine);
                        if (value != null)
                        {
                            currentAccount.AccountName = value;
                        }
                    }

                    // Ищем PersonaName
                    if (trimmedLine.Contains("\"PersonaName\""))
                    {
                        var value = ExtractValue(trimmedLine);
                        if (value != null)
                        {
                            currentAccount.PersonaName = value;
                        }
                    }

                    // Ищем RememberPassword
                    if (trimmedLine.Contains("\"RememberPassword\""))
                    {
                        var value = ExtractValue(trimmedLine);
                        currentAccount.RememberPassword = value == "1";
                    }

                    // Ищем Timestamp
                    if (trimmedLine.Contains("\"Timestamp\""))
                    {
                        var value = ExtractValue(trimmedLine);
                        if (value != null && long.TryParse(value, out var timestamp))
                        {
                            currentAccount.Timestamp = timestamp;
                        }
                    }

                    // Если блок аккаунта закрылся
                    if (trimmedLine == "}" && !string.IsNullOrEmpty(currentAccount.AccountName))
                    {
                        accounts.Add(currentAccount);
                        currentAccount = null;
                    }
                }
            }
        }
        catch (Exception)
        {
            // VDF file parsing error (malformed file, encoding issues) - return partial results
        }

        return accounts;
    }

    private static string? ExtractValue(string line)
    {
        var parts = line.Split('"');
        return parts.Length >= 4 ? parts[3] : null;
    }
}
