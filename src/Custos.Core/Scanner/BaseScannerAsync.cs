using Custos.Core.Abstractions;
using Custos.Core.Configuration;
using Custos.Core.Models;
using Custos.Core.Services;

namespace Custos.Core.Scanner;

/// <summary>
/// Base abstract class for async scanners
/// </summary>
public abstract class BaseScannerAsync : IScanner, IDisposable
{
    protected readonly KeywordMatcherService KeywordMatcher;
    protected readonly IUIService UIService;
    protected readonly ScanSettings ScanSettings;
    private readonly HashSet<string> _excludedDirs;
    private bool _disposed;

    public abstract string Name { get; }
    public abstract string Description { get; }

    protected BaseScannerAsync(
        KeywordMatcherService keywordMatcher,
        IUIService uiService,
        ScanSettings scanSettings)
    {
        KeywordMatcher = keywordMatcher;
        UIService = uiService;
        ScanSettings = scanSettings;
        _excludedDirs = new HashSet<string>(ScanSettings.ExcludedDirectories, StringComparer.OrdinalIgnoreCase);
    }

    public abstract Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default);

    public virtual async Task<ScanResult> ScanAsync(IProgress<ScanProgress>? progress, CancellationToken cancellationToken = default)
    {
        return await ScanAsync(cancellationToken);
    }

    protected List<string> ScanFolder(string path, string[] extensions, int maxDepth, CancellationToken cancellationToken = default)
    {
        var results = new List<string>();
        if (!Directory.Exists(path)) return results;

        ScanFolderRecursive(path, extensions, maxDepth, 0, results, cancellationToken);
        return results;
    }

    private void ScanFolderRecursive(
        string path, string[] extensions, int maxDepth, int currentDepth,
        List<string> results, CancellationToken cancellationToken)
    {
        if (currentDepth > maxDepth) return;
        if (cancellationToken.IsCancellationRequested) return;

        try
        {
            var enumOptions = new EnumerationOptions
            {
                AttributesToSkip = FileAttributes.System,
                IgnoreInaccessible = true,
                RecurseSubdirectories = false
            };

            foreach (var entry in Directory.EnumerateFileSystemEntries(path, "*", enumOptions))
            {
                if (cancellationToken.IsCancellationRequested) return;

                try
                {
                    var name = Path.GetFileName(entry);
                    var isHidden = (File.GetAttributes(entry) & FileAttributes.Hidden) != 0;

                    if (Directory.Exists(entry))
                    {
                        if (_excludedDirs.Contains(name)) continue;

                        if (KeywordMatcher.ContainsKeywordWithWhitelist(name, entry))
                        {
                            var suffix = isHidden ? " [HIDDEN]" : "";
                            results.Add(entry + suffix);
                        }

                        ScanFolderRecursive(entry, extensions, maxDepth, currentDepth + 1, results, cancellationToken);
                    }
                    else if (File.Exists(entry) && KeywordMatcher.ContainsKeywordWithWhitelist(name, entry))
                    {
                        if (extensions.Length == 0 || extensions.Contains(Path.GetExtension(entry).ToLowerInvariant()))
                        {
                            var suffix = isHidden ? " [HIDDEN]" : "";
                            results.Add(entry + suffix);
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
        if (_disposed) return;
        _disposed = true;
    }
}
