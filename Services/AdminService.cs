using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;
using CustosAC.Abstractions;
using Microsoft.Extensions.Logging;

namespace CustosAC.Services;

/// <summary>
/// Реализация сервиса административных привилегий
/// </summary>
[SupportedOSPlatform("windows")]
public class AdminService : IAdminService
{
    private readonly ILogger<AdminService> _logger;

    public AdminService(ILogger<AdminService> logger)
    {
        _logger = logger;
    }

    public bool IsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check admin status");
            return false;
        }
    }

    public void RunAsAdmin()
    {
        if (IsAdmin())
        {
            _logger.LogInformation("Already running as administrator");
            return;
        }

        try
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
            {
                _logger.LogError("Could not determine executable path");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Environment.CurrentDirectory
            };

            _logger.LogInformation("Restarting as administrator");
            Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            _logger.LogWarning("User cancelled UAC prompt");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to start elevated process");
        }
    }

    public void SetupCloseHandler(Action cleanupAction)
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            _logger.LogInformation("Received Ctrl+C, cleaning up...");
            cleanupAction();
            Environment.Exit(0);
        };

        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            _logger.LogInformation("Process exiting, cleaning up...");
            cleanupAction();
        };

        _logger.LogDebug("Close handlers registered");
    }
}
