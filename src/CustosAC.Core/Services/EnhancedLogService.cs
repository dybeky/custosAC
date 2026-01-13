using System.Collections.Concurrent;
using System.Text;

namespace CustosAC.Core.Services;

/// <summary>
/// Enhanced logging service with structured logging, log levels, categories, and async capabilities
/// </summary>
public class EnhancedLogService : IDisposable
{
    private readonly string _logDirectory;
    private readonly string _logFilePath;
    private readonly ConcurrentQueue<LogEntry> _logQueue = new();
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);
    private readonly StringBuilder _sessionLog = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _writeTask;
    private bool _disposed;
    private const int MaxLogSizeMb = 50;
    private const int MaxInMemoryLogEntries = 1000;

    public enum LogLevel
    {
        TRACE = 0,
        DEBUG = 1,
        INFO = 2,
        WARN = 3,
        ERROR = 4,
        CRITICAL = 5
    }

    public enum LogCategory
    {
        General,
        Scanner,
        Security,
        Performance,
        Validation
    }

    private class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public LogCategory Category { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Context { get; set; }
        public Exception? Exception { get; set; }
    }

    public EnhancedLogService(string? logDirectory = null)
    {
        _logDirectory = logDirectory ?? Path.Combine(AppContext.BaseDirectory, "Logs");

        if (!Directory.Exists(_logDirectory))
        {
            try { Directory.CreateDirectory(_logDirectory); }
            catch { _logDirectory = Path.GetTempPath(); }
        }

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _logFilePath = Path.Combine(_logDirectory, $"CustosAC_Enhanced_{timestamp}.log");

        // Start async writer task
        _writeTask = Task.Run(async () => await ProcessLogQueueAsync());

        LogInfo(LogCategory.General, "=== CustosAC Enhanced Logging Started ===");
        LogInfo(LogCategory.General, $"Computer: {Environment.MachineName}");
        LogInfo(LogCategory.General, $"User: {Environment.UserName}");
        LogInfo(LogCategory.General, $"OS: {Environment.OSVersion}");
        LogInfo(LogCategory.General, $"Log File: {_logFilePath}");
    }

    public void LogTrace(LogCategory category, string message, string? context = null)
        => EnqueueLog(LogLevel.TRACE, category, message, context);

    public void LogDebug(LogCategory category, string message, string? context = null)
        => EnqueueLog(LogLevel.DEBUG, category, message, context);

    public void LogInfo(LogCategory category, string message, string? context = null)
        => EnqueueLog(LogLevel.INFO, category, message, context);

    public void LogWarning(LogCategory category, string message, string? context = null)
        => EnqueueLog(LogLevel.WARN, category, message, context);

    public void LogError(LogCategory category, string message, Exception? exception = null, string? context = null)
        => EnqueueLog(LogLevel.ERROR, category, message, context, exception);

    public void LogCritical(LogCategory category, string message, Exception? exception = null, string? context = null)
        => EnqueueLog(LogLevel.CRITICAL, category, message, context, exception);

    public void LogScannerStart(string scannerName)
        => LogInfo(LogCategory.Scanner, $"Starting scan: {scannerName}", scannerName);

    public void LogScannerComplete(string scannerName, int findingsCount, TimeSpan duration)
        => LogInfo(LogCategory.Scanner, $"Completed: {scannerName} | Findings: {findingsCount} | Duration: {duration.TotalSeconds:F2}s", scannerName);

    public void LogScannerError(string scannerName, string error, Exception? exception = null)
        => LogError(LogCategory.Scanner, $"Scanner failed: {scannerName} | Error: {error}", exception, scannerName);

    public void LogSecurityEvent(string eventType, string details, string? context = null)
        => LogWarning(LogCategory.Security, $"[{eventType}] {details}", context);

    public void LogPerformance(string operation, TimeSpan duration, string? context = null)
    {
        var level = duration.TotalSeconds > 5 ? LogLevel.WARN : LogLevel.DEBUG;
        EnqueueLog(level, LogCategory.Performance, $"{operation} took {duration.TotalMilliseconds:F2}ms", context);
    }

    public void LogValidation(string target, string issue, string? context = null)
        => LogWarning(LogCategory.Validation, $"Validation issue in {target}: {issue}", context);

    private void EnqueueLog(LogLevel level, LogCategory category, string message, string? context = null, Exception? exception = null)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Category = category,
            Message = message,
            Context = context,
            Exception = exception
        };

        _logQueue.Enqueue(entry);

        // Keep in-memory log trimmed
        if (_sessionLog.Length > MaxInMemoryLogEntries * 200) // Approx 200 chars per entry
        {
            var lines = _sessionLog.ToString().Split(Environment.NewLine);
            _sessionLog.Clear();
            _sessionLog.AppendJoin(Environment.NewLine, lines.Skip(MaxInMemoryLogEntries / 2));
        }
    }

    private async Task ProcessLogQueueAsync()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                if (_logQueue.TryDequeue(out var entry))
                {
                    var logLine = FormatLogEntry(entry);

                    await _writeSemaphore.WaitAsync(_cancellationTokenSource.Token);
                    try
                    {
                        _sessionLog.AppendLine(logLine);
                        await WriteToFileAsync(logLine);
                    }
                    finally
                    {
                        _writeSemaphore.Release();
                    }
                }
                else
                {
                    await Task.Delay(100, _cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Silently continue on write errors
            }
        }
    }

    private string FormatLogEntry(LogEntry entry)
    {
        var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var level = entry.Level.ToString().PadRight(8);
        var category = entry.Category.ToString().PadRight(11);

        var logLine = $"[{timestamp}] [{level}] [{category}] {entry.Message}";

        if (!string.IsNullOrEmpty(entry.Context))
        {
            logLine += $" | Context: {entry.Context}";
        }

        if (entry.Exception != null)
        {
            logLine += $"{Environment.NewLine}    Exception: {entry.Exception.GetType().Name}: {entry.Exception.Message}";
            logLine += $"{Environment.NewLine}    StackTrace: {entry.Exception.StackTrace}";

            if (entry.Exception.InnerException != null)
            {
                logLine += $"{Environment.NewLine}    InnerException: {entry.Exception.InnerException.Message}";
            }
        }

        return logLine;
    }

    private async Task WriteToFileAsync(string logLine)
    {
        try
        {
            // Check file size and rotate if needed
            var fileInfo = new FileInfo(_logFilePath);
            if (fileInfo.Exists && fileInfo.Length > MaxLogSizeMb * 1024 * 1024)
            {
                await RotateLogFileAsync();
            }

            await File.AppendAllTextAsync(_logFilePath, logLine + Environment.NewLine);
        }
        catch
        {
            // Silently fail on write errors
        }
    }

    private async Task RotateLogFileAsync()
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var rotatedPath = Path.Combine(_logDirectory, $"CustosAC_Enhanced_{timestamp}_rotated.log");

            if (File.Exists(_logFilePath))
            {
                File.Move(_logFilePath, rotatedPath);
            }

            // Clean up old log files (keep only last 10)
            var logFiles = Directory.GetFiles(_logDirectory, "CustosAC_Enhanced_*.log")
                .OrderByDescending(f => File.GetCreationTime(f))
                .Skip(10)
                .ToArray();

            foreach (var oldFile in logFiles)
            {
                try { File.Delete(oldFile); } catch { }
            }

            await Task.CompletedTask;
        }
        catch
        {
            // Silently fail on rotation errors
        }
    }

    public string GetLogFilePath() => _logFilePath;

    public string GetSessionLog()
    {
        _writeSemaphore.Wait();
        try
        {
            return _sessionLog.ToString();
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    public async Task FlushAsync()
    {
        // Wait for queue to empty
        while (!_logQueue.IsEmpty)
        {
            await Task.Delay(50);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        LogInfo(LogCategory.General, "=== CustosAC Enhanced Logging Ended ===");

        // Stop the writer task
        _cancellationTokenSource.Cancel();
        try
        {
            _writeTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch { }

        // Process remaining log entries
        while (_logQueue.TryDequeue(out var entry))
        {
            var logLine = FormatLogEntry(entry);
            try
            {
                File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
            }
            catch { }
        }

        _writeSemaphore?.Dispose();
        _cancellationTokenSource?.Dispose();
        _disposed = true;
    }
}
