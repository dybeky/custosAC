using System.Diagnostics;
using System.Runtime.InteropServices;
using CustosAC.UI;
using CustosAC.WinAPI;

namespace CustosAC.ProcessAnalysis;

/// <summary>
/// Анализатор хендлов процессов (обнаружение подозрительного доступа к другим процессам)
/// </summary>
public static class HandleAnalyzer
{
    #region Result Classes

    public class ProcessHandleInfo
    {
        public int SourceProcessId { get; set; }
        public string SourceProcessName { get; set; } = "";
        public int TargetProcessId { get; set; }
        public string TargetProcessName { get; set; } = "";
        public uint AccessMask { get; set; }
        public string AccessDescription { get; set; } = "";
        public bool IsSuspicious { get; set; }
    }

    public class HandleScanResult
    {
        public List<ProcessHandleInfo> SuspiciousHandles { get; set; } = new();
        public int TotalHandlesScanned { get; set; }
    }

    #endregion

    // Игровые процессы, которые часто являются целями читов
    private static readonly string[] GameProcesses = new[]
    {
        // Шутеры
        "csgo", "cs2", "valorant", "apex_legends", "fortnite",
        "pubg", "cod", "r5apex", "overwatch", "tarkov",
        // Другие игры
        "unturned", "rust", "dayz", "minecraft", "gta5",
        "gtav", "rdr2", "eft", "deadbydaylight", "dbd",
        // Лаунчеры
        "steam", "epicgameslauncher", "battlenet", "origin"
    };

    // Подозрительные права доступа к процессам
    private const uint PROCESS_VM_OPERATION = 0x0008;
    private const uint PROCESS_VM_READ = 0x0010;
    private const uint PROCESS_VM_WRITE = 0x0020;
    private const uint PROCESS_CREATE_THREAD = 0x0002;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;

    /// <summary>
    /// Поиск процессов с подозрительным доступом к играм
    /// </summary>
    public static HandleScanResult FindSuspiciousProcessAccess()
    {
        var result = new HandleScanResult();

        // Получаем список игровых процессов
        var gameProcessIds = new HashSet<int>();
        var gameProcessNames = new Dictionary<int, string>();

        foreach (var proc in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                string lowerName = proc.ProcessName.ToLowerInvariant();
                if (GameProcesses.Any(g => lowerName.Contains(g)))
                {
                    gameProcessIds.Add(proc.Id);
                    gameProcessNames[proc.Id] = proc.ProcessName;
                }
            }
            catch
            {
                // Игнорируем
            }
        }

        if (gameProcessIds.Count == 0)
        {
            return result;
        }

        // Для каждого не-игрового процесса проверяем, имеет ли он хендлы к игровым процессам
        foreach (var proc in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                // Пропускаем игровые и системные процессы
                if (gameProcessIds.Contains(proc.Id) || proc.Id <= 4)
                    continue;

                // Проверяем, может ли этот процесс открыть игровые процессы
                foreach (var gameId in gameProcessIds)
                {
                    // Пытаемся определить, есть ли доступ
                    var handleInfo = CheckProcessAccess(proc.Id, proc.ProcessName, gameId, gameProcessNames[gameId]);

                    if (handleInfo != null && handleInfo.IsSuspicious)
                    {
                        result.SuspiciousHandles.Add(handleInfo);
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки доступа
            }
        }

        return result;
    }

    private static ProcessHandleInfo? CheckProcessAccess(int sourcePid, string sourceName, int targetPid, string targetName)
    {
        // Этот метод проверяет "косвенные признаки" - мы не можем напрямую
        // перечислить хендлы другого процесса без драйвера, но можем проверить
        // некоторые паттерны поведения

        // Проверяем подозрительные имена процессов
        string lowerSource = sourceName.ToLowerInvariant();

        var suspiciousKeywords = new[] { "inject", "hack", "cheat", "external", "loader", "bypass" };

        if (suspiciousKeywords.Any(k => lowerSource.Contains(k)))
        {
            return new ProcessHandleInfo
            {
                SourceProcessId = sourcePid,
                SourceProcessName = sourceName,
                TargetProcessId = targetPid,
                TargetProcessName = targetName,
                IsSuspicious = true,
                AccessDescription = "Подозрительное имя процесса"
            };
        }

        return null;
    }

