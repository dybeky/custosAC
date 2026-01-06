using System.Runtime.InteropServices;
using System.Text;
using CustosAC.UI;
using CustosAC.WinAPI;
using CustosAC.Scanner;

namespace CustosAC.SystemAnalysis;

/// <summary>
/// Анализатор загруженных драйверов системы
/// </summary>
public static class DriverAnalyzer
{
    #region Result Classes

    public class DriverInfo
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public IntPtr BaseAddress { get; set; }
        public bool IsSuspicious { get; set; }
        public bool IsSigned { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public class DriverScanResult
    {
        public List<DriverInfo> Drivers { get; set; } = new();
        public List<DriverInfo> SuspiciousDrivers { get; set; } = new();
        public int TotalDrivers { get; set; }
    }

    #endregion

    // Известные подозрительные/уязвимые драйверы
    private static readonly string[] SuspiciousDriverNames = new[]
    {
        // Часто используемые для читов через уязвимости
        "capcom", "cpuz", "gdrv", "asmmap", "physmem", "winio",
        "winring0", "rtcore", "dbutil", "ene", "bsmi",
        "kprocesshacker", "procexp", "nvoclock", "aswvmm",
        // Общие подозрительные имена
        "hack", "cheat", "inject", "mapper", "vulnerable",
        "bypass", "exploit", "rootkit"
    };

    // Легитимные системные драйверы
    private static readonly string[] SystemDriverPaths = new[]
    {
        @"\SystemRoot\System32\drivers\",
        @"\SystemRoot\System32\",
        @"C:\Windows\System32\drivers\",
        @"C:\Windows\System32\"
    };

    /// <summary>
    /// Получение списка загруженных драйверов
    /// </summary>
    public static DriverScanResult GetLoadedDrivers()
    {
        var result = new DriverScanResult();

        // Загружаем базу сигнатур
        HashScanner.LoadDatabase();
        var suspiciousDriversList = HashScanner.GetSuspiciousDrivers();

        // Получаем адреса драйверов
        IntPtr[] drivers = new IntPtr[1024];
        uint cb = (uint)(drivers.Length * IntPtr.Size);
        uint cbNeeded;

        if (!NativeMethods.EnumDeviceDrivers(drivers, cb, out cbNeeded))
        {
            return result;
        }

        int driverCount = (int)(cbNeeded / IntPtr.Size);
        result.TotalDrivers = driverCount;

        var baseNameBuffer = new StringBuilder(260);
        var fileNameBuffer = new StringBuilder(260);

        for (int i = 0; i < driverCount; i++)
        {
            var driver = new DriverInfo { BaseAddress = drivers[i] };

            // Получаем имя драйвера
            baseNameBuffer.Clear();
            if (NativeMethods.GetDeviceDriverBaseName(drivers[i], baseNameBuffer, 260) > 0)
            {
                driver.Name = baseNameBuffer.ToString();
            }

            // Получаем путь драйвера
            fileNameBuffer.Clear();
            if (NativeMethods.GetDeviceDriverFileName(drivers[i], fileNameBuffer, 260) > 0)
            {
                driver.Path = fileNameBuffer.ToString();
            }

            // Анализируем на подозрительность
            AnalyzeDriver(driver, suspiciousDriversList);

            result.Drivers.Add(driver);

            if (driver.IsSuspicious)
            {
                result.SuspiciousDrivers.Add(driver);
            }
        }

        return result;
    }

    private static void AnalyzeDriver(DriverInfo driver, List<string> suspiciousFromDb)
    {
        string lowerName = driver.Name.ToLowerInvariant();
        string lowerPath = driver.Path.ToLowerInvariant();

        // Проверка 1: Известные подозрительные драйверы из базы
        foreach (var suspicious in suspiciousFromDb)
        {
            if (lowerName.Contains(suspicious.ToLowerInvariant()))
            {
                driver.IsSuspicious = true;
                driver.Warnings.Add($"Известный уязвимый драйвер: {suspicious}");
                return;
            }
        }

        // Проверка 2: Подозрительные имена
        foreach (var suspicious in SuspiciousDriverNames)
        {
            if (lowerName.Contains(suspicious))
            {
                driver.IsSuspicious = true;
                driver.Warnings.Add($"Подозрительное имя: содержит '{suspicious}'");
                break;
            }
        }

        // Проверка 3: Драйвер не в системной папке
        bool inSystemPath = SystemDriverPaths.Any(sp =>
            lowerPath.StartsWith(sp.ToLowerInvariant().Replace(@"\systemroot\", @"c:\windows\")) ||
            lowerPath.StartsWith(sp.ToLowerInvariant()));

        if (!string.IsNullOrEmpty(driver.Path) && !inSystemPath)
        {
            // Исключаем известные легитимные пути
            if (!lowerPath.Contains(@"\program files\") &&
                !lowerPath.Contains(@"\program files (x86)\"))
            {
                driver.Warnings.Add("Загружен из нестандартного расположения");
                // Не помечаем как suspicious, просто предупреждение
            }
        }

        // Проверка 4: Драйвер без расширения .sys
        if (!string.IsNullOrEmpty(driver.Name) &&
            !driver.Name.EndsWith(".sys", StringComparison.OrdinalIgnoreCase))
        {
            // Это может быть ntoskrnl, hal и т.д. - не подозрительно само по себе
        }
    }

    /// <summary>
    /// Полное сканирование драйверов
    /// </summary>
    public static void ScanDrivers()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ АНАЛИЗ ЗАГРУЖЕННЫХ ДРАЙВЕРОВ ═══{ConsoleUI.ColorReset}\n");

        ConsoleUI.Log("+ Получение списка драйверов...", true);

        var result = GetLoadedDrivers();

        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Статистика:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  Всего драйверов: {result.TotalDrivers}");
        Console.WriteLine($"  Подозрительных: {result.SuspiciousDrivers.Count}");

        if (result.SuspiciousDrivers.Count > 0)
        {
            Console.WriteLine($"\n{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}══ ПОДОЗРИТЕЛЬНЫЕ ДРАЙВЕРЫ ══{ConsoleUI.ColorReset}\n");

            foreach (var driver in result.SuspiciousDrivers)
            {
                Console.WriteLine($"{ConsoleUI.ColorRed}► {driver.Name}{ConsoleUI.ColorReset}");
                Console.WriteLine($"  Путь: {driver.Path}");
                Console.WriteLine($"  Базовый адрес: 0x{driver.BaseAddress.ToInt64():X}");

                foreach (var warning in driver.Warnings)
                {
                    Console.WriteLine($"  {ConsoleUI.ColorOrange}! {warning}{ConsoleUI.ColorReset}");
                }
                Console.WriteLine();
            }

            Console.WriteLine($"{ConsoleUI.ColorYellow}ВНИМАНИЕ: Наличие этих драйверов может указывать на{ConsoleUI.ColorReset}");
            Console.WriteLine($"{ConsoleUI.ColorYellow}использование читов с kernel-уровнем доступа.{ConsoleUI.ColorReset}");
        }
        else
        {
            Console.WriteLine($"\n{ConsoleUI.ColorGreen}+ Подозрительных драйверов не обнаружено{ConsoleUI.ColorReset}");
        }

        // Выводим все драйверы
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Все загруженные драйверы ({result.TotalDrivers}):{ConsoleUI.ColorReset}");

        // Группируем по подозрительности
        var sortedDrivers = result.Drivers
            .OrderByDescending(d => d.IsSuspicious)
            .ThenBy(d => d.Name)
            .Take(50);

        foreach (var driver in sortedDrivers)
        {
            string indicator = driver.IsSuspicious ? $"{ConsoleUI.ColorRed}!{ConsoleUI.ColorReset}" :
                              driver.Warnings.Count > 0 ? $"{ConsoleUI.ColorYellow}?{ConsoleUI.ColorReset}" : " ";

            Console.WriteLine($"  {indicator} {driver.Name,-30} 0x{driver.BaseAddress.ToInt64():X}");
        }

        if (result.TotalDrivers > 50)
        {
            Console.WriteLine($"  ... и ещё {result.TotalDrivers - 50} драйверов");
        }

        ConsoleUI.Pause();
    }

    /// <summary>
    /// Быстрая проверка на подозрительные драйверы (для авто-сканирования)
    /// </summary>
    public static List<string> QuickSuspiciousCheck()
    {
        var suspicious = new List<string>();
        var result = GetLoadedDrivers();

        foreach (var driver in result.SuspiciousDrivers)
        {
            suspicious.Add($"{driver.Name}: {string.Join(", ", driver.Warnings)}");
        }

        return suspicious;
    }
}
