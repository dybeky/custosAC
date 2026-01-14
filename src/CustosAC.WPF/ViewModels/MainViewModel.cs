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
        _timer.Tick += TimerTickHandler;
        _timer.Start();

        // Fast startup - run version and update check in parallel
        Task.Run(async () =>
        {
            // Start both tasks in parallel
            var versionTask = _versionService.LoadVersionAsync();
            var updateTask = CheckUpdateSilent();

            // Short loading screen (800ms)
            await Task.Delay(800);

            // Null-check for dispatcher to prevent crash during shutdown
            if (Application.Current?.Dispatcher != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() => IsLoading = false);
            }

            // Wait for version to complete
            await versionTask;

            if (Application.Current?.Dispatcher != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    DisplayVersion = _versionService.Version;
                });
            }

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

            // Null-check for dispatcher to prevent crash during shutdown
            if (Application.Current?.Dispatcher != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    LatestVersion = latestVersion;
                    UpdateUrl = downloadUrl;

                    // Compare after version is loaded
                    if (!string.IsNullOrEmpty(DisplayVersion) && DisplayVersion != "...")
                    {
                        var currentVersion = DisplayVersion.Trim();
                        var latest = latestVersion.Trim();
                        IsUpToDate = CompareVersions(currentVersion, latest);

                        if (!IsUpToDate)
                        {
                            ShowUpdateOverlay = true;
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            // Log error but don't show to user on startup
            System.Diagnostics.Debug.WriteLine($"Failed to check for updates silently: {ex.Message}");
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

            // Compare versions using semantic comparison
            var currentVersion = DisplayVersion.Trim();
            var latest = latestVersion.Trim();

            IsUpToDate = CompareVersions(currentVersion, latest);

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
        if (!string.IsNullOrEmpty(UpdateUrl))
        {
            try
            {
                // Validate URL before opening
                if (Uri.TryCreate(UpdateUrl, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
                {
                    using var process = Process.Start(new ProcessStartInfo(UpdateUrl) { UseShellExecute = true });
                }
            }
            catch
            {
                // Silently ignore if URL can't be opened
            }
        }

        // Use dispatcher to safely hide overlay
        Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
        {
            ShowUpdateOverlay = false;
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    [RelayCommand]
    private void DismissUpdate()
    {
        // Simply set to false - XAML animation will handle smooth close
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

    private void TimerTickHandler(object? sender, EventArgs e)
    {
        CurrentTime = DateTime.Now.ToString("HH:mm:ss");
    }

    /// <summary>
    /// Compare two version strings semantically.
    /// Returns true if current version >= latest version (user is up to date).
    /// </summary>
    private static bool CompareVersions(string current, string latest)
    {
        // Remove common prefixes like "v" or "V"
        current = current.TrimStart('v', 'V').Trim();
        latest = latest.TrimStart('v', 'V').Trim();

        // Try to parse as Version for semantic comparison
        if (Version.TryParse(current, out var currentVer) && Version.TryParse(latest, out var latestVer))
        {
            return currentVer >= latestVer;
        }

        // Fallback to string comparison
        return string.Equals(current, latest, StringComparison.OrdinalIgnoreCase);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_timer != null)
            {
                _timer.Tick -= TimerTickHandler; // Unsubscribe from event
                _timer.Stop();
            }
        }
        base.Dispose(disposing);
    }
}
