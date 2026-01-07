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
    /// Простое синхронное сканирование папки (без deadlock)
    /// </summary>
    protected Task<List<string>> ScanFolderParallelAsync(
        string path,
        string[] extensions,
        int maxDepth,
        CancellationToken ct = default)
    {
        var results = new List<string>();

        if (!Directory.Exists(path))
        {
            return Task.FromResult(results);
        }

        var excludedDirs = new HashSet<string>(ScanSettings.ExcludedDirectories, StringComparer.OrdinalIgnoreCase);

        ScanFolderSync(path, extensions, maxDepth, 0, results, excludedDirs);

        return Task.FromResult(results);
    }

    private void ScanFolderSync(
        string path,
        string[] extensions,
        int maxDepth,
        int currentDepth,
        List<string> results,
        HashSet<string> excludedDirs)
    {
        if (currentDepth > maxDepth)
            return;

        try
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(path))
            {
                try
                {
                    var name = Path.GetFileName(entry);

                    if (Directory.Exists(entry))
                    {
                        // Пропустить исключённые папки
                        if (excludedDirs.Contains(name))
                            continue;

                        // Проверить имя папки на ключевые слова
                        if (KeywordMatcher.ContainsKeyword(name))
                        {
                            results.Add(entry);
                        }

                        // Рекурсивно сканировать
                        ScanFolderSync(entry, extensions, maxDepth, currentDepth + 1, results, excludedDirs);
                    }
                    else if (File.Exists(entry))
                    {
                        // Проверить имя файла
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
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
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
