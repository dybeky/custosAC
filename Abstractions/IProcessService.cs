using System.Diagnostics;

namespace CustosAC.Abstractions;

/// <summary>
/// Интерфейс для работы с процессами
/// </summary>
public interface IProcessService
{
    /// <summary>Отслеживать процесс</summary>
    void TrackProcess(Process process);

    /// <summary>Прекратить отслеживание процесса</summary>
    void UntrackProcess(Process process);

    /// <summary>Завершить все отслеживаемые процессы</summary>
    void KillAllTrackedProcesses();

    /// <summary>Запустить команду и дождаться завершения</summary>
    Task<bool> RunCommandAsync(string command, string? args = null, int timeoutMs = 10000);

    /// <summary>Запустить процесс</summary>
    Task<Process?> StartProcessAsync(ProcessStartInfo psi);

    /// <summary>Запустить отслеживаемый процесс</summary>
    Task<Process?> StartTrackedProcessAsync(string fileName, string? arguments = null);

    /// <summary>Открыть папку в проводнике</summary>
    Task OpenFolderAsync(string path);

    /// <summary>Открыть URL в браузере</summary>
    Task OpenUrlAsync(string url);

    /// <summary>Скопировать текст в буфер обмена</summary>
    Task CopyToClipboardAsync(string text);
}
