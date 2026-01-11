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
    private readonly VersionService _versionService;
    private readonly DispatcherTimer _timer;
    private const string GitHubRepo = "dybeky/custosAC";

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _currentTime = DateTime.Now.ToString("HH:mm");

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
        _timer.Tick += (s, e) => CurrentTime = DateTime.Now.ToString("HH:mm");
        _timer.Start();

        // Hide loading after short delay and load version
        Task.Run(async () =>
        {
            await Task.Delay(1500);
            await Application.Current.Dispatcher.InvokeAsync(() => IsLoading = false);

            // Load version using centralized service
            await _versionService.LoadVersionAsync();
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DisplayVersion = _versionService.Version;
            });

            // Auto-check for updates on startup
            await Task.Delay(500);
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await CheckUpdateSilent();
            });
        });
    }

    private async Task CheckUpdateSilent()
    {
        if (IsCheckingUpdate) return;

        IsCheckingUpdate = true;

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "custosAC");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            client.Timeout = TimeSpan.FromSeconds(30);

            var response = await client.GetAsync($"https://api.github.com/repos/{GitHubRepo}/releases/latest");

            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var latestVersion = doc.RootElement.GetProperty("name").GetString()
                ?? doc.RootElement.GetProperty("tag_name").GetString()
                ?? "N/A";
            var downloadUrl = doc.RootElement.GetProperty("html_url").GetString();

            LatestVersion = latestVersion;
            UpdateUrl = downloadUrl;

            var currentVersion = DisplayVersion.Trim();
            var latest = latestVersion.Trim();

            IsUpToDate = string.Equals(currentVersion, latest, StringComparison.OrdinalIgnoreCase);

            // Only show overlay if update is available
            if (!IsUpToDate)
            {
                ShowUpdateOverlay = true;
            }
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
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            client.Timeout = TimeSpan.FromSeconds(30);

            var response = await client.GetAsync($"https://api.github.com/repos/{GitHubRepo}/releases/latest");

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
            Process.Start(new ProcessStartInfo(UpdateUrl) { UseShellExecute = true });
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
    }
}
