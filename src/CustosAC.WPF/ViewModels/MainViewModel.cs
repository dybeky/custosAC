using System.Diagnostics;
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
    private readonly VersionService _versionService;
    private readonly DispatcherTimer _timer;
    private const string GitHubRepo = "dybeky/custosAC";

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _currentNavigation = "Dashboard";

    [ObservableProperty]
    private string _currentTime = DateTime.Now.ToString("HH:mm:ss");

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _isCheckingUpdate;

    [ObservableProperty]
    private string _displayVersion = "...";

    [ObservableProperty]
    private bool _showUpdateOverlay;

    [ObservableProperty]
    private bool _isUpToDate;

    [ObservableProperty]
    private string _latestVersion = "";

    [ObservableProperty]
    private string? _updateUrl;

    public LocalizationService Localization => LocalizationService.Instance;

    public MainViewModel(AdminService adminService, VersionService versionService)
    {
        _adminService = adminService;
        _versionService = versionService;

        // Show dashboard by default
        CurrentView = App.Services.GetRequiredService<DashboardViewModel>();

        // Update time every second
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => CurrentTime = DateTime.Now.ToString("HH:mm:ss");
        _timer.Start();

        // Fast startup - run version and update check in parallel
        Task.Run(async () =>
        {
            // Start both tasks in parallel
            var versionTask = _versionService.LoadVersionAsync();
            var updateTask = CheckUpdateSilent();

            // Short loading screen (800ms)
            await Task.Delay(800);
            await Application.Current.Dispatcher.InvokeAsync(() => IsLoading = false);

            // Wait for version to complete
            await versionTask;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DisplayVersion = _versionService.Version;
            });

            // Wait for update check and show overlay if needed
            await updateTask;
        });
    }

    private async Task CheckUpdateSilent()
    {
        if (IsCheckingUpdate) return;

        IsCheckingUpdate = true;

        try
        {
            var client = HttpClientService.Instance;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var response = await client.GetAsync($"https://api.github.com/repos/{GitHubRepo}/releases/latest", cts.Token);

            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var latestVersion = doc.RootElement.GetProperty("name").GetString()
                ?? doc.RootElement.GetProperty("tag_name").GetString()
                ?? "N/A";
            var downloadUrl = doc.RootElement.GetProperty("html_url").GetString();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                LatestVersion = latestVersion;
                UpdateUrl = downloadUrl;

                // Compare after version is loaded
                if (!string.IsNullOrEmpty(DisplayVersion) && DisplayVersion != "...")
                {
                    var currentVersion = DisplayVersion.Trim();
                    var latest = latestVersion.Trim();
                    IsUpToDate = string.Equals(currentVersion, latest, StringComparison.OrdinalIgnoreCase);

                    if (!IsUpToDate)
                    {
                        ShowUpdateOverlay = true;
                    }
                }
            });
        }
        catch
        {
            // Silent fail - don't show error on startup
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        CurrentView = App.Services.GetRequiredService<DashboardViewModel>();
        CurrentNavigation = "Dashboard";
    }

    [RelayCommand]
    private void NavigateToScan()
    {
        CurrentView = App.Services.GetRequiredService<ScanViewModel>();
        CurrentNavigation = "Scan";
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentView = App.Services.GetRequiredService<SettingsViewModel>();
        CurrentNavigation = "Settings";
    }

    [RelayCommand]
    private void NavigateToManual()
    {
        CurrentView = App.Services.GetRequiredService<ManualViewModel>();
        CurrentNavigation = "Manual";
    }

    [RelayCommand]
    private void NavigateToUtilities()
    {
        CurrentView = App.Services.GetRequiredService<UtilitiesViewModel>();
        CurrentNavigation = "Utilities";
    }

    [RelayCommand]
    private async Task CheckUpdate()
    {
        if (IsCheckingUpdate) return;

        IsCheckingUpdate = true;

        try
        {
            var client = HttpClientService.Instance;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var response = await client.GetAsync($"https://api.github.com/repos/{GitHubRepo}/releases/latest", cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show(Localization.Strings.NoReleases, Localization.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            // Use "name" field for version display
            var latestVersion = doc.RootElement.GetProperty("name").GetString()
                ?? doc.RootElement.GetProperty("tag_name").GetString()
                ?? "N/A";
            var downloadUrl = doc.RootElement.GetProperty("html_url").GetString();

            LatestVersion = latestVersion;
            UpdateUrl = downloadUrl;

            // Compare versions - check if current version matches latest
            var currentVersion = DisplayVersion.Trim();
            var latest = latestVersion.Trim();

            // Simple comparison - if versions match, user is up to date
            IsUpToDate = string.Equals(currentVersion, latest, StringComparison.OrdinalIgnoreCase);

            // Show overlay
            ShowUpdateOverlay = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{Localization.Strings.Error}: {ex.Message}", Localization.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    [RelayCommand]
    private void OpenUpdate()
    {
        if (UpdateUrl != null)
        {
            using var process = Process.Start(new ProcessStartInfo(UpdateUrl) { UseShellExecute = true });
        }
        ShowUpdateOverlay = false;
    }

    [RelayCommand]
    private void DismissUpdate()
    {
        ShowUpdateOverlay = false;
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
        CurrentNavigation = "Results";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer?.Stop();
        }
        base.Dispose(disposing);
    }
}
