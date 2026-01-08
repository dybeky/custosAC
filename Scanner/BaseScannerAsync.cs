using CustosAC.Abstractions;
using CustosAC.Configuration;
using CustosAC.Models;
using CustosAC.Services;

namespace CustosAC.Scanner;

/// <summary>
/// Базовый абстрактный класс для async сканеров
/// </summary>
public abstract class BaseScannerAsync : IScanner, IDisposable
{
    protected readonly KeywordMatcherService KeywordMatcher;
    protected readonly ConsoleUIService ConsoleUI;
    protected readonly ScanSettings ScanSettings;
    private bool _disposed;

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
    }

    public abstract Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default);

    public virtual async Task<ScanResult> ScanAsync(IProgress<ScanProgress>? progress, CancellationToken cancellationToken = default)
    {
        return await ScanAsync(cancellationToken);
    }

    /// <summary>
    /// Сканирование папки с учётом глубины и исключений
    /// </summary>
    protected List<string> ScanFolder(string path, string[] extensions, int maxDepth)
    {
        var results = new List<string>();
        if (!Directory.Exists(path)) return results;

        var excludedDirs = new HashSet<string>(ScanSettings.ExcludedDirectories, StringComparer.OrdinalIgnoreCase);
        ScanFolderRecursive(path, extensions, maxDepth, 0, results, excludedDirs);
        return results;
    }

    private void ScanFolderRecursive(
        string path,
        string[] extensions,
        int maxDepth,
        int currentDepth,
        List<string> results,
        HashSet<string> excludedDirs)
    {
        if (currentDepth > maxDepth) return;

        try
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(path))
            {
                try
                {
                    var name = Path.GetFileName(entry);

                    if (Directory.Exists(entry))
                    {
                        if (excludedDirs.Contains(name)) continue;

                        if (KeywordMatcher.ContainsKeyword(name))
                            results.Add(entry);

                        ScanFolderRecursive(entry, extensions, maxDepth, currentDepth + 1, results, excludedDirs);
                    }
                    else if (File.Exists(entry) && KeywordMatcher.ContainsKeyword(name))
                    {
                        if (extensions.Length == 0 || extensions.Contains(Path.GetExtension(entry).ToLowerInvariant()))
                            results.Add(entry);
                    }
                }
                catch (UnauthorizedAccessException) { /* Access denied to file/folder - skip */ }
                catch (IOException) { /* File in use or other IO error - skip */ }
            }
        }
        catch (UnauthorizedAccessException) { /* Access denied to directory - skip */ }
        catch (IOException) { /* Directory access error - skip */ }
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}
