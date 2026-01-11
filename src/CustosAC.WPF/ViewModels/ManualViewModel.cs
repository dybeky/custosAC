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
    [ObservableProperty]
    private string _statusMessage = "";

    public LocalizationService Localization => LocalizationService.Instance;

    // Internet/Network Data Usage
    [RelayCommand]
    private void OpenDataUsage()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:datausage",
                UseShellExecute = true
            });
            StatusMessage = "Opening Data Usage settings...";
        }
        catch
        {
            StatusMessage = "Failed to open Data Usage settings";
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
                Process.Start("explorer.exe", $"\"{videosPath}\"");
                StatusMessage = "Opening Videos folder...";
            }
            else
            {
                StatusMessage = "Videos folder not found";
            }
        }
        catch
        {
            StatusMessage = "Failed to open Videos folder";
        }
    }

    // Unturned folder
    [RelayCommand]
    private void OpenUnturned()
    {
        try
        {
            var paths = new[]
            {
                @"C:\Program Files (x86)\Steam\steamapps\common\Unturned",
                @"C:\Program Files\Steam\steamapps\common\Unturned",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Unturned")
            };

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", $"\"{path}\"");
                    StatusMessage = "Opening Unturned folder...";
                    return;
                }
            }

            StatusMessage = "Unturned folder not found";
        }
        catch
        {
            StatusMessage = "Failed to open Unturned folder";
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
                Process.Start("explorer.exe", $"\"{downloadsPath}\"");
                StatusMessage = "Opening Downloads folder...";
            }
            else
            {
                StatusMessage = "Downloads folder not found";
            }
        }
        catch
        {
            StatusMessage = "Failed to open Downloads folder";
        }
    }

    // AppData folder
    [RelayCommand]
    private void OpenAppData()
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            Process.Start("explorer.exe", $"\"{appDataPath}\"");
            StatusMessage = "Opening AppData folder...";
        }
        catch
        {
            StatusMessage = "Failed to open AppData folder";
        }
    }

    // LocalAppData folder
    [RelayCommand]
    private void OpenLocalAppData()
    {
        try
        {
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            Process.Start("explorer.exe", $"\"{localAppDataPath}\"");
            StatusMessage = "Opening LocalAppData folder...";
        }
        catch
        {
            StatusMessage = "Failed to open LocalAppData folder";
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
                Process.Start("explorer.exe", $"\"{prefetchPath}\"");
                StatusMessage = "Opening Prefetch folder...";
            }
            else
            {
                StatusMessage = "Prefetch folder not found";
            }
        }
        catch
        {
            StatusMessage = "Failed to open Prefetch folder";
        }
    }

    // Registry - MuiCache
    [RelayCommand]
    private void OpenRegistryMuiCache()
    {
        OpenRegistryAt(@"HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache", "MuiCache");
    }

    // Registry - AppSwitched
    [RelayCommand]
    private void OpenRegistryAppSwitched()
    {
        OpenRegistryAt(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched", "AppSwitched");
    }

    // Registry - ShowJumpView (Taskbar jump lists history)
    [RelayCommand]
    private void OpenRegistryShowJumpView()
    {
        OpenRegistryAt(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\ShowJumpView", "ShowJumpView");
    }

    // Registry - RunMRU (Win+R dialog history)
    [RelayCommand]
    private void OpenRegistryRunMRU()
    {
        OpenRegistryAt(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU", "RunMRU");
    }

    // Registry - UserAssist (Program usage statistics)
    [RelayCommand]
    private void OpenRegistryUserAssist()
    {
        OpenRegistryAt(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist", "UserAssist");
    }

    private void OpenRegistryAt(string registryPath, string name)
    {
        try
        {
            // Kill any existing regedit processes first
            foreach (var proc in Process.GetProcessesByName("regedit"))
            {
                try
                {
                    proc.Kill();
                    proc.WaitForExit(1000);
                }
                catch { }
            }

            // Small delay to ensure regedit is fully closed
            System.Threading.Thread.Sleep(200);

            // Set the last key in regedit settings
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit");
            key?.SetValue("LastKey", registryPath);

            // Start regedit
            Process.Start(new ProcessStartInfo
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
                Process.Start("explorer.exe", $"\"{oneDrivePath}\"");
                StatusMessage = "Opening OneDrive folder...";
            }
            else
            {
                StatusMessage = "OneDrive folder not found";
            }
        }
        catch
        {
            StatusMessage = "Failed to open OneDrive folder";
        }
    }

    // Steam folder
    [RelayCommand]
    private void OpenSteam()
    {
        try
        {
            var paths = new[]
            {
                @"C:\Program Files (x86)\Steam",
                @"C:\Program Files\Steam"
            };

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", $"\"{path}\"");
                    StatusMessage = "Opening Steam folder...";
                    return;
                }
            }

            StatusMessage = "Steam folder not found";
        }
        catch
        {
            StatusMessage = "Failed to open Steam folder";
        }
    }

    // Open Telegram - undead seller bot
    [RelayCommand]
    private void OpenTelegramUndead()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://t.me/undeadsellerbot",
                UseShellExecute = true
            });
            StatusMessage = "Opening Telegram @undeadsellerbot...";
        }
        catch
        {
            StatusMessage = "Failed to open Telegram";
        }
    }

    // Open Telegram - Melony Solution bot
    [RelayCommand]
    private void OpenTelegramMelony()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://t.me/MelonySolutionBot",
                UseShellExecute = true
            });
            StatusMessage = "Opening Telegram @MelonySolutionBot...";
        }
        catch
        {
            StatusMessage = "Failed to open Telegram";
        }
    }

    // Open Windows Defender / Windows Security
    [RelayCommand]
    private void OpenWindowsDefender()
    {
        try
        {
            Process.Start(new ProcessStartInfo
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
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:windowsdefender",
                    UseShellExecute = true
                });
                StatusMessage = "Opening Windows Security...";
            }
            catch
            {
                StatusMessage = "Failed to open Windows Defender";
            }
        }
    }
}
