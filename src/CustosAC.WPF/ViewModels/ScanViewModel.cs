using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CustosAC.Core.Models;
using CustosAC.Core.Services;
using CustosAC.WPF.ViewModels.Base;
using Microsoft.Win32;

namespace CustosAC.WPF.ViewModels;

public partial class ScanViewModel : ViewModelBase
{
    private readonly ScannerFactory _scannerFactory;
    private readonly LogService _logService;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private int _totalScanners;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isReadyToScan = true;

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

        _cts = new CancellationTokenSource();
        bool wasCancelled = false;

        try
        {
            var scanners = _scannerFactory.CreateAllScanners().ToList();

            foreach (var scanner in scanners)
            {
                if (_cts.Token.IsCancellationRequested)
                {
                    wasCancelled = true;
                    break;
                }

                try
                {
                    using (scanner)
                    {
                        var result = await scanner.ScanAsync(_cts.Token);
                        _results.Add((scanner.GetType().Name.Replace("Async", "").Replace("Scanner", ""), result));
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
            }
        }
        finally
        {
            IsScanning = false;
            _cts?.Dispose();
            _cts = null;
        }

        // Save results to file only if not cancelled
        if (!wasCancelled && _results.Count > 0)
        {
            await SaveResultsToFile();
        }

        // Return to ready state
        IsReadyToScan = true;
    }

    private async Task SaveResultsToFile()
    {
        var saveDialog = new SaveFileDialog
        {
            Title = Localization.CurrentLanguage == "ru" ? "Сохранить результаты" : "Save Results",
            Filter = "Text files (*.txt)|*.txt",
            DefaultExt = "txt",
            FileName = $"custosAC_scan_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt"
        };

        if (saveDialog.ShowDialog() == true)
        {
            var sb = new StringBuilder();

            // Simple clean header
            sb.AppendLine("==============================================================");
            sb.AppendLine("                    custosAC Scan Results                     ");
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
