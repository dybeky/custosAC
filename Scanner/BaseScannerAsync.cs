using System.Collections.Concurrent;
using CustosAC.Abstractions;
using CustosAC.Configuration;
using CustosAC.Models;
using CustosAC.Services;

namespace CustosAC.Scanner;

/// <summary>
/// Базовый абстрактный класс для async сканеров
/// </summary>
public abstract class BaseScannerAsync : IScanner
{
    protected readonly KeywordMatcherService KeywordMatcher;
    protected readonly ConsoleUIService ConsoleUI;
    protected readonly ScanSettings ScanSettings;
    protected readonly SemaphoreSlim _scanSemaphore;

    public abstract string Name { get; }
    public abstract string Description { get; }

    protected BaseScannerAsync(
        KeywordMatcherService keywordMatcher,
        ConsoleUIService consoleUI,
        ScanSettings scanSettings)
    {
        KeywordMatcher = keywordMatcher;
        ConsoleUI = consoleUI;
        ScanSettings = scanSettings;

        // Limit total concurrent I/O operations to prevent thread explosion
        _scanSemaphore = new SemaphoreSlim(
            scanSettings.MaxDegreeOfParallelism,
            scanSettings.MaxDegreeOfParallelism);
    }

    public abstract Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default);

    public virtual async Task<ScanResult> ScanAsync(IProgress<ScanProgress>? progress, CancellationToken cancellationToken = default)
    {
        return await ScanAsync(cancellationToken);
    }

    /// <summary>
    /// Параллельное сканирование папки
    /// </summary>
    protected async Task<List<string>> ScanFolderParallelAsync(
        string path,
        string[] extensions,
        int maxDepth,
        CancellationToken ct = default)
    {
        var results = new ConcurrentBag<string>();

        if (!Directory.Exists(path))
        {
            return results.ToList();
        }

        await ScanFolderRecursiveAsync(path, extensions, maxDepth, 0, results, ct);

        return results.ToList();
    }

    private async Task ScanFolderRecursiveAsync(
        string path,
        string[] extensions,
        int maxDepth,
        int currentDepth,
        ConcurrentBag<string> results,
        CancellationToken ct)
    {
        if (currentDepth > maxDepth || ct.IsCancellationRequested)
            return;

        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(path);
        }
        catch (UnauthorizedAccessException)
        {
            // Expected: access denied to system folders (e.g., System Volume Information)
            return;
        }
        catch (IOException)
        {
            // Expected: folder is locked or in use
            return;
        }

        var excludedDirs = new HashSet<string>(ScanSettings.ExcludedDirectories, StringComparer.OrdinalIgnoreCase);

        if (ScanSettings.ParallelScanEnabled)
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = ScanSettings.MaxDegreeOfParallelism,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(entries, options, async (entry, token) =>
            {
                await ProcessEntryAsync(entry, extensions, maxDepth, currentDepth, results, excludedDirs, token);
            });
        }
        else
        {
            foreach (var entry in entries)
            {
                if (ct.IsCancellationRequested)
                    break;

                await ProcessEntryAsync(entry, extensions, maxDepth, currentDepth, results, excludedDirs, ct);
            }
        }
    }

    private async Task ProcessEntryAsync(
        string entry,
        string[] extensions,
        int maxDepth,
        int currentDepth,
        ConcurrentBag<string> results,
        HashSet<string> excludedDirs,
        CancellationToken ct)
    {
        try
        {
            var name = Path.GetFileName(entry);

            if (Directory.Exists(entry))
            {
                if (excludedDirs.Contains(name))
                    return;

                if (KeywordMatcher.ContainsKeyword(name))
                {
                    results.Add(entry);
                }

                // Only use semaphore for recursive directory scanning to prevent thread explosion
                await _scanSemaphore.WaitAsync(ct);
                try
                {
                    await ScanFolderRecursiveAsync(entry, extensions, maxDepth, currentDepth + 1, results, ct);
                }
                finally
                {
                    _scanSemaphore.Release();
                }
            }
            else if (File.Exists(entry))
            {
                if (KeywordMatcher.ContainsKeyword(name))
                {
                    if (extensions.Length == 0)
                    {
                        results.Add(entry);
                    }
                    else
                    {
                        var ext = Path.GetExtension(entry).ToLowerInvariant();
                        if (extensions.Contains(ext))
                        {
                            results.Add(entry);
                        }
                    }
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Expected: access denied to protected files (common during scan)
        }
        catch (IOException)
        {
            // Expected: file in use or locked by another process
        }
    }

    /// <summary>
    /// Создать результат успешного сканирования
    /// </summary>
    protected ScanResult CreateSuccessResult(List<string> findings, DateTime startTime)
    {
        return new ScanResult
        {
            ScannerName = Name,
            Success = true,
            Findings = findings,
            StartTime = startTime,
            EndTime = DateTime.Now
        };
    }

    /// <summary>
    /// Создать результат неуспешного сканирования
    /// </summary>
    protected ScanResult CreateErrorResult(string error, DateTime startTime)
    {
        return new ScanResult
        {
            ScannerName = Name,
            Success = false,
            Error = error,
            Findings = new List<string>(),
            StartTime = startTime,
            EndTime = DateTime.Now
        };
    }
}
