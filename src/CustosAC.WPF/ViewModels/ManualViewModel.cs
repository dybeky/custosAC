using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CustosAC.Core.Services;
using CustosAC.WPF.ViewModels.Base;
using Microsoft.Win32;

namespace CustosAC.WPF.ViewModels;

public partial class ManualViewModel : ViewModelBase
{
    private readonly GamePathFinderService _gamePathFinder;

    [ObservableProperty]
    private string _statusMessage = "";

    public LocalizationService Localization => LocalizationService.Instance;

    public ManualViewModel(GamePathFinderService gamePathFinder)
    {
        _gamePathFinder = gamePathFinder;
    }

    /// <summary>
    /// Helper method to start a process and dispose it properly.
    /// </summary>
    private static void StartProcess(ProcessStartInfo startInfo)
    {
        using var process = Process.Start(startInfo);
    }

    /// <summary>
    /// Helper method to open a folder in explorer.
    /// </summary>
    private static void OpenFolder(string path)
    {
        using var process = Process.Start("explorer.exe", $"\"{path}\"");
    }

    // Internet/Network Data Usage
    [RelayCommand]
    private void OpenDataUsage()
    {
        try
        {
            StartProcess(new ProcessStartInfo
            {
                FileName = "ms-settings:datausage",
                UseShellExecute = true
            });
            StatusMessage = "Opening Data Usage settings...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open Data Usage settings: {ex.Message}";
        }
    }

