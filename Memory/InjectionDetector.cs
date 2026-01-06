using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using CustosAC.UI;
using CustosAC.WinAPI;

namespace CustosAC.Memory;

/// <summary>
/// Обнаружение инъекций DLL и подозрительных модулей
/// </summary>
public static class InjectionDetector
{
    #region Result Classes

    public class ModuleInfo
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public IntPtr BaseAddress { get; set; }
        public uint Size { get; set; }
        public bool IsSuspicious { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public class InjectionScanResult
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = "";
        public List<ModuleInfo> Modules { get; set; } = new();
        public List<ModuleInfo> SuspiciousModules { get; set; } = new();
        public List<string> HiddenPEAddresses { get; set; } = new();
        public int SuspiciousScore { get; set; }
    }

    #endregion

    // Известные легитимные пути для DLL
    private static readonly string[] TrustedPaths = new[]
    {
        @"C:\Windows\System32",
        @"C:\Windows\SysWOW64",
        @"C:\Windows\WinSxS",
        @"C:\Windows\Microsoft.NET",
        @"C:\Program Files",
        @"C:\Program Files (x86)",
        @"C:\Windows\assembly"
    };

    // Подозрительные имена DLL
    private static readonly string[] SuspiciousDllNames = new[]
    {
        "inject", "hook", "cheat", "hack", "bypass",
        "esp", "aimbot", "overlay", "d3d", "opengl32",
        "dinput", "xinput", "loader", "external", "internal"
    };

