using System.Collections.Concurrent;

namespace CustosAC.Core.Services;

/// <summary>
/// Manages temporary files with automatic cleanup and retry logic
/// </summary>
public class TempFileManager : IDisposable
{
    private readonly string _baseTempDirectory;
    private readonly ConcurrentDictionary<string, TempFileInfo> _trackedFiles = new();
    private readonly EnhancedLogService? _logService;
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    private class TempFileInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string ScannerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool MarkedForDeletion { get; set; }
    }

    public TempFileManager(EnhancedLogService? logService = null)
    {
        _logService = logService;
        _baseTempDirectory = Path.Combine(Path.GetTempPath(), "CustosAC_Temp");

        try
        {
            if (!Directory.Exists(_baseTempDirectory))
            {
                Directory.CreateDirectory(_baseTempDirectory);
            }

            // Clean up any orphaned files from previous runs
            CleanupOrphanedFiles();
        }
        catch (Exception ex)
        {
            _logService?.LogWarning(EnhancedLogService.LogCategory.General,
                $"Failed to initialize temp directory: {ex.Message}");
            _baseTempDirectory = Path.GetTempPath();
        }

        // Setup periodic cleanup timer (every 5 minutes)
        _cleanupTimer = new Timer(_ => CleanupOldFiles(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        // Register cleanup on AppDomain unload
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    /// <summary>
    /// Creates a temporary file for a scanner with auto-tracking
    /// </summary>
    public string CreateTempFile(string scannerName, string? preferredExtension = null)
    {
        var extension = preferredExtension ?? ".tmp";
        if (!extension.StartsWith("."))
            extension = "." + extension;

        var fileName = $"{scannerName}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_baseTempDirectory, fileName);

        var fileInfo = new TempFileInfo
        {
            FilePath = filePath,
            ScannerName = scannerName,
            CreatedAt = DateTime.Now,
            MarkedForDeletion = false
        };

        _trackedFiles[filePath] = fileInfo;

        _logService?.LogTrace(EnhancedLogService.LogCategory.Scanner,
            $"Created temp file: {fileName}", scannerName);

        return filePath;
    }

    /// <summary>
    /// Copies a file to temporary location with lock detection
    /// </summary>
    public async Task<string?> CopyToTempAsync(string sourcePath, string scannerName, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
        {
            _logService?.LogWarning(EnhancedLogService.LogCategory.Scanner,
                $"Source file does not exist: {sourcePath}", scannerName);
            return null;
        }

        var extension = Path.GetExtension(sourcePath);
        var tempPath = CreateTempFile(scannerName, extension);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                // Try to detect if file is locked
                if (IsFileLocked(sourcePath))
                {
                    _logService?.LogDebug(EnhancedLogService.LogCategory.Scanner,
                        $"File is locked, attempt {attempt + 1}/3: {sourcePath}", scannerName);

                    if (attempt < 2)
                    {
                        await Task.Delay(100 * (attempt + 1), cancellationToken);
                        continue;
                    }
                }

                await using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                await using (var dest = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await source.CopyToAsync(dest, cancellationToken);
                }

                _logService?.LogTrace(EnhancedLogService.LogCategory.Scanner,
                    $"Copied to temp: {Path.GetFileName(sourcePath)} -> {Path.GetFileName(tempPath)}", scannerName);

                return tempPath;
            }
            catch (IOException ex) when (attempt < 2)
            {
                _logService?.LogDebug(EnhancedLogService.LogCategory.Scanner,
                    $"Copy failed (attempt {attempt + 1}/3): {ex.Message}", scannerName);
                await Task.Delay(200 * (attempt + 1), cancellationToken);
            }
            catch (Exception ex)
            {
                _logService?.LogError(EnhancedLogService.LogCategory.Scanner,
                    $"Failed to copy file to temp: {sourcePath}", ex, scannerName);

                // Cleanup failed temp file
                DeleteTempFile(tempPath, scannerName);
                return null;
            }
        }

        _logService?.LogWarning(EnhancedLogService.LogCategory.Scanner,
            $"Failed to copy file after 3 attempts: {sourcePath}", scannerName);

        DeleteTempFile(tempPath, scannerName);
        return null;
    }

    /// <summary>
    /// Deletes a temporary file with retry logic
    /// </summary>
    public bool DeleteTempFile(string filePath, string? scannerName = null, bool immediate = true)
    {
        if (!_trackedFiles.ContainsKey(filePath))
        {
            _logService?.LogDebug(EnhancedLogService.LogCategory.Scanner,
                $"Attempting to delete untracked file: {filePath}", scannerName ?? "Unknown");
        }

        if (immediate)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        _logService?.LogTrace(EnhancedLogService.LogCategory.Scanner,
                            $"Deleted temp file: {Path.GetFileName(filePath)}", scannerName ?? "Unknown");
                    }

                    _trackedFiles.TryRemove(filePath, out _);
                    return true;
                }
                catch (IOException) when (attempt < 2)
                {
                    Thread.Sleep(100 * (attempt + 1));
                }
                catch (Exception ex)
                {
                    _logService?.LogDebug(EnhancedLogService.LogCategory.Scanner,
                        $"Failed to delete temp file: {ex.Message}", scannerName ?? "Unknown");
                    break;
                }
            }
        }

        // Mark for delayed deletion
        if (_trackedFiles.TryGetValue(filePath, out var fileInfo))
        {
            fileInfo.MarkedForDeletion = true;
            _logService?.LogTrace(EnhancedLogService.LogCategory.Scanner,
                $"Marked for delayed deletion: {Path.GetFileName(filePath)}", scannerName ?? "Unknown");
        }

        return false;
    }

    /// <summary>
    /// Deletes all temporary files created by a specific scanner
    /// </summary>
    public void DeleteScannerTempFiles(string scannerName)
    {
        var scannerFiles = _trackedFiles.Values
            .Where(f => f.ScannerName.Equals(scannerName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var fileInfo in scannerFiles)
        {
            DeleteTempFile(fileInfo.FilePath, scannerName, immediate: false);
        }
    }

    /// <summary>
    /// Gets the number of tracked temporary files
    /// </summary>
    public int GetTrackedFileCount() => _trackedFiles.Count;

    /// <summary>
    /// Gets tracked files for a specific scanner
    /// </summary>
    public IReadOnlyList<string> GetScannerTempFiles(string scannerName)
    {
        return _trackedFiles.Values
            .Where(f => f.ScannerName.Equals(scannerName, StringComparison.OrdinalIgnoreCase))
            .Select(f => f.FilePath)
            .ToList();
    }

    private bool IsFileLocked(string filePath)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void CleanupOldFiles()
    {
        var oldFiles = _trackedFiles.Values
            .Where(f => f.MarkedForDeletion || (DateTime.Now - f.CreatedAt).TotalMinutes > 30)
            .ToList();

        foreach (var fileInfo in oldFiles)
        {
            DeleteTempFile(fileInfo.FilePath, fileInfo.ScannerName, immediate: true);
        }
    }

    private void CleanupOrphanedFiles()
    {
        try
        {
            if (!Directory.Exists(_baseTempDirectory))
                return;

            var orphanedFiles = Directory.GetFiles(_baseTempDirectory);
            var deletedCount = 0;

            foreach (var file in orphanedFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    // Delete files older than 1 hour
                    if ((DateTime.Now - fileInfo.CreationTime).TotalHours > 1)
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                }
                catch
                {
                    // Silently continue
                }
            }

            if (deletedCount > 0)
            {
                _logService?.LogInfo(EnhancedLogService.LogCategory.General,
                    $"Cleaned up {deletedCount} orphaned temp file(s)");
            }
        }
        catch (Exception ex)
        {
            _logService?.LogWarning(EnhancedLogService.LogCategory.General,
                $"Failed to cleanup orphaned files: {ex.Message}");
        }
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        Cleanup();
    }

    private void Cleanup()
    {
        _logService?.LogDebug(EnhancedLogService.LogCategory.General,
            $"Cleaning up {_trackedFiles.Count} temp file(s)");

        foreach (var fileInfo in _trackedFiles.Values.ToList())
        {
            try
            {
                if (File.Exists(fileInfo.FilePath))
                {
                    File.Delete(fileInfo.FilePath);
                }
            }
            catch
            {
                // Silently continue
            }
        }

        _trackedFiles.Clear();

        // Try to remove temp directory if empty
        try
        {
            if (Directory.Exists(_baseTempDirectory) && !Directory.EnumerateFileSystemEntries(_baseTempDirectory).Any())
            {
                Directory.Delete(_baseTempDirectory);
            }
        }
        catch
        {
            // Silently continue
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _cleanupTimer?.Dispose();
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        Cleanup();

        _disposed = true;
    }
}
