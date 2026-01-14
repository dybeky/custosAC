using CustosAC.Core.Abstractions;
using CustosAC.Core.Configuration;
using CustosAC.Core.Models;
using CustosAC.Core.Services;

namespace CustosAC.Core.Scanner;

/// <summary>
/// Recently created/modified executable files scanner
/// </summary>
public class RecentFileScannerAsync : BaseScannerAsync
{
    private readonly int _daysToCheck;

    public override string Name => "Recent Files Scanner";
    public override string Description => "Scanning recently created files";

    public RecentFileScannerAsync(
        KeywordMatcherService keywordMatcher,
        IUIService uiService,
        ScanSettings scanSettings,
        int daysToCheck = 7)
        : base(keywordMatcher, uiService, scanSettings)
    {
        _daysToCheck = daysToCheck;
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
        {
            var findings = await Task.Run(() =>
            {
                var results = new List<string>();
                var cutoffDate = DateTime.Now.AddDays(-_daysToCheck);
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                var suspiciousPaths = new[]
                {
                    Path.Combine(userProfile, "Downloads"),
                    Path.Combine(userProfile, "Desktop"),
                    Path.Combine(userProfile, "Documents"),
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"C:\Users\Public",
                    @"C:\ProgramData"
                };

                foreach (var basePath in suspiciousPaths)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    if (!Directory.Exists(basePath)) continue;

                    ScanDirectoryForRecentFiles(basePath, cutoffDate, results, 3, cancellationToken, 0);
                }

                return results;
            }, cancellationToken);

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

    private void ScanDirectoryForRecentFiles(string path, DateTime cutoffDate, List<string> results,
        int maxDepth, CancellationToken cancellationToken, int currentDepth)
    {
        if (currentDepth > maxDepth || cancellationToken.IsCancellationRequested) return;

        try
        {
            var enumOptions = new EnumerationOptions
            {
                AttributesToSkip = FileAttributes.System,
                IgnoreInaccessible = true,
                RecurseSubdirectories = false
            };

            foreach (var file in Directory.EnumerateFiles(path, "*.*", enumOptions))
            {
                if (cancellationToken.IsCancellationRequested) return;

                try
                {
                    var fileName = Path.GetFileName(file);

                    // Search by keywords only - no extension filtering
                    if (!KeywordMatcher.ContainsKeyword(fileName)) continue;

                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime >= cutoffDate || fileInfo.LastWriteTime >= cutoffDate)
                    {
                        var suspicion = AnalyzeSuspicion(file, fileInfo);
                        results.Add($"[RISK:{suspicion.Level}] {file} | Created: {fileInfo.CreationTime:dd.MM.yyyy HH:mm} | {suspicion.Reason}");
                    }
                }
                catch { }
            }

            foreach (var dir in Directory.EnumerateDirectories(path, "*", enumOptions))
            {
                if (cancellationToken.IsCancellationRequested) return;
                try
                {
                    var dirName = Path.GetFileName(dir).ToLowerInvariant();
                    if (dirName.StartsWith("$") || dirName == "windows") continue;
                    ScanDirectoryForRecentFiles(dir, cutoffDate, results, maxDepth, cancellationToken, currentDepth + 1);
                }
                catch { }
            }
        }
        catch { }
    }

    private (int Level, string Reason) AnalyzeSuspicion(string filePath, FileInfo fileInfo)
    {
        var reasons = new List<string>();
        int level = 0;

        if (KeywordMatcher.ContainsKeyword(fileInfo.Name)) { level += 3; reasons.Add("Keyword"); }
        if (filePath.ToLowerInvariant().Contains(@"\temp\")) { level += 2; reasons.Add("Temp folder"); }
        if ((fileInfo.Attributes & FileAttributes.Hidden) != 0) { level += 2; reasons.Add("Hidden"); }
        if (fileInfo.Length < 10 * 1024) { level += 1; reasons.Add("Small file"); }

        return (level, string.Join(", ", reasons));
    }

}
