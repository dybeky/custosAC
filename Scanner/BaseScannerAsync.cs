using System.Collections.Concurrent;
using CustosAC.Abstractions;
using CustosAC.Configuration;
using CustosAC.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustosAC.Scanner;

/// <summary>
/// Базовый абстрактный класс для async сканеров
/// </summary>
public abstract class BaseScannerAsync : IScanner
{
    protected readonly IFileSystemService FileSystem;
    protected readonly IKeywordMatcher KeywordMatcher;
    protected readonly IConsoleUI ConsoleUI;
    protected readonly ILogger Logger;
    protected readonly ScanSettings ScanSettings;
    protected readonly SemaphoreSlim _scanSemaphore;

    public abstract string Name { get; }
    public abstract string Description { get; }

    protected BaseScannerAsync(
        IFileSystemService fileSystem,
        IKeywordMatcher keywordMatcher,
        IConsoleUI consoleUI,
        ILogger logger,
        IOptions<ScanSettings> scanSettings)
    {
        FileSystem = fileSystem;
        KeywordMatcher = keywordMatcher;
        ConsoleUI = consoleUI;
        Logger = logger;
        ScanSettings = scanSettings.Value;

        // Limit total concurrent I/O operations to prevent thread explosion
        _scanSemaphore = new SemaphoreSlim(
            ScanSettings.MaxDegreeOfParallelism,
            ScanSettings.MaxDegreeOfParallelism);
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

        if (!FileSystem.DirectoryExists(path))
        {
            Logger.LogWarning("Directory does not exist: {Path}", path);
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
            entries = FileSystem.EnumerateFileSystemEntries(path);
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
        await _scanSemaphore.WaitAsync(ct);
        try
        {
            var name = FileSystem.GetFileName(entry);

            if (FileSystem.IsDirectory(entry))
            {
                if (excludedDirs.Contains(name))
                    return;

                if (KeywordMatcher.ContainsKeyword(name))
                {
                    results.Add(entry);
                }

                await ScanFolderRecursiveAsync(entry, extensions, maxDepth, currentDepth + 1, results, ct);
            }
            else if (FileSystem.FileExists(entry))
            {
                if (KeywordMatcher.ContainsKeyword(name))
                {
                    if (extensions.Length == 0)
                    {
                        results.Add(entry);
                    }
                    else
                    {
                        var ext = FileSystem.GetExtension(entry).ToLowerInvariant();
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
        finally
        {
            _scanSemaphore.Release();
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
