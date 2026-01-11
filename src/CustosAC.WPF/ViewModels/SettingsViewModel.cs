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
        LoadVersionAsync();
    }

    private async void LoadVersionAsync()
    {
        await _versionService.LoadVersionAsync();
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            CurrentVersion = _versionService.Version;
        });
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
        Process.Start(new ProcessStartInfo("https://github.com/dybeky/custosAC") { UseShellExecute = true });
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
                // Create a batch script to delete the program after it closes
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (exePath != null)
                {
                    var exeDir = Path.GetDirectoryName(exePath);
                    var batchPath = Path.Combine(Path.GetTempPath(), "custosac_uninstall.bat");

                    var batchContent = $@"
@echo off
timeout /t 2 /nobreak >nul
rd /s /q ""{exeDir}""
del ""%~f0""
";
                    File.WriteAllText(batchPath, batchContent);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = batchPath,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    });

                    Application.Current.Shutdown();
                }
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
