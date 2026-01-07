using System.Diagnostics;
using CustosAC.Abstractions;
using CustosAC.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustosAC.Services;

/// <summary>
/// Реализация сервиса работы с реестром Windows
/// </summary>
public class RegistryService : IRegistryService
{
    private readonly IProcessService _processService;
    private readonly ILogger<RegistryService> _logger;
    private readonly AppSettings _settings;

    public RegistryService(
        IProcessService processService,
        ILogger<RegistryService> logger,
        IOptions<AppSettings> settings)
    {
        _processService = processService;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<bool> ExportKeyAsync(string keyPath, string outputFile, CancellationToken ct = default)
    {
        try
        {
            var args = $"export \"{keyPath}\" \"{outputFile}\" /y";
            return await _processService.RunCommandAsync("reg", args, _settings.Timeouts.DefaultProcessTimeoutMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export registry key: {KeyPath}", keyPath);
            return false;
        }
    }

    public async Task<bool> DeleteValueAsync(string keyPath, string valueName, CancellationToken ct = default)
    {
        try
        {
            var args = $"delete \"{keyPath}\" /v \"{valueName}\" /f";
            return await _processService.RunCommandAsync("reg", args, _settings.Timeouts.DefaultProcessTimeoutMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete registry value: {KeyPath}\\{ValueName}", keyPath, valueName);
            return false;
        }
    }

    public async Task<bool> DeleteKeyAsync(string keyPath, CancellationToken ct = default)
    {
        try
        {
            var args = $"delete \"{keyPath}\" /f";
            return await _processService.RunCommandAsync("reg", args, _settings.Timeouts.DefaultProcessTimeoutMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete registry key: {KeyPath}", keyPath);
            return false;
        }
    }

    public async Task OpenRegistryEditorAsync(string keyPath)
    {
        try
        {
            // Копируем путь в буфер обмена
            await _processService.CopyToClipboardAsync(keyPath);

            // Открываем regedit
            var psi = new ProcessStartInfo
            {
                FileName = "regedit",
                UseShellExecute = true
            };

            var process = Process.Start(psi);
            if (process != null)
            {
                _processService.TrackProcess(process);
            }

            _logger.LogInformation("Opened registry editor, path copied to clipboard: {KeyPath}", keyPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open registry editor");
        }
    }

    public async Task<bool> KeyExistsAsync(string keyPath, CancellationToken ct = default)
    {
        try
        {
            var args = $"query \"{keyPath}\"";
            return await _processService.RunCommandAsync("reg", args, _settings.Timeouts.DefaultProcessTimeoutMs);
        }
        catch
        {
            return false;
        }
    }
}