    /// <summary>
    /// Анализ процессов, которые могут обращаться к целевому процессу
    /// </summary>
    public static List<ProcessHandleInfo> AnalyzeAccessToProcess(int targetPid)
    {
        var results = new List<ProcessHandleInfo>();

        string? targetName = null;
        try
        {
            targetName = System.Diagnostics.Process.GetProcessById(targetPid).ProcessName;
        }
        catch
        {
            return results;
        }

        // Исключаем системные процессы и сам целевой процесс
        var excludedProcesses = new[] { "System", "Idle", "csrss", "smss", "lsass", "services" };
        int currentPid = Environment.ProcessId;

        foreach (var proc in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                if (proc.Id == targetPid || proc.Id == currentPid || proc.Id <= 4)
                    continue;

                if (excludedProcesses.Contains(proc.ProcessName, StringComparer.OrdinalIgnoreCase))
                    continue;

                // Пытаемся открыть целевой процесс от имени проверяемого
                // (упрощенная проверка - смотрим на характеристики процесса)

                var info = new ProcessHandleInfo
                {
                    SourceProcessId = proc.Id,
                    SourceProcessName = proc.ProcessName,
                    TargetProcessId = targetPid,
                    TargetProcessName = targetName
                };

                // Проверяем подозрительные признаки
                string lowerName = proc.ProcessName.ToLowerInvariant();

                // Подозрительные имена
                if (ContainsSuspiciousKeyword(lowerName))
                {
                    info.IsSuspicious = true;
                    info.AccessDescription = "Подозрительное имя процесса";
                    results.Add(info);
                    continue;
                }

                // Проверяем путь процесса
                try
                {
                    string? path = proc.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (path.Contains(@"\Temp\", StringComparison.OrdinalIgnoreCase) ||
                            path.Contains(@"\Downloads\", StringComparison.OrdinalIgnoreCase))
                        {
                            info.IsSuspicious = true;
                            info.AccessDescription = "Запущен из подозрительной директории";
                            results.Add(info);
                        }
                    }
                }
                catch
                {
                    // Не можем получить путь - это тоже подозрительно для некоторых процессов
                }
            }
            catch
            {
                // Игнорируем недоступные процессы
            }
        }

        return results;
    }

    private static bool ContainsSuspiciousKeyword(string name)
    {
        var keywords = new[] {
            "inject", "hook", "cheat", "hack", "bypass", "loader",
            "external", "internal", "esp", "aim", "trigger", "wallhack",
            "speedhack", "noclip", "godmode", "mapper"
        };

        return keywords.Any(k => name.Contains(k));
    }

    /// <summary>
    /// Сканирование хендлов
    /// </summary>
    public static void ScanHandles()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ АНАЛИЗ ДОСТУПА К ПРОЦЕССАМ ═══{ConsoleUI.ColorReset}\n");

        ConsoleUI.Log("+ Поиск игровых процессов...", true);

        // Ищем игровые процессы
        var gameProcesses = new List<(int pid, string name)>();
        foreach (var proc in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                string lowerName = proc.ProcessName.ToLowerInvariant();
                if (GameProcesses.Any(g => lowerName.Contains(g)))
                {
                    gameProcesses.Add((proc.Id, proc.ProcessName));
                }
            }
            catch { }
        }

        if (gameProcesses.Count == 0)
        {
            Console.WriteLine($"\n{ConsoleUI.ColorYellow}Игровые процессы не найдены.{ConsoleUI.ColorReset}");
            Console.WriteLine("Запустите игру и повторите сканирование.");
            ConsoleUI.Pause();
            return;
        }

        Console.WriteLine($"\n{ConsoleUI.ColorGreen}Найдено игровых процессов: {gameProcesses.Count}{ConsoleUI.ColorReset}");
        foreach (var (pid, name) in gameProcesses)
        {
            Console.WriteLine($"  • {name} (PID: {pid})");
        }

        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Анализ процессов с потенциальным доступом...{ConsoleUI.ColorReset}");

        var allSuspicious = new List<ProcessHandleInfo>();

        foreach (var (pid, name) in gameProcesses)
        {
            var suspicious = AnalyzeAccessToProcess(pid);
            allSuspicious.AddRange(suspicious);
        }

        // Также проверяем общие подозрительные процессы
        var generalResult = FindSuspiciousProcessAccess();
        allSuspicious.AddRange(generalResult.SuspiciousHandles);

        // Убираем дубликаты
        allSuspicious = allSuspicious
            .GroupBy(h => h.SourceProcessId)
            .Select(g => g.First())
            .ToList();

        if (allSuspicious.Count > 0)
        {
            Console.WriteLine($"\n{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}══ ПОДОЗРИТЕЛЬНЫЕ ПРОЦЕССЫ: {allSuspicious.Count} ══{ConsoleUI.ColorReset}\n");

            foreach (var handle in allSuspicious)
            {
                Console.WriteLine($"{ConsoleUI.ColorRed}► {handle.SourceProcessName} (PID: {handle.SourceProcessId}){ConsoleUI.ColorReset}");
                Console.WriteLine($"  Цель: {handle.TargetProcessName} (PID: {handle.TargetProcessId})");
                Console.WriteLine($"  Причина: {handle.AccessDescription}");
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine($"\n{ConsoleUI.ColorGreen}+ Подозрительных процессов не обнаружено{ConsoleUI.ColorReset}");
        }

        ConsoleUI.Pause();
    }
}
