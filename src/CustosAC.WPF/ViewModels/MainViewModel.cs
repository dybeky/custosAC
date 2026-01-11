using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CustosAC.Core.Services;
using CustosAC.WPF.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace CustosAC.WPF.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly AdminService _adminService;
    private readonly DispatcherTimer _timer;
    private const string GitHubRepo = "dybeky/custosAC";
    private const string CurrentVersion = "2.1.0";

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _currentTime = DateTime.Now.ToString("HH:mm");

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _isCheckingUpdate;

    public MainViewModel(AdminService adminService)
    {
        _adminService = adminService;

        // Show dashboard by default
        CurrentView = App.Services.GetRequiredService<DashboardViewModel>();

        // Update time every second
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => CurrentTime = DateTime.Now.ToString("HH:mm");
        _timer.Start();

        // Hide loading after short delay and check for updates
        Task.Run(async () =>
        {
            await Task.Delay(1500);
            await Application.Current.Dispatcher.InvokeAsync(() => IsLoading = false);

            // Auto-check updates on startup (silent mode - only notify if update found)
            await CheckUpdateSilent();
        });
    }

    private async Task CheckUpdateSilent()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "custosAC");
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetStringAsync($"https://api.github.com/repos/{GitHubRepo}/releases/latest");
            using var doc = JsonDocument.Parse(response);

            var latestVersion = doc.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "0.0.0";
            var downloadUrl = doc.RootElement.GetProperty("html_url").GetString();

            if (string.Compare(latestVersion, CurrentVersion, StringComparison.OrdinalIgnoreCase) > 0)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var result = MessageBox.Show(
                        $"New version available: v{latestVersion}\nCurrent version: v{CurrentVersion}\n\nOpen download page?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes && downloadUrl != null)
                    {
                        Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });
                    }
                });
            }
        }
        catch
        {
            // Silently ignore update check errors on startup
        }
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        CurrentView = App.Services.GetRequiredService<DashboardViewModel>();
    }

    [RelayCommand]
    private void NavigateToScan()
    {
        CurrentView = App.Services.GetRequiredService<ScanViewModel>();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentView = App.Services.GetRequiredService<SettingsViewModel>();
    }

    [RelayCommand]
    private void NavigateToManual()
    {
        CurrentView = App.Services.GetRequiredService<ManualViewModel>();
    }

    [RelayCommand]
    private void NavigateToUtilities()
    {
        CurrentView = App.Services.GetRequiredService<UtilitiesViewModel>();
    }

    [RelayCommand]
    private async Task CheckUpdate()
    {
        if (IsCheckingUpdate) return;

        IsCheckingUpdate = true;

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "custosAC");
            client.Timeout = TimeSpan.FromSeconds(30);

            var response = await client.GetStringAsync($"https://api.github.com/repos/{GitHubRepo}/releases/latest");
            using var doc = JsonDocument.Parse(response);

            var latestVersion = doc.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "0.0.0";
            var downloadUrl = doc.RootElement.GetProperty("html_url").GetString();

            if (string.Compare(latestVersion, CurrentVersion, StringComparison.OrdinalIgnoreCase) > 0)
            {
                var result = MessageBox.Show(
                    $"New version available: v{latestVersion}\nCurrent version: v{CurrentVersion}\n\nOpen download page?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes && downloadUrl != null)
                {
                    Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });
                }
            }
            else
            {
                MessageBox.Show($"You have the latest version (v{CurrentVersion})", "No Updates", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to check for updates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    public void ShowScanView()
    {
        NavigateToScan();
    }

    public void ShowResultsView(List<(string name, Core.Models.ScanResult result)> results)
    {
        var vm = App.Services.GetRequiredService<ResultsViewModel>();
        vm.LoadResults(results);
        CurrentView = vm;
    }
}