    // Videos folder
    [RelayCommand]
    private void OpenVideos()
    {
        try
        {
            var videosPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Videos");
            if (Directory.Exists(videosPath))
            {
                OpenFolder(videosPath);
                StatusMessage = "Opening Videos folder...";
            }
            else
            {
                StatusMessage = "Videos folder not found";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open Videos folder: {ex.Message}";
        }
    }

    // Unturned folder - smart search across all drives
    [RelayCommand]
    private void OpenUnturned()
    {
        try
        {
            StatusMessage = "Searching for Unturned...";

            var unturnedPath = _gamePathFinder.GetUnturnedPath();

            if (!string.IsNullOrEmpty(unturnedPath) && Directory.Exists(unturnedPath))
            {
                OpenFolder(unturnedPath);
                StatusMessage = $"Opening Unturned: {unturnedPath}";
                return;
            }

            StatusMessage = "Unturned folder not found on any drive";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open Unturned folder: {ex.Message}";
        }
    }

    // Downloads folder
    [RelayCommand]
    private void OpenDownloads()
    {
        try
        {
            var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(downloadsPath))
            {
                OpenFolder(downloadsPath);
                StatusMessage = "Opening Downloads folder...";
            }
            else
            {
                StatusMessage = "Downloads folder not found";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open Downloads folder: {ex.Message}";
        }
    }

    // AppData folder
    [RelayCommand]
    private void OpenAppData()
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            OpenFolder(appDataPath);
            StatusMessage = "Opening AppData folder...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open AppData folder: {ex.Message}";
        }
    }

    // LocalAppData folder
    [RelayCommand]
    private void OpenLocalAppData()
    {
        try
        {
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            OpenFolder(localAppDataPath);
            StatusMessage = "Opening LocalAppData folder...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open LocalAppData folder: {ex.Message}";
        }
    }

    // Prefetch folder
    [RelayCommand]
    private void OpenPrefetch()
    {
        try
        {
            var prefetchPath = @"C:\Windows\Prefetch";
            if (Directory.Exists(prefetchPath))
            {
                OpenFolder(prefetchPath);
                StatusMessage = "Opening Prefetch folder...";
            }
            else
            {
                StatusMessage = "Prefetch folder not found";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open Prefetch folder: {ex.Message}";
        }
    }

    // Registry - MuiCache
    [RelayCommand]
    private async Task OpenRegistryMuiCache()
    {
        await OpenRegistryAtAsync(@"HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache", "MuiCache");
    }

    // Registry - AppSwitched
    [RelayCommand]
    private async Task OpenRegistryAppSwitched()
    {
        await OpenRegistryAtAsync(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched", "AppSwitched");
    }

    // Registry - ShowJumpView (Taskbar jump lists history)
    [RelayCommand]
    private async Task OpenRegistryShowJumpView()
    {
        await OpenRegistryAtAsync(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\ShowJumpView", "ShowJumpView");
    }

    // Registry - AppBadgeUpdated (App badge notification history)
    [RelayCommand]
    private async Task OpenRegistryAppBadgeUpdated()
    {
        await OpenRegistryAtAsync(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppBadgeUpdated", "AppBadgeUpdated");
    }

    // Registry - AppLaunch (Application launch history)
    [RelayCommand]
    private async Task OpenRegistryAppLaunch()
    {
        await OpenRegistryAtAsync(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppLaunch", "AppLaunch");
    }

    // Registry - RunMRU (Win+R dialog history)
    [RelayCommand]
    private async Task OpenRegistryRunMRU()
    {
        await OpenRegistryAtAsync(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU", "RunMRU");
    }

    // Registry - UserAssist (Program usage statistics)
    [RelayCommand]
    private async Task OpenRegistryUserAssist()
    {
        await OpenRegistryAtAsync(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist", "UserAssist");
    }

    private async Task OpenRegistryAtAsync(string registryPath, string name)
    {
        try
        {
            // Kill any existing regedit processes first
            var processes = Process.GetProcessesByName("regedit");
            try
            {
                foreach (var proc in processes)
                {
                    try
                    {
                        proc.Kill();
                        proc.WaitForExit(1000);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to kill regedit process: {ex.Message}");
                    }
                }
            }
            finally
            {
                // Dispose all process objects to prevent memory leak
                foreach (var proc in processes)
                {
                    proc.Dispose();
                }
            }

            // Small delay to ensure regedit is fully closed (non-blocking)
            await Task.Delay(200);

            // Set the last key in regedit settings
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit");
            key?.SetValue("LastKey", registryPath);

            // Start regedit
            StartProcess(new ProcessStartInfo
            {
                FileName = "regedit.exe",
                UseShellExecute = true
            });
            StatusMessage = $"Opening Registry ({name})...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open Registry: {ex.Message}";
        }
    }

    // OneDrive folder
    [RelayCommand]
    private void OpenOneDrive()
    {
        try
        {
            var oneDrivePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive");
            if (Directory.Exists(oneDrivePath))
            {
                OpenFolder(oneDrivePath);
                StatusMessage = "Opening OneDrive folder...";
            }
            else
            {
                StatusMessage = "OneDrive folder not found";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open OneDrive folder: {ex.Message}";
        }
    }

    // Steam folder - smart search with registry and disk scan
    [RelayCommand]
    private void OpenSteam()
    {
        try
        {
            StatusMessage = "Searching for Steam...";

            var steamPath = _gamePathFinder.GetSteamPath();

            if (!string.IsNullOrEmpty(steamPath) && Directory.Exists(steamPath))
            {
                OpenFolder(steamPath);
                StatusMessage = $"Opening Steam: {steamPath}";
                return;
            }

            StatusMessage = "Steam folder not found on any drive";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open Steam folder: {ex.Message}";
        }
    }

    // Open Telegram - undead seller bot
    [RelayCommand]
    private void OpenTelegramUndead()
    {
        try
        {
            StartProcess(new ProcessStartInfo
            {
                FileName = "https://t.me/undeadsellerbot",
                UseShellExecute = true
            });
            StatusMessage = "Opening Telegram @undeadsellerbot...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open Telegram: {ex.Message}";
        }
    }

    // Open Telegram - Melony Solution bot
    [RelayCommand]
    private void OpenTelegramMelony()
    {
        try
        {
            StartProcess(new ProcessStartInfo
            {
                FileName = "https://t.me/MelonySolutionBot",
                UseShellExecute = true
            });
            StatusMessage = "Opening Telegram @MelonySolutionBot...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open Telegram: {ex.Message}";
        }
    }

    // Open Windows Defender / Windows Security
    [RelayCommand]
    private void OpenWindowsDefender()
    {
        try
        {
            StartProcess(new ProcessStartInfo
            {
                FileName = "windowsdefender://",
                UseShellExecute = true
            });
            StatusMessage = "Opening Windows Defender...";
        }
        catch
        {
            // Fallback to Windows Security settings
            try
            {
                StartProcess(new ProcessStartInfo
                {
                    FileName = "ms-settings:windowsdefender",
                    UseShellExecute = true
                });
                StatusMessage = "Opening Windows Security...";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to open Windows Defender: {ex.Message}";
            }
        }
    }
}
