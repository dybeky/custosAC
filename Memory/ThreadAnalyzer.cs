using System.Diagnostics;
using System.Runtime.InteropServices;
using CustosAC.UI;
using CustosAC.WinAPI;

namespace CustosAC.Memory;

/// <summary>
/// Анализатор потоков процессов
/// </summary>
public static class ThreadAnalyzer
{
    #region Result Classes

    public class ThreadInfo
    {
        public uint ThreadId { get; set; }
        public uint OwnerProcessId { get; set; }
        public int BasePriority { get; set; }
        public bool IsSuspicious { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public class ThreadAnalysisResult
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = "";
        public List<ThreadInfo> Threads { get; set; } = new();
        public List<ThreadInfo> SuspiciousThreads { get; set; } = new();
        public int TotalThreads { get; set; }
        public int SuspiciousScore { get; set; }
    }

    #endregion

    /// <summary>
    /// Анализ потоков процесса
    /// </summary>
    public static ThreadAnalysisResult AnalyzeProcessThreads(int processId)
    {
        var result = new ThreadAnalysisResult { ProcessId = processId };

        try
        {
            var process = Process.GetProcessById(processId);
            result.ProcessName = process.ProcessName;
        }
        catch
        {
            return result;
        }

        // Получаем список всех потоков
        IntPtr snapshot = NativeMethods.CreateToolhelp32Snapshot(NativeMethods.TH32CS_SNAPTHREAD, 0);

        if (snapshot == NativeMethods.INVALID_HANDLE_VALUE)
            return result;

        try
        {
            var threadEntry = new NativeMethods.THREADENTRY32();
            threadEntry.dwSize = (uint)Marshal.SizeOf<NativeMethods.THREADENTRY32>();

            if (NativeMethods.Thread32First(snapshot, ref threadEntry))
            {
                do
                {
                    if (threadEntry.th32OwnerProcessID == processId)
                    {
                        var thread = new ThreadInfo
                        {
                            ThreadId = threadEntry.th32ThreadID,
                            OwnerProcessId = threadEntry.th32OwnerProcessID,
                            BasePriority = threadEntry.tpBasePri
                        };

                        result.Threads.Add(thread);
                        result.TotalThreads++;
                    }
                }
                while (NativeMethods.Thread32Next(snapshot, ref threadEntry));
            }
        }
        finally
        {
            NativeMethods.CloseHandle(snapshot);
        }

        // Получаем модули процесса для проверки адресов
        var modules = InjectionDetector.GetProcessModules(processId);

        // Проверяем потоки на подозрительность
        // (базовые проверки без глубокого анализа стартовых адресов)
        foreach (var thread in result.Threads)
        {
            bool suspicious = false;

            // Проверка на необычный приоритет
            if (thread.BasePriority > 15 || thread.BasePriority < -15)
            {
                thread.Warnings.Add($"Необычный приоритет: {thread.BasePriority}");
                suspicious = true;
            }

            if (suspicious)
            {
                thread.IsSuspicious = true;
                result.SuspiciousThreads.Add(thread);
                result.SuspiciousScore += 10;
            }
        }

        // Проверка на слишком много потоков (может указывать на threading-based чит)
        if (result.TotalThreads > 100)
        {
            result.SuspiciousScore += 5;
        }

        return result;
    }

    /// <summary>
    /// Сканирование потоков всех процессов
    /// </summary>
    public static void ScanAllProcesses()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ АНАЛИЗ ПОТОКОВ ПРОЦЕССОВ ═══{ConsoleUI.ColorReset}\n");

        var processThreadCounts = new List<(string name, int pid, int threads)>();
        var suspiciousProcesses = new List<ThreadAnalysisResult>();
        int totalThreads = 0;

        var processes = Process.GetProcesses();
        int currentPid = Environment.ProcessId;

        // Исключаем системные процессы
        var excludedNames = new[] { "System", "Idle", "Registry", "smss", "csrss" };

        ConsoleUI.Log($"+ Анализ потоков в {processes.Length} процессах...", true);

        foreach (var process in processes)
        {
            try
            {
                if (process.Id == currentPid ||
                    process.Id <= 4 ||
                    excludedNames.Any(n => process.ProcessName.Equals(n, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var analysis = AnalyzeProcessThreads(process.Id);
                totalThreads += analysis.TotalThreads;

                processThreadCounts.Add((analysis.ProcessName, analysis.ProcessId, analysis.TotalThreads));

                if (analysis.SuspiciousScore > 0)
                {
                    suspiciousProcesses.Add(analysis);
                }
            }
            catch
            {
                // Игнорируем недоступные процессы
            }
        }

        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Статистика потоков:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  Всего потоков в системе: {totalThreads}");
        Console.WriteLine($"  Процессов проанализировано: {processThreadCounts.Count}");

        // Топ процессов по количеству потоков
        var topByThreads = processThreadCounts.OrderByDescending(p => p.threads).Take(10).ToList();

        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Топ-10 процессов по количеству потоков:{ConsoleUI.ColorReset}");
        foreach (var (name, pid, threads) in topByThreads)
        {
            string color = threads > 100 ? ConsoleUI.ColorOrange :
                          threads > 50 ? ConsoleUI.ColorYellow :
                          ConsoleUI.ColorReset;
            Console.WriteLine($"  {color}{threads,4} потоков - {name} (PID: {pid}){ConsoleUI.ColorReset}");
        }

        // Подозрительные процессы
        if (suspiciousProcesses.Count > 0)
        {
            Console.WriteLine($"\n{ConsoleUI.ColorOrange}{ConsoleUI.ColorBold}══ ПРОЦЕССЫ С ПОДОЗРИТЕЛЬНЫМИ ПОТОКАМИ: {suspiciousProcesses.Count} ══{ConsoleUI.ColorReset}\n");

            foreach (var proc in suspiciousProcesses.OrderByDescending(p => p.SuspiciousScore))
            {
                Console.WriteLine($"{ConsoleUI.ColorOrange}► {proc.ProcessName} (PID: {proc.ProcessId}){ConsoleUI.ColorReset}");
                Console.WriteLine($"  Потоков: {proc.TotalThreads}");
                Console.WriteLine($"  Подозрительность: {proc.SuspiciousScore}");

                foreach (var thread in proc.SuspiciousThreads)
                {
                    Console.WriteLine($"  Поток {thread.ThreadId}:");
                    foreach (var warning in thread.Warnings)
                    {
                        Console.WriteLine($"    - {warning}");
                    }
                }
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine($"\n{ConsoleUI.ColorGreen}+ Подозрительных потоков не обнаружено{ConsoleUI.ColorReset}");
        }

        ConsoleUI.Pause();
    }
}
