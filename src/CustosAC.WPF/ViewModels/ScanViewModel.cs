using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CustosAC.Core.Models;
using CustosAC.Core.Services;
using CustosAC.WPF.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace CustosAC.WPF.ViewModels;

public partial class ScanViewModel : ViewModelBase
{
    private readonly ScannerFactory _scannerFactory;
    private readonly LogService _logService;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private int _totalScanners = 7;

    [ObservableProperty]
    private int _completedScanners;

    [ObservableProperty]
    private int _overallProgress;

    [ObservableProperty]
    private string _currentScannerName = "Ready to scan";

    [ObservableProperty]
    private string _elapsedTime = "00:00";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isReadyToScan = true;

    [ObservableProperty]
    private ObservableCollection<ScannerProgressItem> _scanners = new();

    private DateTime _startTime;
    private System.Windows.Threading.DispatcherTimer? _elapsedTimer;
    private List<(string name, ScanResult result)> _results = new();

    public ScanViewModel(ScannerFactory scannerFactory, LogService logService)
    {
        _scannerFactory = scannerFactory;
        _logService = logService;

        InitializeScanners();
    }

    private void InitializeScanners()
    {
        Scanners.Clear();
        Scanners.Add(new ScannerProgressItem { Name = "AppData", Status = "Pending" });
        Scanners.Add(new ScannerProgressItem { Name = "System Folders", Status = "Pending" });
        Scanners.Add(new ScannerProgressItem { Name = "Prefetch", Status = "Pending" });
        Scanners.Add(new ScannerProgressItem { Name = "Registry", Status = "Pending" });
        Scanners.Add(new ScannerProgressItem { Name = "Steam", Status = "Pending" });
        Scanners.Add(new ScannerProgressItem { Name = "Processes", Status = "Pending" });
        Scanners.Add(new ScannerProgressItem { Name = "Recent Files", Status = "Pending" });
    }

    [RelayCommand]
    private async Task StartScan()
    {
        if (IsScanning) return;

        IsReadyToScan = false;
        IsScanning = true;
        _startTime = DateTime.Now;
        _results.Clear();
        CompletedScanners = 0;
        OverallProgress = 0;

        // Start elapsed timer
        _elapsedTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _elapsedTimer.Tick += (s, e) =>
        {
            var elapsed = DateTime.Now - _startTime;
            ElapsedTime = $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        };
        _elapsedTimer.Start();

        _cts = new CancellationTokenSource();

        try
        {
            var scannersList = new[]
            {
                ("AppData", (Func<Core.Scanner.BaseScannerAsync>)(() => _scannerFactory.CreateAppDataScanner())),
                ("System Folders", () => _scannerFactory.CreateSystemScanner()),
                ("Prefetch", () => _scannerFactory.CreatePrefetchScanner()),
                ("Registry", () => _scannerFactory.CreateRegistryScanner()),
                ("Steam", () => _scannerFactory.CreateSteamScanner()),
                ("Processes", () => _scannerFactory.CreateProcessScanner()),
                ("Recent Files", () => _scannerFactory.CreateRecentFileScanner())
            };

            for (int i = 0; i < scannersList.Length; i++)
            {
                if (_cts.Token.IsCancellationRequested) break;

                var (name, createScanner) = scannersList[i];
                CurrentScannerName = name;

                // Update UI
                var scannerItem = Scanners[i];
                scannerItem.Status = "Scanning...";
                scannerItem.IsActive = true;

                try
                {
                    using var scanner = createScanner();
                    var result = await scanner.ScanAsync(_cts.Token);
                    _results.Add((name, result));

                    scannerItem.FindingsCount = result.Count;
                    scannerItem.Status = result.Count > 0 ? $"{result.Count} found" : "Clean";
                    scannerItem.IsComplete = true;
                }
                catch (OperationCanceledException)
                {
                    scannerItem.Status = "Cancelled";
                    break;
                }
                catch (Exception ex)
                {
                    _logService.LogError($"Scanner {name} failed", ex);
                    scannerItem.Status = "Error";
                    _results.Add((name, new ScanResult { Success = false, Error = ex.Message }));
                }
                finally
                {
                    scannerItem.IsActive = false;
                }

                CompletedScanners = i + 1;
                OverallProgress = (CompletedScanners * 100) / TotalScanners;
            }
        }
        finally
        {
            _elapsedTimer?.Stop();
            IsScanning = false;
            _cts?.Dispose();
            _cts = null;
        }

        // Navigate to results
        CurrentScannerName = "Complete!";
        await Task.Delay(500);

        var mainVm = App.Services.GetRequiredService<MainViewModel>();
        mainVm.ShowResultsView(_results);
    }

    [RelayCommand]
    private void CancelScan()
    {
        _cts?.Cancel();
    }
}

public partial class ScannerProgressItem : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _status = "Pending";

    [ObservableProperty]
    private int _findingsCount;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _isComplete;
}
