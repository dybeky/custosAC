using System.Diagnostics;
using CustosAC.Abstractions;
using CustosAC.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustosAC.Services;

/// <summary>
/// Реализация сервиса работы с процессами
/// </summary>
public class ProcessService : IProcessService, IDisposable
{
    private readonly List<Process> _trackedProcesses = new();
    private readonly object _lock = new();
    private readonly ILogger<ProcessService> _logger;
    private readonly AppSettings _settings;
    private bool _disposed = false;

    public ProcessService(ILogger<ProcessService> logger, IOptions<AppSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public void TrackProcess(Process process)
    {
        lock (_lock)
        {
            _trackedProcesses.Add(process);
            _logger.LogDebug("Tracking process: {ProcessName} (PID: {PID})", process.ProcessName, process.Id);
        }
    }

    public void UntrackProcess(Process process)
    {
        lock (_lock)
        {
            if (_trackedProcesses.Remove(process))
            {
                _logger.LogDebug("Untracking process: PID {PID}", process.Id);
                // Dispose when untracking
                try
                {
                    process.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing untracked process");
                }
            }
        }
    }

    public void KillAllTrackedProcesses()
    {
        List<Process> processesCopy;
        lock (_lock)
        {
            processesCopy = new List<Process>(_trackedProcesses);
            _trackedProcesses.Clear();
        }

        foreach (var process in processesCopy)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    _logger.LogDebug("Killed process: PID {PID}", process.Id);
                }
            }
            catch (InvalidOperationException)
            {
                // Процесс уже завершён
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                _logger.LogWarning(ex, "Failed to kill process PID {PID}", process.Id);
            }
            finally
            {
                // CRITICAL: Dispose the process object
                try
                {
                    process.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing process");
                }
            }
        }
    }

    public async Task<bool> RunCommandAsync(string command, string? args = null, int timeoutMs = 10000)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args ?? string.Empty,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return false;

            using var cts = new CancellationTokenSource(timeoutMs);
            await process.WaitForExitAsync(cts.Token);
            return process.ExitCode == 0;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Command timed out: {Command}", command);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run command: {Command}", command);
            return false;
        }
    }

    public Task<Process?> StartProcessAsync(ProcessStartInfo psi)
    {
        try
        {
            var process = Process.Start(psi);
            return Task.FromResult(process);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start process: {FileName}", psi.FileName);
            return Task.FromResult<Process?>(null);
        }
    }

    public async Task<Process?> StartTrackedProcessAsync(string fileName, string? arguments = null)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? string.Empty,
                UseShellExecute = true
            };

            var process = await StartProcessAsync(psi);
            if (process != null)
            {
                TrackProcess(process);
            }
            return process;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start tracked process: {FileName}", fileName);
            return null;
        }
    }

    public async Task OpenFolderAsync(string path)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = path,
                UseShellExecute = true
            };

            var process = await StartProcessAsync(psi);
            if (process != null)
            {
                TrackProcess(process);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open folder: {Path}", path);
        }
    }

    public async Task OpenUrlAsync(string url)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };

            var process = await StartProcessAsync(psi);
            if (process != null)
            {
                TrackProcess(process);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open URL: {Url}", url);
        }
    }

    public async Task CopyToClipboardAsync(string text)
    {
        try
        {
            // Use PowerShell Set-Clipboard to prevent command injection
            // Escape single quotes in the text for PowerShell
            var escapedText = text.Replace("'", "''");
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"Set-Clipboard -Value '{escapedText}'\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                await process.WaitForExitAsync();
                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    _logger.LogWarning("PowerShell clipboard command failed: {Error}", error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy to clipboard");
        }
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

        if (disposing)
        {
            lock (_lock)
            {
                foreach (var process in _trackedProcesses)
                {
                    try
                    {
                        process?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error disposing tracked process");
                    }
                }
                _trackedProcesses.Clear();
            }
        }

        _disposed = true;
    }
}
