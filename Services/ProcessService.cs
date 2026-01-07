using System.Diagnostics;
using CustosAC.Configuration;

namespace CustosAC.Services;

/// <summary>
/// Сервис работы с процессами
/// </summary>
public class ProcessService : IDisposable
{
    private readonly List<Process> _trackedProcesses = new();
    private readonly object _lock = new();
    private readonly AppSettings _settings;
    private bool _disposed = false;

    public ProcessService(AppSettings settings)
    {
        _settings = settings;
    }

    public void TrackProcess(Process process)
    {
        lock (_lock)
        {
            _trackedProcesses.Add(process);
        }
    }

    public void UntrackProcess(Process process)
    {
        lock (_lock)
        {
            if (_trackedProcesses.Remove(process))
            {
                try
                {
                    process.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
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
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Failed to kill process
            }
            finally
            {
                try
                {
                    process.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
        }
    }

    public async Task<bool> RunCommandAsync(string command, string? args = null, int timeoutMs = 10000)
    {
        Process? process = null;
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

            process = Process.Start(psi);
            if (process == null)
                return false;

            using var cts = new CancellationTokenSource(timeoutMs);
            await process.WaitForExitAsync(cts.Token);
            return process.ExitCode == 0;
        }
        catch (OperationCanceledException)
        {
            // Timeout - kill the process to prevent resource leak
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
            catch { }
            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            process?.Dispose();
        }
    }

    public Task<Process?> StartProcessAsync(ProcessStartInfo psi)
    {
        try
        {
            var process = Process.Start(psi);
            return Task.FromResult(process);
        }
        catch
        {
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
        catch
        {
            return null;
        }
    }

    public Task OpenFolderAsync(string path)
        => OpenWithShellAsync("explorer.exe", path);

    public Task OpenUrlAsync(string url)
        => OpenWithShellAsync(url);

    private async Task OpenWithShellAsync(string fileName, string? arguments = null)
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
        }
        catch
        {
            // Ignore errors opening shell processes
        }
    }

    public async Task CopyToClipboardAsync(string text)
    {
        Process? process = null;
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

            process = Process.Start(psi);
            if (process != null)
            {
                using var cts = new CancellationTokenSource(_settings.Timeouts.PowerShellTimeoutMs);
                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Timeout - kill the process
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                    catch { }
                }
            }
        }
        catch
        {
            // Ignore errors
        }
        finally
        {
            process?.Dispose();
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
                    catch
                    {
                        // Ignore disposal errors
                    }
                }
                _trackedProcesses.Clear();
            }
        }

        _disposed = true;
    }
}
