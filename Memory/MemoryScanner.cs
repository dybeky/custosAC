using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using CustosAC.UI;
using CustosAC.WinAPI;

namespace CustosAC.Memory;

/// <summary>
/// Сканер памяти процессов
/// </summary>
public static class MemoryScanner
{
    #region Result Classes

    public class MemoryRegion
    {
        public IntPtr BaseAddress { get; set; }
        public long Size { get; set; }
        public uint Protection { get; set; }
        public uint State { get; set; }
        public uint Type { get; set; }
        public bool IsExecutable { get; set; }
        public bool IsWritable { get; set; }
        public bool IsRWX { get; set; }
        public string ProtectionString { get; set; } = "";
    }

    public class ProcessMemoryInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = "";
        public List<MemoryRegion> Regions { get; set; } = new();
        public int TotalRegions { get; set; }
        public int ExecutableRegions { get; set; }
        public int RWXRegions { get; set; }
        public long TotalPrivateMemory { get; set; }
        public List<string> Warnings { get; set; } = new();
        public int SuspiciousScore { get; set; }
    }

    public class PatternMatch
    {
        public IntPtr Address { get; set; }
        public string PatternName { get; set; } = "";
        public byte[] MatchedBytes { get; set; } = Array.Empty<byte>();
    }

    #endregion

    /// <summary>
    /// Сканирование памяти процесса
    /// </summary>
    public static ProcessMemoryInfo ScanProcessMemory(int processId)
    {
        var result = new ProcessMemoryInfo { ProcessId = processId };

        try
        {
            var process = Process.GetProcessById(processId);
            result.ProcessName = process.ProcessName;
        }
        catch
        {
            result.Warnings.Add("Не удалось получить имя процесса");
        }

        IntPtr processHandle = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_VM_READ,
            false,
            (uint)processId);

        if (processHandle == IntPtr.Zero)
        {
            result.Warnings.Add("Не удалось открыть процесс (требуются права администратора)");
            return result;
        }

        try
        {
            IntPtr address = IntPtr.Zero;
            long maxAddress = Environment.Is64BitProcess ? 0x7FFFFFFFFFFF : 0x7FFFFFFF;

            while (address.ToInt64() < maxAddress)
            {
                NativeMethods.MEMORY_BASIC_INFORMATION mbi;
                int queryResult = NativeMethods.VirtualQueryEx(
                    processHandle,
                    address,
                    out mbi,
                    (uint)Marshal.SizeOf<NativeMethods.MEMORY_BASIC_INFORMATION>());

                if (queryResult == 0)
                    break;

                if (mbi.State == NativeMethods.MEM_COMMIT)
                {
                    result.TotalRegions++;

                    var region = new MemoryRegion
                    {
                        BaseAddress = mbi.BaseAddress,
                        Size = mbi.RegionSize.ToInt64(),
                        Protection = mbi.Protect,
                        State = mbi.State,
                        Type = mbi.Type,
                        IsExecutable = NativeMethods.IsExecutableMemory(mbi.Protect),
                        IsWritable = NativeMethods.IsWritableMemory(mbi.Protect),
                        IsRWX = NativeMethods.IsRWXMemory(mbi.Protect),
                        ProtectionString = GetProtectionString(mbi.Protect)
                    };

                    // Считаем статистику
                    if (mbi.Type == NativeMethods.MEM_PRIVATE)
                    {
                        result.TotalPrivateMemory += region.Size;
                    }

                    if (region.IsExecutable)
                    {
                        result.ExecutableRegions++;
                    }

                    if (region.IsRWX)
                    {
                        result.RWXRegions++;
                        result.Regions.Add(region);
                        result.Warnings.Add($"RWX регион: 0x{region.BaseAddress.ToInt64():X} ({region.Size} байт)");
                        result.SuspiciousScore += 20;
                    }
                    // Добавляем только подозрительные регионы
                    else if (region.IsExecutable && mbi.Type == NativeMethods.MEM_PRIVATE)
                    {
                        result.Regions.Add(region);
                        result.SuspiciousScore += 5;
                    }
                }

                // Переходим к следующему региону
                address = new IntPtr(mbi.BaseAddress.ToInt64() + mbi.RegionSize.ToInt64());
            }
        }
        finally
        {
            NativeMethods.CloseHandle(processHandle);
        }

        return result;
    }

    /// <summary>
    /// Поиск паттерна в памяти процесса
    /// </summary>
    public static List<PatternMatch> ScanForPattern(int processId, byte[] pattern, string patternName)
    {
        var matches = new List<PatternMatch>();

        IntPtr processHandle = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_VM_READ,
            false,
            (uint)processId);

        if (processHandle == IntPtr.Zero)
            return matches;

        try
        {
            IntPtr address = IntPtr.Zero;
            long maxAddress = Environment.Is64BitProcess ? 0x7FFFFFFFFFFF : 0x7FFFFFFF;

            while (address.ToInt64() < maxAddress)
            {
                NativeMethods.MEMORY_BASIC_INFORMATION mbi;
                int queryResult = NativeMethods.VirtualQueryEx(
                    processHandle,
                    address,
                    out mbi,
                    (uint)Marshal.SizeOf<NativeMethods.MEMORY_BASIC_INFORMATION>());

                if (queryResult == 0)
                    break;

                // Сканируем только коммиченные регионы
                if (mbi.State == NativeMethods.MEM_COMMIT &&
                    (mbi.Protect & NativeMethods.PAGE_GUARD) == 0 &&
                    mbi.Protect != NativeMethods.PAGE_NOACCESS)
                {
                    int regionSize = (int)Math.Min(mbi.RegionSize.ToInt64(), 10 * 1024 * 1024); // Макс 10MB
                    byte[] buffer = new byte[regionSize];

                    if (NativeMethods.ReadProcessMemory(processHandle, mbi.BaseAddress, buffer, regionSize, out int bytesRead))
                    {
                        // Ищем паттерн в буфере
                        for (int i = 0; i <= bytesRead - pattern.Length; i++)
                        {
                            bool found = true;
                            for (int j = 0; j < pattern.Length; j++)
                            {
                                if (buffer[i + j] != pattern[j])
                                {
                                    found = false;
                                    break;
                                }
                            }

                            if (found)
                            {
                                matches.Add(new PatternMatch
                                {
                                    Address = new IntPtr(mbi.BaseAddress.ToInt64() + i),
                                    PatternName = patternName,
                                    MatchedBytes = buffer.Skip(i).Take(pattern.Length).ToArray()
                                });
                            }
                        }
                    }
                }

                address = new IntPtr(mbi.BaseAddress.ToInt64() + mbi.RegionSize.ToInt64());
            }
        }
        finally
        {
            NativeMethods.CloseHandle(processHandle);
        }

        return matches;
    }

    /// <summary>
    /// Проверка на наличие MZ заголовка в неожиданных местах (возможная инъекция)
    /// </summary>
    public static List<IntPtr> FindHiddenPE(int processId)
    {
        var hiddenPEs = new List<IntPtr>();
        byte[] mzSignature = new byte[] { 0x4D, 0x5A }; // MZ

        IntPtr processHandle = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_QUERY_INFORMATION | NativeMethods.PROCESS_VM_READ,
            false,
            (uint)processId);

        if (processHandle == IntPtr.Zero)
            return hiddenPEs;

        try
        {
            IntPtr address = IntPtr.Zero;
            long maxAddress = Environment.Is64BitProcess ? 0x7FFFFFFFFFFF : 0x7FFFFFFF;

            while (address.ToInt64() < maxAddress)
            {
                NativeMethods.MEMORY_BASIC_INFORMATION mbi;
                int queryResult = NativeMethods.VirtualQueryEx(
                    processHandle,
                    address,
                    out mbi,
                    (uint)Marshal.SizeOf<NativeMethods.MEMORY_BASIC_INFORMATION>());

                if (queryResult == 0)
                    break;

                // Ищем только в приватной памяти (не в загруженных модулях)
                if (mbi.State == NativeMethods.MEM_COMMIT &&
                    mbi.Type == NativeMethods.MEM_PRIVATE &&
                    NativeMethods.IsExecutableMemory(mbi.Protect))
                {
                    byte[] buffer = new byte[2];

                    if (NativeMethods.ReadProcessMemory(processHandle, mbi.BaseAddress, buffer, 2, out _))
                    {
                        if (buffer[0] == 0x4D && buffer[1] == 0x5A)
                        {
                            hiddenPEs.Add(mbi.BaseAddress);
                        }
                    }
                }

                address = new IntPtr(mbi.BaseAddress.ToInt64() + mbi.RegionSize.ToInt64());
            }
        }
        finally
        {
            NativeMethods.CloseHandle(processHandle);
        }

        return hiddenPEs;
    }

    private static string GetProtectionString(uint protect)
    {
        var sb = new StringBuilder();

        if ((protect & NativeMethods.PAGE_EXECUTE_READWRITE) != 0) return "RWX";
        if ((protect & NativeMethods.PAGE_EXECUTE_WRITECOPY) != 0) return "RWC";
        if ((protect & NativeMethods.PAGE_EXECUTE_READ) != 0) return "RX";
        if ((protect & NativeMethods.PAGE_EXECUTE) != 0) return "X";
        if ((protect & NativeMethods.PAGE_READWRITE) != 0) return "RW";
        if ((protect & NativeMethods.PAGE_WRITECOPY) != 0) return "WC";
        if ((protect & NativeMethods.PAGE_READONLY) != 0) return "R";
        if ((protect & NativeMethods.PAGE_NOACCESS) != 0) return "NA";

        return protect.ToString("X");
    }

    /// <summary>
    /// Сканирование всех процессов на подозрительную память
    /// </summary>
    public static void ScanAllProcesses()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ СКАНИРОВАНИЕ ПАМЯТИ ПРОЦЕССОВ ═══{ConsoleUI.ColorReset}\n");

        var suspiciousProcesses = new List<ProcessMemoryInfo>();
        int scannedCount = 0;
        int errorCount = 0;

        var processes = Process.GetProcesses();
        int currentPid = Environment.ProcessId;

        // Исключаем системные процессы
        var excludedNames = new[] { "System", "Idle", "Registry", "smss", "csrss", "wininit", "services", "lsass", "svchost" };

        ConsoleUI.Log($"+ Найдено процессов: {processes.Length}", true);

        foreach (var process in processes)
        {
            try
            {
                // Пропускаем системные и свой процесс
                if (process.Id == currentPid ||
                    process.Id <= 4 ||
                    excludedNames.Any(n => process.ProcessName.Equals(n, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                scannedCount++;
                Console.Write($"\r  Сканирование: {scannedCount}/{processes.Length} - {process.ProcessName}".PadRight(60));

                var memInfo = ScanProcessMemory(process.Id);

                if (memInfo.RWXRegions > 0 || memInfo.SuspiciousScore >= 10)
                {
                    suspiciousProcesses.Add(memInfo);
                }
            }
            catch
            {
                errorCount++;
            }
        }

        Console.WriteLine($"\r  Просканировано: {scannedCount}, Ошибок: {errorCount}".PadRight(60));
        Console.WriteLine();

        // Сортируем по подозрительности
        suspiciousProcesses = suspiciousProcesses.OrderByDescending(p => p.SuspiciousScore).ToList();

        if (suspiciousProcesses.Count > 0)
        {
            Console.WriteLine($"{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}══ ПОДОЗРИТЕЛЬНЫЕ ПРОЦЕССЫ: {suspiciousProcesses.Count} ══{ConsoleUI.ColorReset}\n");

            foreach (var proc in suspiciousProcesses)
            {
                string scoreColor = proc.SuspiciousScore >= 30 ? ConsoleUI.ColorRed :
                                   proc.SuspiciousScore >= 15 ? ConsoleUI.ColorOrange :
                                   ConsoleUI.ColorYellow;

                Console.WriteLine($"{ConsoleUI.ColorCyan}► {proc.ProcessName} (PID: {proc.ProcessId}){ConsoleUI.ColorReset}");
                Console.WriteLine($"  {scoreColor}Уровень подозрительности: {proc.SuspiciousScore}{ConsoleUI.ColorReset}");
                Console.WriteLine($"  Всего регионов: {proc.TotalRegions}");
                Console.WriteLine($"  Исполняемых регионов: {proc.ExecutableRegions}");
                Console.WriteLine($"  {ConsoleUI.ColorRed}RWX регионов: {proc.RWXRegions}{ConsoleUI.ColorReset}");
                Console.WriteLine($"  Приватная память: {proc.TotalPrivateMemory / 1024 / 1024} MB");

                if (proc.Warnings.Count > 0)
                {
                    Console.WriteLine($"  Предупреждения:");
                    foreach (var warning in proc.Warnings.Take(5))
                    {
                        Console.WriteLine($"    - {warning}");
                    }
                }

                Console.WriteLine();
            }
        }
        else
        {
            ConsoleUI.Log("+ Подозрительной активности в памяти не обнаружено", true);
        }

        ConsoleUI.Pause();
    }
}
