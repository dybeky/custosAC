using Microsoft.Data.Sqlite;
using System.ComponentModel;
using System.IO;

namespace Custos.Core.Services;

/// <summary>
/// Centralized exception handler for scanner operations with categorization and retry logic
/// </summary>
public class ScannerExceptionHandler
{
    private readonly EnhancedLogService? _logService;

    public enum ExceptionCategory
    {
        Retryable,      // Can be retried (database locked, temp access denied)
        Ignorable,      // Expected in some scenarios (single process/file access denied)
        Critical,       // System-level failure (permission denied for critical paths)
        Security        // Potential tampering or integrity violation
    }

    public class ExceptionContext
    {
        public string ScannerName { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string? ResourcePath { get; set; }
        public Exception Exception { get; set; } = null!;
        public ExceptionCategory Category { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool CanRetry => Category == ExceptionCategory.Retryable;
        public bool ShouldReport => Category == ExceptionCategory.Critical || Category == ExceptionCategory.Security;
    }

    public ScannerExceptionHandler(EnhancedLogService? logService = null)
    {
        _logService = logService;
    }

    /// <summary>
    /// Categorizes an exception and returns context information
    /// </summary>
    public ExceptionContext CategorizeException(Exception ex, string scannerName, string operation, string? resourcePath = null)
    {
        var context = new ExceptionContext
        {
            ScannerName = scannerName,
            Operation = operation,
            ResourcePath = resourcePath,
            Exception = ex
        };

        switch (ex)
        {
            // Database locking - retryable
            case SqliteException sqliteEx when sqliteEx.SqliteErrorCode == SQLitePCL.raw.SQLITE_BUSY:
                context.Category = ExceptionCategory.Retryable;
                context.Message = $"Database is locked: {resourcePath}";
                _logService?.LogWarning(EnhancedLogService.LogCategory.Scanner,
                    context.Message, scannerName);
                break;

            // Corrupted database - ignorable for individual browsers
            case SqliteException sqliteEx when sqliteEx.SqliteErrorCode == SQLitePCL.raw.SQLITE_CORRUPT:
                context.Category = ExceptionCategory.Ignorable;
                context.Message = $"Corrupted database: {resourcePath}";
                _logService?.LogWarning(EnhancedLogService.LogCategory.Scanner,
                    context.Message, scannerName);
                break;

            // Generic SQLite errors - retryable
            case SqliteException:
                context.Category = ExceptionCategory.Retryable;
                context.Message = $"SQLite error: {ex.Message}";
                _logService?.LogWarning(EnhancedLogService.LogCategory.Scanner,
                    context.Message, scannerName);
                break;

            // File access denied - depends on context
            case UnauthorizedAccessException:
                // Critical if it's a system-level path, ignorable for individual user files
                if (IsCriticalPath(resourcePath))
                {
                    context.Category = ExceptionCategory.Critical;
                    context.Message = $"Access denied to critical path: {resourcePath}";
                    _logService?.LogError(EnhancedLogService.LogCategory.Security,
                        context.Message, ex, scannerName);
                }
                else
                {
                    context.Category = ExceptionCategory.Ignorable;
                    context.Message = $"Access denied: {resourcePath}";
                    _logService?.LogDebug(EnhancedLogService.LogCategory.Scanner,
                        context.Message, scannerName);
                }
                break;

            // File not found - ignorable
            case FileNotFoundException:
            case DirectoryNotFoundException:
                context.Category = ExceptionCategory.Ignorable;
                context.Message = $"Resource not found: {resourcePath}";
                _logService?.LogDebug(EnhancedLogService.LogCategory.Scanner,
                    context.Message, scannerName);
                break;

            // File in use - retryable
            case IOException ioEx when IsFileLockedException(ioEx):
                context.Category = ExceptionCategory.Retryable;
                context.Message = $"File is locked: {resourcePath}";
                _logService?.LogWarning(EnhancedLogService.LogCategory.Scanner,
                    context.Message, scannerName);
                break;

            // Generic IO error - depends on context
            case IOException:
                context.Category = ExceptionCategory.Retryable;
                context.Message = $"IO error: {ex.Message}";
                _logService?.LogWarning(EnhancedLogService.LogCategory.Scanner,
                    context.Message, scannerName);
                break;

            // Process access denied - ignorable (common for system processes)
            case Win32Exception win32Ex when win32Ex.NativeErrorCode == 5: // ERROR_ACCESS_DENIED
                context.Category = ExceptionCategory.Ignorable;
                context.Message = $"Process access denied: {resourcePath}";
                _logService?.LogDebug(EnhancedLogService.LogCategory.Scanner,
                    context.Message, scannerName);
                break;

            // Process already exited - ignorable
            case InvalidOperationException when operation.Contains("process", StringComparison.OrdinalIgnoreCase):
                context.Category = ExceptionCategory.Ignorable;
                context.Message = $"Process already exited: {resourcePath}";
                _logService?.LogDebug(EnhancedLogService.LogCategory.Scanner,
                    context.Message, scannerName);
                break;

            // Timeout - retryable
            case TimeoutException:
                context.Category = ExceptionCategory.Retryable;
                context.Message = $"Operation timed out: {operation}";
                _logService?.LogWarning(EnhancedLogService.LogCategory.Performance,
                    context.Message, scannerName);
                break;

            // Operation cancelled - ignorable
            case OperationCanceledException:
                context.Category = ExceptionCategory.Ignorable;
                context.Message = "Operation was cancelled";
                _logService?.LogInfo(EnhancedLogService.LogCategory.Scanner,
                    context.Message, scannerName);
                break;

            // Unknown exception - critical
            default:
                context.Category = ExceptionCategory.Critical;
                context.Message = $"Unexpected error in {operation}: {ex.GetType().Name} - {ex.Message}";
                _logService?.LogError(EnhancedLogService.LogCategory.Scanner,
                    context.Message, ex, scannerName);
                break;
        }

        return context;
    }

    /// <summary>
    /// Executes an operation with retry logic for retryable exceptions
    /// </summary>
    public async Task<TResult> ExecuteWithRetryAsync<TResult>(
        Func<Task<TResult>> operation,
        string scannerName,
        string operationName,
        string? resourcePath = null,
        int maxRetries = 3,
        int[] retryDelaysMs = null!)
    {
        retryDelaysMs ??= new[] { 100, 500, 2000 };
        int attempt = 0;

        while (true)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                attempt++;
                var context = CategorizeException(ex, scannerName, operationName, resourcePath);

                if (!context.CanRetry || attempt >= maxRetries)
                {
                    _logService?.LogError(EnhancedLogService.LogCategory.Scanner,
                        $"Operation failed after {attempt} attempts: {operationName}",
                        ex, scannerName);
                    throw;
                }

                var delay = attempt - 1 < retryDelaysMs.Length
                    ? retryDelaysMs[attempt - 1]
                    : retryDelaysMs[^1];

                _logService?.LogDebug(EnhancedLogService.LogCategory.Scanner,
                    $"Retrying {operationName} in {delay}ms (attempt {attempt}/{maxRetries})",
                    scannerName);

                await Task.Delay(delay);
            }
        }
    }

