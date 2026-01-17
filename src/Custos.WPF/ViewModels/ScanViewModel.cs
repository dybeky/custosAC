using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Custos.Core.Models;
using Custos.Core.Services;
using Custos.WPF.ViewModels.Base;
using Microsoft.Win32;

namespace Custos.WPF.ViewModels;

public partial class ScanViewModel : ViewModelBase
{
    private readonly ScannerFactory _scannerFactory;
    private readonly LogService _logService;
    private CancellationTokenSource? _cts;

    private const double ProgressBarMaxWidth = 260;

    [ObservableProperty]
    private int _totalScanners;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isReadyToScan = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScanProgressWidth))]
    private double _scanProgress;

    [ObservableProperty]
    private int _completedScanners;

    [ObservableProperty]
    private string _currentScannerName = "";

    public double ScanProgressWidth => ScanProgress * ProgressBarMaxWidth;

    public LocalizationService Localization => LocalizationService.Instance;

    private List<(string name, ScanResult result)> _results = new();

    public ScanViewModel(ScannerFactory scannerFactory, LogService logService)
    {
        _scannerFactory = scannerFactory;
        _logService = logService;
        TotalScanners = _scannerFactory.GetScannerCount();
    }

    [RelayCommand]
    private async Task StartScan()
    {
        if (IsScanning) return;

        IsReadyToScan = false;
        IsScanning = true;
        _results.Clear();
        ScanProgress = 0;
        CompletedScanners = 0;
        CurrentScannerName = "";

        _cts = new CancellationTokenSource();
        bool wasCancelled = false;

        try
        {
            var scanners = _scannerFactory.CreateAllScanners().ToList();
            var totalCount = scanners.Count;

            for (int i = 0; i < scanners.Count; i++)
            {
                var scanner = scanners[i];

                if (_cts.Token.IsCancellationRequested)
                {
                    wasCancelled = true;
                    break;
                }

                CurrentScannerName = scanner.GetType().Name.Replace("Async", "").Replace("Scanner", "");

                try
                {
                    using (scanner)
                    {
                        var result = await scanner.ScanAsync(_cts.Token).ConfigureAwait(false);
                        _results.Add((CurrentScannerName, result));
                    }
                }
                catch (OperationCanceledException)
                {
                    wasCancelled = true;
                    break;
                }
                catch (Exception ex)
                {
                    _logService.LogError($"Scanner failed", ex);
                }

                CompletedScanners = i + 1;
                ScanProgress = (double)(i + 1) / totalCount;
            }
        }
        finally
        {
            IsScanning = false;
            CurrentScannerName = "";
            _cts?.Dispose();
            _cts = null;
        }

        if (!wasCancelled && _results.Count > 0)
        {
            await SaveResultsToFile();
        }

        IsReadyToScan = true;
    }

    private async Task SaveResultsToFile()
    {
        var saveDialog = new SaveFileDialog
        {
            Title = Localization.CurrentLanguage == "ru" ? "Сохранить результаты" : "Save Results",
            Filter = "Text files (*.txt)|*.txt",
            DefaultExt = "txt",
            FileName = $"custos_scan_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt"
        };

        if (saveDialog.ShowDialog() == true)
        {
            var sb = new StringBuilder();

            sb.AppendLine("==============================================================");
            sb.AppendLine("                    custos Scan Results                     ");
            sb.AppendLine($"                     {DateTime.Now:yyyy-MM-dd HH:mm:ss}                      ");
            sb.AppendLine("==============================================================");
            sb.AppendLine();

            int totalFindings = 0;

            foreach (var (name, result) in _results)
            {
                if (!result.Success)
                {
                    sb.AppendLine($"[ERROR] {name}: {result.Error}");
                    continue;
                }

                if (result.Count > 0)
                {
                    sb.AppendLine($"--- {name} ({result.Count} findings) ---");
                    sb.AppendLine();

                    foreach (var finding in result.Findings)
                    {
                        sb.AppendLine($"  * {finding}");
                    }
                    sb.AppendLine();

                    totalFindings += result.Count;
                }
            }

            sb.AppendLine("==============================================================");
            sb.AppendLine($"Total findings: {totalFindings}");
            sb.AppendLine("==============================================================");

            await File.WriteAllTextAsync(saveDialog.FileName, sb.ToString());
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        _cts?.Cancel();
    }
}
