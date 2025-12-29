using System.Diagnostics;
using System.Security.Principal;

namespace CustosAC.WinAPI;

public static class AdminHelper
{
    private static readonly List<Process> _runningProcesses = new();
    private static readonly object _lock = new();

    public static bool IsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static void RunAsAdmin()
    {
        if (IsAdmin())
            return;

        try
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
                return;

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Environment.CurrentDirectory
            };

            Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch
        {
            // Пользователь отменил UAC или ошибка
        }
    }

    public static void TrackProcess(Process process)
    {
        lock (_lock)
        {
            _runningProcesses.Add(process);
        }
    }

    public static void UntrackProcess(Process process)
    {
        lock (_lock)
        {
            _runningProcesses.Remove(process);
        }
    }

    public static void KillAllProcesses()
    {
        List<Process> processesCopy;
        lock (_lock)
        {
            processesCopy = new List<Process>(_runningProcesses);
            _runningProcesses.Clear();
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
            catch
            {
                // Игнорируем ошибки при завершении процессов
            }
        }
    }

    public static void SetupCloseHandler(Action cleanupFunc)
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cleanupFunc();
            Environment.Exit(0);
        };

        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            cleanupFunc();
        };
    }
}
