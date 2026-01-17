using System.Text;
using Custos.Core.Models;

namespace Custos.Core.Services;

/// <summary>
/// Logging service for actions and scan results
/// </summary>
public class LogService : IDisposable
{
    private readonly string _logDirectory;
    private readonly string _logFilePath;
    private readonly object _lock = new();
    private readonly StringBuilder _sessionLog = new();
    private bool _disposed;

    public LogService(string? logDirectory = null)
    {
        // Logging to file is disabled - only in-memory logging
        _logDirectory = string.Empty;
        _logFilePath = string.Empty;

        LogInfo("=== Custos Session Started (Memory Only) ===");
        LogInfo($"Computer: {Environment.MachineName}");
        LogInfo($"User: {Environment.UserName}");
        LogInfo($"OS: {Environment.OSVersion}");
    }

    public void LogInfo(string message) => WriteLog("INFO", message);
    public void LogWarning(string message) => WriteLog("WARN", message);

    public void LogError(string message, Exception? exception = null)
    {
        var fullMessage = exception != null ? $"{message} | Exception: {exception.Message}" : message;
        WriteLog("ERROR", fullMessage);
    }

    public void LogScanResult(string scannerName, ScanResult result)
    {
        var status = result.Success ? "SUCCESS" : "FAILED";
        WriteLog("SCAN", $"[{scannerName}] {status} | Findings: {result.Count} | Duration: {result.Duration.TotalSeconds:F2}s");

        if (result.HasFindings)
        {
            foreach (var finding in result.Findings)
                WriteLog("FIND", $"[{scannerName}] {finding}");
        }
    }

    public void LogSecurityEvent(string eventType, string details) => WriteLog("SECURITY", $"[{eventType}] {details}");
    public string GetLogFilePath() => _logFilePath;
    public string GetSessionLog() { lock (_lock) { return _sessionLog.ToString(); } }

    private void WriteLog(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logLine = $"[{timestamp}] [{level,-8}] {message}";

        lock (_lock)
        {
            _sessionLog.AppendLine(logLine);
            // File logging disabled - only in-memory logging is active
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        LogInfo("=== Custos Session Ended ===");
        _disposed = true;
    }
}
