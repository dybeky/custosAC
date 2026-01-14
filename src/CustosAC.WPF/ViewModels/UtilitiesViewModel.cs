using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CustosAC.Core.Services;
using CustosAC.WPF.ViewModels.Base;

namespace CustosAC.WPF.ViewModels;

public partial class UtilitiesViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "";

    public LocalizationService Localization => LocalizationService.Instance;

    // LastActivityView - NirSoft
    [RelayCommand]
    private void OpenLastActivityView()
    {
        OpenUrl("https://www.nirsoft.net/utils/computer_activity_view.html", "LastActivityView");
    }

    // USB Deview - NirSoft
    [RelayCommand]
    private void OpenUsbDeview()
    {
        OpenUrl("https://www.nirsoft.net/utils/usb_devices_view.html", "USB Deview");
    }

    // Everything - voidtools
    [RelayCommand]
    private void OpenEverything()
    {
        OpenUrl("https://www.voidtools.com/", "Everything");
    }

    // System Informer
    [RelayCommand]
    private void OpenSystemInformer()
    {
        OpenUrl("https://systeminformer.sourceforge.io/", "System Informer");
    }

    private void OpenUrl(string url, string name)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            StatusMessage = $"Opening {name}...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open {name}: {ex.Message}";
        }
    }
}
