using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Custos.Core.Services;
using Custos.WPF.ViewModels.Base;
using System.Diagnostics;
using System.Text.Json;

namespace Custos.WPF.ViewModels;

public partial class SplashViewModel : ViewModelBase
{
    private const string GitHubRepo = "dybeky/custos";
    private readonly VersionService _versionService;

    [ObservableProperty]
    private string _statusText = "Initializing...";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isIndeterminate = true;

    [ObservableProperty]
    private bool _showUpdatePanel;

    [ObservableProperty]
    private string _currentVersion = "...";

    [ObservableProperty]
    private string _latestVersion = "";

    [ObservableProperty]
    private string? _updateUrl;

    public Action<bool>? CloseRequested { get; set; }

    public LocalizationService Localization => LocalizationService.Instance;

    public SplashViewModel(VersionService versionService)
    {
        _versionService = versionService;
    }

    public async Task<SplashResult> InitializeAndWaitAsync()
    {
        try
        {
            // Stage 1: Initializing
            StatusText = Localization.Strings.Initializing;
            Progress = 0;
            await Task.Delay(400);

            // Stage 2: Loading version
            StatusText = Localization.Strings.LoadingVersion;
            Progress = 30;
            await _versionService.LoadVersionAsync();
            CurrentVersion = _versionService.Version;

            // Stage 3: Checking for updates
            StatusText = Localization.Strings.CheckingForUpdates;
            Progress = 60;
            var updateResult = await CheckForUpdatesAsync();

            // Stage 4: Complete or show update panel
            if (updateResult.HasUpdate)
            {
                LatestVersion = updateResult.LatestVersion;
                UpdateUrl = updateResult.DownloadUrl;
                ShowUpdatePanel = true;
                IsIndeterminate = false;
                Progress = 100;
                StatusText = Localization.Strings.UpdateAvailable;

                // Return that we're showing update panel - window stays open
                return new SplashResult(false, true);
            }
            else
            {
                StatusText = Localization.Strings.Loading;
                Progress = 100;
                IsIndeterminate = false;

                await Task.Delay(300);

                // No update - close and continue
                return new SplashResult(false, false);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Splash initialization error: {ex.Message}");
            // On error - close and continue
            return new SplashResult(false, false);
        }
    }

    private async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        try
        {
            var client = HttpClientService.Instance;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var response = await client.GetAsync(
                $"https://api.github.com/repos/{GitHubRepo}/releases/latest",
                cts.Token);

            if (!response.IsSuccessStatusCode)
                return new UpdateCheckResult(false, "", null);

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(json);

            var latestVersion = doc.RootElement.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString()
                : doc.RootElement.TryGetProperty("tag_name", out var tagElement)
                    ? tagElement.GetString()
                    : "N/A";

            var downloadUrl = doc.RootElement.TryGetProperty("html_url", out var urlElement)
                ? urlElement.GetString()
                : null;

            var hasUpdate = !CompareVersions(CurrentVersion, latestVersion ?? "");

            return new UpdateCheckResult(hasUpdate, latestVersion ?? "", downloadUrl);
        }
        catch
        {
            return new UpdateCheckResult(false, "", null);
        }
    }

    private static bool CompareVersions(string current, string latest)
    {
        current = current.TrimStart('v', 'V').Trim();
        latest = latest.TrimStart('v', 'V').Trim();

        if (Version.TryParse(current, out var currentVer) &&
            Version.TryParse(latest, out var latestVer))
        {
            return currentVer >= latestVer;
        }

        return string.Equals(current, latest, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private void OpenUpdate()
    {
        if (!string.IsNullOrEmpty(UpdateUrl))
        {
            try
            {
                if (Uri.TryCreate(UpdateUrl, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
                {
                    Process.Start(new ProcessStartInfo(UpdateUrl) { UseShellExecute = true });
                }
            }
            catch { }
        }

        // Open main app after opening update page (don't exit)
        CloseRequested?.Invoke(false);
    }

    [RelayCommand]
    private void SkipUpdate()
    {
        CloseRequested?.Invoke(false);
    }
}

public record SplashResult(bool ShouldExit, bool ShowUpdatePanel);
public record UpdateCheckResult(bool HasUpdate, string LatestVersion, string? DownloadUrl);