    /// <summary>
    /// Executes an operation with retry logic (non-generic version)
    /// </summary>
    public async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        string scannerName,
        string operationName,
        string? resourcePath = null,
        int maxRetries = 3,
        int[] retryDelaysMs = null!)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true;
        }, scannerName, operationName, resourcePath, maxRetries, retryDelaysMs);
    }

    /// <summary>
    /// Executes an operation and returns success/failure without throwing
    /// </summary>
    public async Task<(bool Success, TResult? Result, ExceptionContext? Error)> TryExecuteAsync<TResult>(
        Func<Task<TResult>> operation,
        string scannerName,
        string operationName,
        string? resourcePath = null)
    {
        try
        {
            var result = await operation();
            return (true, result, null);
        }
        catch (Exception ex)
        {
            var context = CategorizeException(ex, scannerName, operationName, resourcePath);
            return (false, default, context);
        }
    }

    /// <summary>
    /// Handles exception with appropriate logging based on category
    /// </summary>
    public void HandleException(Exception ex, string scannerName, string operation, string? resourcePath = null)
    {
        var context = CategorizeException(ex, scannerName, operation, resourcePath);

        if (context.ShouldReport)
        {
            _logService?.LogError(EnhancedLogService.LogCategory.Scanner,
                $"Critical error in {scannerName}: {context.Message}",
                ex, scannerName);
        }
    }

    private static bool IsCriticalPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var criticalPaths = new[]
        {
            @"C:\Windows\System32",
            @"C:\Windows\Prefetch",
            @"C:\Program Files",
            @"C:\ProgramData"
        };

        return criticalPaths.Any(cp =>
            path.StartsWith(cp, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsFileLockedException(IOException ex)
    {
        const int ERROR_SHARING_VIOLATION = 0x20;
        const int ERROR_LOCK_VIOLATION = 0x21;

        var errorCode = ex.HResult & 0xFFFF;
        return errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION;
    }
}
