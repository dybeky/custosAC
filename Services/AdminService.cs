using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace CustosAC.Services;

/// <summary>
/// Сервис административных привилегий
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
        catch
        {
            return false;
        }
    }

    public void RunAsAdmin()
    {
        if (IsAdmin())
        {
            return;
        }

        try
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
            {
                return;
            }

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
        catch (System.ComponentModel.Win32Exception)
        {
            // User cancelled UAC prompt
        }
        catch (InvalidOperationException)
        {
            // Failed to start elevated process
        }
    }

    public void SetupCloseHandler(Action cleanupAction)
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cleanupAction();
            Environment.Exit(0);
        };

        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            cleanupAction();
        };
    }
}
