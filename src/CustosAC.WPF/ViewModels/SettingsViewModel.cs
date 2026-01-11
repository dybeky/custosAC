using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CustosAC.WPF.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace CustosAC.WPF.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private const string SettingsFileName = "custosac_settings.json";

    [ObservableProperty]
    private bool _checkUpdatesOnStartup = true;

    [ObservableProperty]
    private bool _autoDownloadUpdates = false;

    [ObservableProperty]
    private bool _deleteAfterUse = false;

    [ObservableProperty]
    private string _currentVersion = "2.1.0";

    public SettingsViewModel()
    {
        LoadSettings();
    }

    private string GetSettingsPath()
    {
        return Path.Combine(AppContext.BaseDirectory, SettingsFileName);
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
                    CheckUpdatesOnStartup = settings.CheckUpdatesOnStartup;
                    AutoDownloadUpdates = settings.AutoDownloadUpdates;
                    DeleteAfterUse = settings.DeleteAfterUse;
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
                CheckUpdatesOnStartup = CheckUpdatesOnStartup,
                AutoDownloadUpdates = AutoDownloadUpdates,
                DeleteAfterUse = DeleteAfterUse
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

    partial void OnCheckUpdatesOnStartupChanged(bool value) => SaveSettings();
    partial void OnAutoDownloadUpdatesChanged(bool value) => SaveSettings();
    partial void OnDeleteAfterUseChanged(bool value) => SaveSettings();

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
            "Are you sure you want to delete custosAC?\n\nThis will close the application and remove all program files.",
            "Confirm Delete",
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
                MessageBox.Show($"Failed to delete program: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
    public bool CheckUpdatesOnStartup { get; set; } = true;
    public bool AutoDownloadUpdates { get; set; } = false;
    public bool DeleteAfterUse { get; set; } = false;
}
