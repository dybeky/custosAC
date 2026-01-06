using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using CustosAC.UI;
using CustosAC.WinAPI;
using CustosAC.Scanner;

namespace CustosAC.ProcessAnalysis;

/// <summary>
/// Анализатор запущенных процессов
/// </summary>
public static class ProcessAnalyzer
{
    #region Result Classes

    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public int ParentProcessId { get; set; }
        public string Name { get; set; } = "";
        public string? Path { get; set; }
        public string? CommandLine { get; set; }
        public string? ParentName { get; set; }
        public DateTime StartTime { get; set; }
        public long WorkingSet { get; set; }
        public int ThreadCount { get; set; }
        public bool IsSuspicious { get; set; }
        public List<string> Warnings { get; set; } = new();
        public int SuspiciousScore { get; set; }
    }

    #endregion

    // Известные легитимные родительские процессы
    private static readonly Dictionary<string, string[]> LegitParentChild = new()
    {
        ["explorer"] = new[] { "*" }, // Explorer может запускать что угодно
        ["services"] = new[] { "svchost", "spoolsv", "lsass", "searchindexer" },
        ["svchost"] = new[] { "wuauclt", "taskhostw", "sihost", "ctfmon" },
        ["cmd"] = new[] { "*" },
        ["powershell"] = new[] { "*" },
        ["userinit"] = new[] { "explorer" }
    };

    // Подозрительные имена процессов
    private static readonly string[] SuspiciousProcessNames = new[]
    {
        "inject", "hook", "cheat", "hack", "bypass", "loader",
        "external", "internal", "esp", "aimbot", "trigger",
        "mapper", "driver", "kdmapper", "vulnerable"
    };

    /// <summary>
    /// Получение информации о процессе
    /// </summary>
    public static ProcessInfo GetProcessInfo(int processId)
    {
        var info = new ProcessInfo { ProcessId = processId };

        try
        {
            var process = System.Diagnostics.Process.GetProcessById(processId);
            info.Name = process.ProcessName;
            info.ThreadCount = process.Threads.Count;
            info.WorkingSet = process.WorkingSet64;

            try
            {
                info.Path = process.MainModule?.FileName;
                info.StartTime = process.StartTime;
            }
            catch
            {
                // Некоторые процессы не дают доступ к этой информации
            }
        }
        catch
        {
            return info;
        }

        // Получаем Parent PID
        info.ParentProcessId = GetParentProcessId(processId);

        if (info.ParentProcessId > 0)
        {
            try
            {
                var parent = System.Diagnostics.Process.GetProcessById(info.ParentProcessId);
                info.ParentName = parent.ProcessName;
            }
            catch
            {
                info.ParentName = "(завершен)";
            }
        }

        return info;
    }

    /// <summary>
    /// Получение PID родительского процесса
    /// </summary>
    public static int GetParentProcessId(int processId)
    {
        IntPtr handle = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)processId);
        if (handle == IntPtr.Zero)
            return 0;

        try
        {
            var pbi = new NativeMethods.PROCESS_BASIC_INFORMATION();
            int status = NativeMethods.NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out _);

            if (status == 0)
            {
                return (int)pbi.InheritedFromUniqueProcessId.ToInt64();
            }
        }
        finally
        {
            NativeMethods.CloseHandle(handle);
        }

        return 0;
    }

    /// <summary>
    /// Анализ всех запущенных процессов
    /// </summary>
    public static List<ProcessInfo> AnalyzeAllProcesses()
    {
        var results = new List<ProcessInfo>();
        var processes = System.Diagnostics.Process.GetProcesses();

        // Загружаем базу сигнатур
        HashScanner.LoadDatabase();

        foreach (var process in processes)
        {
            try
            {
                if (process.Id <= 4) continue; // Системные процессы

                var info = GetProcessInfo(process.Id);
                AnalyzeProcessSuspiciousness(info);
                results.Add(info);
            }
            catch
            {
                // Игнорируем недоступные процессы
            }
        }

        return results;
    }

    /// <summary>
    /// Анализ подозрительности процесса
    /// </summary>
    private static void AnalyzeProcessSuspiciousness(ProcessInfo info)
    {
        string lowerName = info.Name.ToLowerInvariant();

        // Проверка 1: Подозрительное имя
        foreach (var suspicious in SuspiciousProcessNames)
        {
            if (lowerName.Contains(suspicious))
            {
                info.Warnings.Add($"Подозрительное имя: содержит '{suspicious}'");
                info.SuspiciousScore += 20;
                info.IsSuspicious = true;
                break;
            }
        }

        // Проверка 2: Запущен из временной папки
        if (!string.IsNullOrEmpty(info.Path))
        {
            if (info.Path.Contains(@"\Temp\", StringComparison.OrdinalIgnoreCase) ||
                info.Path.Contains(@"\AppData\Local\Temp", StringComparison.OrdinalIgnoreCase))
            {
                info.Warnings.Add("Запущен из временной папки");
                info.SuspiciousScore += 15;
                info.IsSuspicious = true;
            }

            // Проверка на путь с пробелами без кавычек (потенциальный hijacking)
            // Проверка на нестандартное расположение
            if (!info.Path.StartsWith(@"C:\Windows", StringComparison.OrdinalIgnoreCase) &&
                !info.Path.StartsWith(@"C:\Program Files", StringComparison.OrdinalIgnoreCase) &&
                !info.Path.Contains(@"\AppData\Roaming\Microsoft", StringComparison.OrdinalIgnoreCase))
            {
                // Запущен не из стандартных папок - легкое предупреждение
                info.SuspiciousScore += 2;
            }
        }

        // Проверка 3: Подозрительная цепочка родитель-ребенок
        if (!string.IsNullOrEmpty(info.ParentName))
        {
            // Например: cmd.exe запущен из notepad.exe - подозрительно
            string parentLower = info.ParentName.ToLowerInvariant();

            // Если родитель - приложение, которое обычно не запускает другие процессы
            var unusualParents = new[] { "notepad", "calc", "mspaint", "wordpad" };
            if (unusualParents.Contains(parentLower) && !unusualParents.Contains(lowerName))
            {
                info.Warnings.Add($"Необычный родительский процесс: {info.ParentName}");
                info.SuspiciousScore += 25;
                info.IsSuspicious = true;
            }
        }

        // Проверка 4: Маскировка под системный процесс
        var systemProcesses = new[] { "svchost", "csrss", "lsass", "services", "smss", "wininit" };
        if (systemProcesses.Contains(lowerName))
        {
            // Системные процессы должны быть в System32
            if (!string.IsNullOrEmpty(info.Path) &&
                !info.Path.StartsWith(@"C:\Windows\System32", StringComparison.OrdinalIgnoreCase))
            {
                info.Warnings.Add($"Маскировка под системный процесс! Неверный путь: {info.Path}");
                info.SuspiciousScore += 50;
                info.IsSuspicious = true;
            }
        }

        // Проверка 5: Много потоков (может указывать на DLL injection)
        if (info.ThreadCount > 200)
        {
            info.Warnings.Add($"Очень много потоков: {info.ThreadCount}");
            info.SuspiciousScore += 5;
        }

        // Проверка 6: Проверяем по ключевым словам из базы
        var (match, keyword) = HashScanner.CheckKeywords(info.Name);
        if (match)
        {
            info.Warnings.Add($"Совпадение с ключевым словом: '{keyword}'");
            info.SuspiciousScore += 15;
            info.IsSuspicious = true;
        }
    }

    /// <summary>
    /// Построение дерева процессов
    /// </summary>
    public static Dictionary<int, List<ProcessInfo>> BuildProcessTree(List<ProcessInfo> processes)
    {
        var tree = new Dictionary<int, List<ProcessInfo>>();

        foreach (var proc in processes)
        {
            if (!tree.ContainsKey(proc.ParentProcessId))
            {
                tree[proc.ParentProcessId] = new List<ProcessInfo>();
            }
            tree[proc.ParentProcessId].Add(proc);
        }

        return tree;
    }

    /// <summary>
    /// Полное сканирование процессов
    /// </summary>
    public static void ScanAllProcesses()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ АНАЛИЗ ЗАПУЩЕННЫХ ПРОЦЕССОВ ═══{ConsoleUI.ColorReset}\n");

        ConsoleUI.Log("+ Сбор информации о процессах...", true);

        var processes = AnalyzeAllProcesses();
        var suspiciousProcesses = processes.Where(p => p.IsSuspicious).OrderByDescending(p => p.SuspiciousScore).ToList();

        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Статистика:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  Всего процессов: {processes.Count}");
        Console.WriteLine($"  Подозрительных: {suspiciousProcesses.Count}");

        if (suspiciousProcesses.Count > 0)
        {
            Console.WriteLine($"\n{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}══ ПОДОЗРИТЕЛЬНЫЕ ПРОЦЕССЫ ══{ConsoleUI.ColorReset}\n");

            foreach (var proc in suspiciousProcesses)
            {
                string scoreColor = proc.SuspiciousScore >= 40 ? ConsoleUI.ColorRed :
                                   proc.SuspiciousScore >= 20 ? ConsoleUI.ColorOrange :
                                   ConsoleUI.ColorYellow;

                Console.WriteLine($"{ConsoleUI.ColorCyan}► {proc.Name} (PID: {proc.ProcessId}){ConsoleUI.ColorReset}");
                Console.WriteLine($"  {scoreColor}Уровень подозрительности: {proc.SuspiciousScore}{ConsoleUI.ColorReset}");

                if (!string.IsNullOrEmpty(proc.Path))
                    Console.WriteLine($"  Путь: {proc.Path}");

                if (!string.IsNullOrEmpty(proc.ParentName))
                    Console.WriteLine($"  Родитель: {proc.ParentName} (PID: {proc.ParentProcessId})");

                Console.WriteLine($"  Потоков: {proc.ThreadCount}");
                Console.WriteLine($"  Память: {proc.WorkingSet / 1024 / 1024} MB");

                if (proc.Warnings.Count > 0)
                {
                    Console.WriteLine($"  Предупреждения:");
                    foreach (var warning in proc.Warnings)
                    {
                        Console.WriteLine($"    {ConsoleUI.ColorOrange}! {warning}{ConsoleUI.ColorReset}");
                    }
                }

                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine($"\n{ConsoleUI.ColorGreen}+ Подозрительных процессов не обнаружено{ConsoleUI.ColorReset}");
        }

        // Вывод всех процессов (опционально)
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Все процессы (топ-20 по памяти):{ConsoleUI.ColorReset}");
        var topByMemory = processes.OrderByDescending(p => p.WorkingSet).Take(20);

        foreach (var proc in topByMemory)
        {
            string indicator = proc.IsSuspicious ? $"{ConsoleUI.ColorRed}!{ConsoleUI.ColorReset}" : " ";
            Console.WriteLine($"  {indicator} {proc.Name,-25} PID:{proc.ProcessId,-6} Память:{proc.WorkingSet / 1024 / 1024,5} MB");
        }

        ConsoleUI.Pause();
    }
}
