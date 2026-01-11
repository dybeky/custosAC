using System.Diagnostics;
using CustosAC.Core.Abstractions;
using CustosAC.Core.Configuration;
using CustosAC.Core.Models;
using CustosAC.Core.Services;

namespace CustosAC.Core.Scanner;

/// <summary>
/// Running processes scanner
/// </summary>
public class ProcessScannerAsync : BaseScannerAsync
{
    private readonly CheatHashDatabase _hashDatabase;
    private readonly LogService? _logService;

    private const int ProcessPathTimeoutMs = 2000;
    private const int SuspiciousBasePriorityThreshold = 8;

    public override string Name => "Process Scanner";
    public override string Description => "Scanning running processes";

    public ProcessScannerAsync(
        KeywordMatcherService keywordMatcher,
        IUIService uiService,
        ScanSettings scanSettings,
        CheatHashDatabase hashDatabase,
        LogService? logService = null)
        : base(keywordMatcher, uiService, scanSettings)
    {
        _hashDatabase = hashDatabase;
        _logService = logService;
    }

    public override async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        Process[]? allProcesses = null;

        try
        {
            var findings = await Task.Run(async () =>
            {
                var results = new List<string>();
                allProcesses = Process.GetProcesses();

                foreach (var process in allProcesses)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var processName = process.ProcessName;

                        if (KeywordMatcherService.IsWhitelistedName(processName)) continue;

                        string? processPath = await GetProcessPathSafeAsync(process);

                        if (!string.IsNullOrEmpty(processPath) && KeywordMatcherService.IsWhitelisted(processPath))
                            continue;

                        if (KeywordMatcher.ContainsKeywordWithWhitelist(processName, processPath))
                        {
                            results.Add(BuildProcessInfo(process, processPath, "Keyword match"));
                            continue;
                        }

                        if (CheatHashDatabase.IsSuspiciousFileName(processName))
                        {
                            results.Add(BuildProcessInfo(process, processPath, "Suspicious name"));
                            continue;
                        }

                        if (!string.IsNullOrEmpty(processPath))
                        {
                            if (KeywordMatcher.ContainsKeywordWithWhitelist(processPath))
                            {
                                results.Add(BuildProcessInfo(process, processPath, "Suspicious path"));
                                continue;
                            }

                            var hashResult = _hashDatabase.CheckFileHash(processPath);
                            if (hashResult.IsKnownCheat)
                            {
                                results.Add(BuildProcessInfo(process, processPath,
                                    $"KNOWN CHEAT: {hashResult.CheatName}"));
                                continue;
                            }
                        }
                    }
                    catch { }
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
        finally
        {
            if (allProcesses != null)
            {
                foreach (var p in allProcesses)
                {
                    try { p?.Dispose(); } catch { }
                }
            }
        }
    }

    private static string BuildProcessInfo(Process process, string? path, string reason)
    {
        var info = $"[{reason}] {process.ProcessName} (PID: {process.Id})";
        if (!string.IsNullOrEmpty(path)) info += $" | Path: {path}";
        try { info += $" | Started: {process.StartTime:dd.MM.yyyy HH:mm:ss}"; } catch { }
        return info;
    }

    private static async Task<string?> GetProcessPathSafeAsync(Process process)
    {
        try
        {
            var getPathTask = Task.Run(() =>
            {
                try { return process.MainModule?.FileName; }
                catch { return null; }
            });

            var completedTask = await Task.WhenAny(getPathTask, Task.Delay(ProcessPathTimeoutMs));
            return completedTask == getPathTask ? await getPathTask : null;
        }
        catch { return null; }
    }
}
