using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CustosAC.Core.Services;
using CustosAC.WPF.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace CustosAC.WPF.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private const string SettingsFileName = "custosac_settings.json";
    private readonly VersionService _versionService;

    [ObservableProperty]
    private bool _deleteAfterUse = false;

    [ObservableProperty]
    private string _currentVersion = "Loading...";

    [ObservableProperty]
    private int _selectedLanguageIndex = 0;

    public LocalizationService Localization => LocalizationService.Instance;

    public SettingsViewModel(VersionService versionService)
    {
        _versionService = versionService;
        LoadSettings();

        // Start loading version asynchronously
        _ = LoadVersionAsync();
    }

    private async Task LoadVersionAsync()
    {
        try
        {
            await _versionService.LoadVersionAsync();

            // Null-check for dispatcher to prevent crash during shutdown
            if (Application.Current?.Dispatcher != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    CurrentVersion = _versionService.Version;
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SettingsViewModel.LoadVersionAsync failed: {ex.Message}");
        }
    }

    private string GetSettingsPath()
    {
        return Path.Combine(Path.GetTempPath(), SettingsFileName);
    }

    private void LoadSettings()
    {
        try
        {
            var path = GetSettingsPath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var settings = System.Text.Json.JsonSerializer.Deserialize<SettingsData>(json);
                if (settings != null)
                {
                    DeleteAfterUse = settings.DeleteAfterUse;
                    SelectedLanguageIndex = settings.Language == "ru" ? 1 : 0;
                    LocalizationService.Instance.CurrentLanguage = settings.Language ?? "en";
                }
            }
        }
        catch
        {
            // Use defaults on error
        }
    }

    private void SaveSettings()
    {
        try
        {
            var settings = new SettingsData
            {
                DeleteAfterUse = DeleteAfterUse,
                Language = LocalizationService.Instance.CurrentLanguage
            };
            var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(GetSettingsPath(), json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    partial void OnDeleteAfterUseChanged(bool value) => SaveSettings();

    partial void OnSelectedLanguageIndexChanged(int value)
    {
        LocalizationService.Instance.CurrentLanguage = value == 1 ? "ru" : "en";
        SaveSettings();
        OnPropertyChanged(nameof(Localization));
    }

    [RelayCommand]
    private void CheckForUpdates()
    {
        var mainVm = App.Services.GetRequiredService<MainViewModel>();
        mainVm.CheckUpdateCommand.Execute(null);
    }

    [RelayCommand]
    private void OpenGitHub()
    {
        using var process = Process.Start(new ProcessStartInfo("https://github.com/dybeky/custosAC") { UseShellExecute = true });
    }

    [RelayCommand]
    private void DeleteProgram()
    {
        var result = MessageBox.Show(
            Localization.Strings.ConfirmDelete,
            Localization.Strings.ConfirmDeleteTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                // Get current process path safely
                string? exePath;
                using (var currentProcess = Process.GetCurrentProcess())
                {
                    exePath = currentProcess.MainModule?.FileName;
                }

                if (string.IsNullOrEmpty(exePath))
                {
                    MessageBox.Show("Cannot determine application path.", Localization.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var exeDir = Path.GetDirectoryName(exePath);
                if (string.IsNullOrEmpty(exeDir))
                {
                    MessageBox.Show("Cannot determine application directory.", Localization.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Validate the path is safe to delete
                var fullPath = Path.GetFullPath(exeDir);
                var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                // Prevent deletion of system directories
                if (fullPath.StartsWith(systemRoot, StringComparison.OrdinalIgnoreCase) ||
                    fullPath.Equals(programFiles, StringComparison.OrdinalIgnoreCase) ||
                    fullPath.Equals(programFilesX86, StringComparison.OrdinalIgnoreCase) ||
                    fullPath.Length <= 3) // Prevent deletion of drive root
                {
                    MessageBox.Show("Cannot delete from protected system directory.", Localization.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Escape special characters for batch script
                var escapedDir = fullPath.Replace("^", "^^").Replace("&", "^&").Replace("<", "^<").Replace(">", "^>").Replace("|", "^|");

                var batchPath = Path.Combine(Path.GetTempPath(), $"custosac_uninstall_{Guid.NewGuid():N}.bat");

                var batchContent = $@"@echo off
timeout /t 2 /nobreak >nul
rd /s /q ""{escapedDir}""
del ""%~f0""
";
                File.WriteAllText(batchPath, batchContent);

                // Process needs to run independently after app closes - don't dispose
                _ = Process.Start(new ProcessStartInfo
                {
                    FileName = batchPath,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });

                Application.Current?.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Localization.Strings.DeleteFailed} {ex.Message}", Localization.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        var mainVm = App.Services.GetRequiredService<MainViewModel>();
        mainVm.NavigateToDashboardCommand.Execute(null);
    }
}

public class SettingsData
{
    public bool DeleteAfterUse { get; set; } = false;
    public string Language { get; set; } = "en";
}
