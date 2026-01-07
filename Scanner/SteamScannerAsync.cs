using System.Text.RegularExpressions;
using CustosAC.Abstractions;
using CustosAC.Configuration;
using CustosAC.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustosAC.Scanner;

/// <summary>
/// Async сканер Steam аккаунтов
/// </summary>
public class SteamScannerAsync : BaseScannerAsync
{
    private readonly PathSettings _pathSettings;

    public override string Name => "Steam Scanner";
    public override string Description => "Парсинг Steam аккаунтов из loginusers.vdf";

    public SteamScannerAsync(
        IFileSystemService fileSystem,
        IKeywordMatcher keywordMatcher,
        IConsoleUI consoleUI,
        ILogger<SteamScannerAsync> logger,
        IOptions<ScanSettings> scanSettings,
        IOptions<PathSettings> pathSettings)
        : base(fileSystem, keywordMatcher, consoleUI, logger, scanSettings)
    {
        _pathSettings = pathSettings.Value;
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        Logger.LogInformation("Starting Steam accounts scan");

        try
        {
            var possiblePaths = GetSteamLoginUsersPaths();
            var vdfPath = FindFirstExisting(possiblePaths);

            if (vdfPath == null)
            {
                Logger.LogWarning("loginusers.vdf not found");
                return CreateErrorResult("Файл loginusers.vdf не найден", startTime);
            }

            Logger.LogInformation("Found loginusers.vdf: {Path}", vdfPath);

            var accounts = await Task.Run(() => ParseSteamAccounts(vdfPath), cancellationToken);

            var findings = accounts.Select(a => $"SteamID: {a.SteamId} | Account: {a.AccountName} | Name: {a.PersonaName}").ToList();

            Logger.LogInformation("Steam scan completed. Found {Count} accounts", accounts.Count);

            return CreateSuccessResult(findings, startTime);
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Steam scan was cancelled");
            return CreateErrorResult("Scan cancelled", startTime);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Steam scan failed");
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

    private string? FindFirstExisting(List<string> paths)
    {
        foreach (var path in paths)
        {
            if (FileSystem.FileExists(path))
            {
                return path;
            }
        }
        return null;
    }

    private List<SteamAccount> ParseSteamAccounts(string vdfPath)
    {
        var accounts = new List<SteamAccount>();

        try
        {
            var content = FileSystem.ReadAllText(vdfPath);
            var lines = content.Split('\n');

            SteamAccount? currentAccount = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Ищем SteamID (начинается с "7656...")
                if (trimmedLine.StartsWith("\"7656") && trimmedLine.Contains("\""))
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
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error parsing loginusers.vdf");
        }

        return accounts;
    }

    private static string? ExtractValue(string line)
    {
        var parts = line.Split('"');
        return parts.Length >= 4 ? parts[3] : null;
    }
}