    /// <summary>
    /// Сканирование процесса на инъекции
    /// </summary>
    public static InjectionScanResult ScanProcess(int processId)
    {
        var result = new InjectionScanResult { ProcessId = processId };

        try
        {
            var process = Process.GetProcessById(processId);
            result.ProcessName = process.ProcessName;
        }
        catch
        {
            return result;
        }

        // Получаем список модулей через Toolhelp32
        result.Modules = GetProcessModules(processId);

        foreach (var module in result.Modules)
        {
            bool isSuspicious = false;

            // Проверка 1: Путь не в доверенных директориях
            if (!string.IsNullOrEmpty(module.Path))
            {
                bool inTrustedPath = TrustedPaths.Any(tp =>
                    module.Path.StartsWith(tp, StringComparison.OrdinalIgnoreCase));

                if (!inTrustedPath)
                {
                    // Исключаем путь самого приложения
                    string? processPath = GetProcessPath(processId);
                    if (processPath != null)
                    {
                        string processDir = Path.GetDirectoryName(processPath) ?? "";
                        if (!module.Path.StartsWith(processDir, StringComparison.OrdinalIgnoreCase))
                        {
                            module.Warnings.Add("DLL загружена из нестандартного пути");
                            isSuspicious = true;
                        }
                    }
                }
            }

            // Проверка 2: Подозрительное имя
            string lowerName = module.Name.ToLowerInvariant();
            foreach (var suspicious in SuspiciousDllNames)
            {
                if (lowerName.Contains(suspicious))
                {
                    module.Warnings.Add($"Подозрительное имя: содержит '{suspicious}'");
                    isSuspicious = true;
                    break;
                }
            }

            // Проверка 3: DLL из временных папок
            if (module.Path.Contains(@"\Temp\", StringComparison.OrdinalIgnoreCase) ||
                module.Path.Contains(@"\AppData\Local\Temp", StringComparison.OrdinalIgnoreCase))
            {
                module.Warnings.Add("DLL загружена из временной папки");
                isSuspicious = true;
            }

            // Проверка 4: DLL из AppData (кроме известных приложений)
            if (module.Path.Contains(@"\AppData\", StringComparison.OrdinalIgnoreCase) &&
                !module.Path.Contains(@"\Microsoft\", StringComparison.OrdinalIgnoreCase))
            {
                module.Warnings.Add("DLL загружена из AppData");
                isSuspicious = true;
            }

            if (isSuspicious)
            {
                module.IsSuspicious = true;
                result.SuspiciousModules.Add(module);
                result.SuspiciousScore += 15;
            }
        }

        // Проверка на скрытые PE в памяти
        var hiddenPEs = MemoryScanner.FindHiddenPE(processId);
        foreach (var pe in hiddenPEs)
        {
            result.HiddenPEAddresses.Add($"0x{pe.ToInt64():X}");
            result.SuspiciousScore += 30;
        }

        return result;
    }

    /// <summary>
    /// Получение списка модулей процесса
    /// </summary>
    public static List<ModuleInfo> GetProcessModules(int processId)
    {
        var modules = new List<ModuleInfo>();

        IntPtr snapshot = NativeMethods.CreateToolhelp32Snapshot(
            NativeMethods.TH32CS_SNAPMODULE | NativeMethods.TH32CS_SNAPMODULE32,
            (uint)processId);

        if (snapshot == NativeMethods.INVALID_HANDLE_VALUE)
            return modules;

        try
        {
            var moduleEntry = new NativeMethods.MODULEENTRY32W();
            moduleEntry.dwSize = (uint)Marshal.SizeOf<NativeMethods.MODULEENTRY32W>();

            if (NativeMethods.Module32FirstW(snapshot, ref moduleEntry))
            {
                do
                {
                    modules.Add(new ModuleInfo
                    {
                        Name = moduleEntry.szModule,
                        Path = moduleEntry.szExePath,
                        BaseAddress = moduleEntry.modBaseAddr,
                        Size = moduleEntry.modBaseSize
                    });
                }
                while (NativeMethods.Module32NextW(snapshot, ref moduleEntry));
            }
        }
        finally
        {
            NativeMethods.CloseHandle(snapshot);
        }

        return modules;
    }

    private static string? GetProcessPath(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Сканирование всех процессов на инъекции
    /// </summary>
    public static void ScanAllProcesses()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ОБНАРУЖЕНИЕ ИНЪЕКЦИЙ DLL ═══{ConsoleUI.ColorReset}\n");

        var suspiciousProcesses = new List<InjectionScanResult>();
        int scannedCount = 0;
        int errorCount = 0;

        var processes = Process.GetProcesses();
        int currentPid = Environment.ProcessId;

        // Исключаем системные процессы
        var excludedNames = new[] { "System", "Idle", "Registry", "smss", "csrss", "wininit", "services", "lsass" };

        ConsoleUI.Log($"+ Анализ процессов: {processes.Length}", true);

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

                scannedCount++;
                Console.Write($"\r  Анализ: {scannedCount} - {process.ProcessName}".PadRight(60));

                var scanResult = ScanProcess(process.Id);

                if (scanResult.SuspiciousModules.Count > 0 || scanResult.HiddenPEAddresses.Count > 0)
                {
                    suspiciousProcesses.Add(scanResult);
                }
            }
            catch
            {
                errorCount++;
            }
        }

        Console.WriteLine($"\r  Проанализировано: {scannedCount}, Ошибок: {errorCount}".PadRight(60));
        Console.WriteLine();

        // Сортируем по подозрительности
        suspiciousProcesses = suspiciousProcesses.OrderByDescending(p => p.SuspiciousScore).ToList();

        if (suspiciousProcesses.Count > 0)
        {
            Console.WriteLine($"{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}══ ПРОЦЕССЫ С ПОДОЗРИТЕЛЬНЫМИ МОДУЛЯМИ: {suspiciousProcesses.Count} ══{ConsoleUI.ColorReset}\n");

            foreach (var proc in suspiciousProcesses)
            {
                string scoreColor = proc.SuspiciousScore >= 50 ? ConsoleUI.ColorRed :
                                   proc.SuspiciousScore >= 25 ? ConsoleUI.ColorOrange :
                                   ConsoleUI.ColorYellow;

                Console.WriteLine($"{ConsoleUI.ColorCyan}► {proc.ProcessName} (PID: {proc.ProcessId}){ConsoleUI.ColorReset}");
                Console.WriteLine($"  {scoreColor}Уровень подозрительности: {proc.SuspiciousScore}{ConsoleUI.ColorReset}");
                Console.WriteLine($"  Всего модулей: {proc.Modules.Count}");

                if (proc.HiddenPEAddresses.Count > 0)
                {
                    Console.WriteLine($"  {ConsoleUI.ColorRed}Скрытые PE в памяти: {proc.HiddenPEAddresses.Count}{ConsoleUI.ColorReset}");
                    foreach (var addr in proc.HiddenPEAddresses)
                    {
                        Console.WriteLine($"    - Адрес: {addr}");
                    }
                }

                if (proc.SuspiciousModules.Count > 0)
                {
                    Console.WriteLine($"  Подозрительные модули:");
                    foreach (var module in proc.SuspiciousModules)
                    {
                        Console.WriteLine($"    {ConsoleUI.ColorOrange}• {module.Name}{ConsoleUI.ColorReset}");
                        Console.WriteLine($"      Путь: {module.Path}");
                        Console.WriteLine($"      Адрес: 0x{module.BaseAddress.ToInt64():X}");
                        foreach (var warning in module.Warnings)
                        {
                            Console.WriteLine($"      ! {warning}");
                        }
                    }
                }

                Console.WriteLine();
            }
        }
        else
        {
            ConsoleUI.Log("+ Подозрительных инъекций не обнаружено", true);
        }

        ConsoleUI.Pause();
    }

    /// <summary>
    /// Проверка конкретного процесса (по имени или PID)
    /// </summary>
    public static void ScanSpecificProcess(string processIdentifier)
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ПРОВЕРКА ПРОЦЕССА: {processIdentifier} ═══{ConsoleUI.ColorReset}\n");

        Process? targetProcess = null;

        // Попробуем как PID
        if (int.TryParse(processIdentifier, out int pid))
        {
            try
            {
                targetProcess = Process.GetProcessById(pid);
            }
            catch
            {
                ConsoleUI.Log($"- Процесс с PID {pid} не найден", false);
            }
        }
        else
        {
            // Ищем по имени
            var processes = Process.GetProcessesByName(processIdentifier);
            if (processes.Length > 0)
            {
                targetProcess = processes[0];
                if (processes.Length > 1)
                {
                    ConsoleUI.Log($"+ Найдено {processes.Length} процессов, анализируем первый (PID: {targetProcess.Id})", true);
                }
            }
            else
            {
                ConsoleUI.Log($"- Процесс '{processIdentifier}' не найден", false);
            }
        }

        if (targetProcess == null)
        {
            ConsoleUI.Pause();
            return;
        }

        ConsoleUI.Log($"+ Анализ процесса: {targetProcess.ProcessName} (PID: {targetProcess.Id})", true);

        var scanResult = ScanProcess(targetProcess.Id);

        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Информация о процессе:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  Имя: {scanResult.ProcessName}");
        Console.WriteLine($"  PID: {scanResult.ProcessId}");
        Console.WriteLine($"  Загружено модулей: {scanResult.Modules.Count}");

        if (scanResult.SuspiciousModules.Count > 0 || scanResult.HiddenPEAddresses.Count > 0)
        {
            Console.WriteLine($"\n{ConsoleUI.ColorRed}Обнаружены подозрительные элементы!{ConsoleUI.ColorReset}");
            Console.WriteLine($"  Уровень подозрительности: {scanResult.SuspiciousScore}");

            if (scanResult.HiddenPEAddresses.Count > 0)
            {
                Console.WriteLine($"\n{ConsoleUI.ColorRed}Скрытые PE в памяти:{ConsoleUI.ColorReset}");
                foreach (var addr in scanResult.HiddenPEAddresses)
                {
                    Console.WriteLine($"  - {addr}");
                }
            }

            if (scanResult.SuspiciousModules.Count > 0)
            {
                Console.WriteLine($"\n{ConsoleUI.ColorOrange}Подозрительные модули:{ConsoleUI.ColorReset}");
                foreach (var module in scanResult.SuspiciousModules)
                {
                    Console.WriteLine($"\n  {ConsoleUI.ColorOrange}• {module.Name}{ConsoleUI.ColorReset}");
                    Console.WriteLine($"    Путь: {module.Path}");
                    Console.WriteLine($"    Базовый адрес: 0x{module.BaseAddress.ToInt64():X}");
                    Console.WriteLine($"    Размер: {module.Size} байт");
                    foreach (var warning in module.Warnings)
                    {
                        Console.WriteLine($"    ! {warning}");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"\n{ConsoleUI.ColorGreen}Подозрительных модулей не обнаружено{ConsoleUI.ColorReset}");
        }

        // Выводим все модули по запросу
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Все загруженные модули ({scanResult.Modules.Count}):{ConsoleUI.ColorReset}");
        foreach (var module in scanResult.Modules.Take(30))
        {
            Console.WriteLine($"  • {module.Name}");
            Console.WriteLine($"    {module.Path}");
        }

        if (scanResult.Modules.Count > 30)
        {
            Console.WriteLine($"  ... и ещё {scanResult.Modules.Count - 30} модулей");
        }

        ConsoleUI.Pause();
    }
}
