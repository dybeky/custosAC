using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace CustosAC.Core.Services;

/// <summary>
/// Administrative privileges service
/// </summary>
[SupportedOSPlatform("windows")]
public class AdminService
{
    public bool IsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    public void RequestElevation()
    {
        if (IsAdmin()) return;

        try
        {
            using var currentProcess = Process.GetCurrentProcess();
            var exePath = Environment.ProcessPath ?? currentProcess.MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Environment.CurrentDirectory
            };

            // Process will run independently, but dispose handle before exit
            using var process = Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch { }
    }
}
