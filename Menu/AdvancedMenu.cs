using CustosAC.UI;
using CustosAC.Scanner;
using CustosAC.Memory;
using CustosAC.ProcessAnalysis;
using CustosAC.SystemAnalysis;
using CustosAC.Network;

namespace CustosAC.Menu;

/// <summary>
/// Меню расширенных проверок (низкоуровневый анализ)
/// </summary>
public static class AdvancedMenu
{
    public static void Run()
    {
        while (true)
        {
            ConsoleUI.PrintHeader();
            ConsoleUI.PrintMenu("РАСШИРЕННАЯ ПРОВЕРКА", new[]
            {
                // Сигнатурный анализ
                "Сканирование по хешам",
                "Анализ PE-файлов",
                "Проверка цифровых подписей",
                "────────────────────────────────",
                // Анализ памяти
                "Сканирование памяти процессов",
                "Обнаружение инъекций DLL",
                "Анализ потоков",
                "────────────────────────────────",
                // Анализ процессов
                "Анализ запущенных процессов",
                "Проверка доступа к игровым процессам",
                "────────────────────────────────",
                // Системный анализ
                "Анализ загруженных драйверов",
                "Анализ системных сервисов",
                "Детекция хуков",
                "────────────────────────────────",
                // Сетевой анализ
                "Анализ сетевых соединений",
                "────────────────────────────────",
                "> ПОЛНАЯ ПРОВЕРКА (ВСЕ МОДУЛИ)"
            }, true);

            int choice = ConsoleUI.GetChoice(13);

            switch (choice)
            {
                case 0:
                    return;

                // Сигнатурный анализ
                case 1:
                    HashScanner.ScanDirectory();
                    break;
                case 2:
                    PEAnalyzer.ScanDirectory();
                    break;
                case 3:
                    SignatureVerifier.ScanUnsignedFiles();
                    break;

                // Анализ памяти
                case 4:
                    MemoryScanner.ScanAllProcesses();
                    break;
                case 5:
                    InjectionDetector.ScanAllProcesses();
                    break;
                case 6:
                    ThreadAnalyzer.ScanAllProcesses();
                    break;

                // Анализ процессов
                case 7:
                    ProcessAnalyzer.ScanAllProcesses();
                    break;
                case 8:
                    HandleAnalyzer.ScanHandles();
                    break;

                // Системный анализ
                case 9:
                    DriverAnalyzer.ScanDrivers();
                    break;
                case 10:
                    ServiceAnalyzer.ScanServices();
                    break;
                case 11:
                    HookDetector.ScanHooks();
                    break;

                // Сетевой анализ
                case 12:
                    NetworkAnalyzer.ScanNetwork();
                    break;

                // Полная проверка
                case 13:
                    RunFullScan();
                    break;
            }
        }
    }

    /// <summary>
    /// Запуск полной проверки всеми модулями
    /// </summary>
    private static void RunFullScan()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══════════════════════════════════════════{ConsoleUI.ColorReset}");
        Console.WriteLine($"{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}       ПОЛНАЯ РАСШИРЕННАЯ ПРОВЕРКА          {ConsoleUI.ColorReset}");
        Console.WriteLine($"{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══════════════════════════════════════════{ConsoleUI.ColorReset}\n");

        var allResults = new List<(string category, List<string> findings)>();

        // 1. Загружаем базу сигнатур
        ConsoleUI.Log("+ Загрузка базы сигнатур...", true);
        HashScanner.LoadDatabase();

        // 2. Анализ драйверов
        Console.WriteLine($"\n{ConsoleUI.ColorYellow}[1/7] Анализ драйверов...{ConsoleUI.ColorReset}");
        var driverFindings = DriverAnalyzer.QuickSuspiciousCheck();
        if (driverFindings.Count > 0)
        {
            allResults.Add(("Подозрительные драйверы", driverFindings));
        }
        ConsoleUI.Log($"  Найдено: {driverFindings.Count}", driverFindings.Count == 0);

        // 3. Анализ сервисов
        Console.WriteLine($"\n{ConsoleUI.ColorYellow}[2/7] Анализ сервисов...{ConsoleUI.ColorReset}");
        var serviceFindings = ServiceAnalyzer.QuickSuspiciousCheck();
        if (serviceFindings.Count > 0)
        {
            allResults.Add(("Подозрительные сервисы", serviceFindings));
        }
        ConsoleUI.Log($"  Найдено: {serviceFindings.Count}", serviceFindings.Count == 0);

        // 4. Анализ процессов
        Console.WriteLine($"\n{ConsoleUI.ColorYellow}[3/7] Анализ процессов...{ConsoleUI.ColorReset}");
        var processes = ProcessAnalyzer.AnalyzeAllProcesses();
        var suspiciousProcesses = processes.Where(p => p.IsSuspicious).ToList();
        if (suspiciousProcesses.Count > 0)
        {
            var processFindings = suspiciousProcesses.Select(p =>
                $"{p.Name} (PID:{p.ProcessId}): {string.Join(", ", p.Warnings)}").ToList();
            allResults.Add(("Подозрительные процессы", processFindings));
        }
        ConsoleUI.Log($"  Найдено: {suspiciousProcesses.Count}", suspiciousProcesses.Count == 0);

        // 5. Сканирование памяти (только RWX регионы)
        Console.WriteLine($"\n{ConsoleUI.ColorYellow}[4/7] Сканирование памяти (RWX)...{ConsoleUI.ColorReset}");
        var memoryFindings = new List<string>();
        int processesWithRWX = 0;

        foreach (var proc in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                if (proc.Id <= 4) continue;

                var memInfo = MemoryScanner.ScanProcessMemory(proc.Id);
                if (memInfo.RWXRegions > 0)
                {
                    processesWithRWX++;
                    memoryFindings.Add($"{memInfo.ProcessName} (PID:{memInfo.ProcessId}): {memInfo.RWXRegions} RWX регионов");
                }
            }
            catch { }
        }

        if (memoryFindings.Count > 0)
        {
            allResults.Add(("Процессы с RWX памятью", memoryFindings.Take(10).ToList()));
        }
        ConsoleUI.Log($"  Процессов с RWX: {processesWithRWX}", processesWithRWX == 0);

        // 6. Сетевые соединения
        Console.WriteLine($"\n{ConsoleUI.ColorYellow}[5/7] Анализ сети...{ConsoleUI.ColorReset}");
        var networkFindings = NetworkAnalyzer.QuickSuspiciousCheck();
        if (networkFindings.Count > 0)
        {
            allResults.Add(("Подозрительные соединения", networkFindings));
        }
        ConsoleUI.Log($"  Найдено: {networkFindings.Count}", networkFindings.Count == 0);

        // 7. Проверка хуков
        Console.WriteLine($"\n{ConsoleUI.ColorYellow}[6/7] Проверка хуков...{ConsoleUI.ColorReset}");
        var hookResult = HookDetector.ScanForHooks();
        if (hookResult.DetectedHooks.Count > 0)
        {
            var hookFindings = hookResult.DetectedHooks.Select(h =>
                $"{h.ModuleName}!{h.FunctionName}: {h.HookType}").ToList();
            allResults.Add(("Обнаруженные хуки", hookFindings));
        }
        ConsoleUI.Log($"  Найдено: {hookResult.DetectedHooks.Count}", hookResult.DetectedHooks.Count == 0);

        // 8. Проверка инъекций
        Console.WriteLine($"\n{ConsoleUI.ColorYellow}[7/7] Проверка инъекций...{ConsoleUI.ColorReset}");
        var injectionFindings = new List<string>();

        foreach (var proc in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                if (proc.Id <= 4) continue;

                var scanResult = InjectionDetector.ScanProcess(proc.Id);
                if (scanResult.SuspiciousModules.Count > 0 || scanResult.HiddenPEAddresses.Count > 0)
                {
                    string finding = $"{scanResult.ProcessName} (PID:{scanResult.ProcessId}): ";
                    if (scanResult.SuspiciousModules.Count > 0)
                    {
                        finding += $"{scanResult.SuspiciousModules.Count} подозрительных модулей";
                    }
                    if (scanResult.HiddenPEAddresses.Count > 0)
                    {
                        finding += $" {scanResult.HiddenPEAddresses.Count} скрытых PE";
                    }
                    injectionFindings.Add(finding);
                }
            }
            catch { }
        }

        if (injectionFindings.Count > 0)
        {
            allResults.Add(("Возможные инъекции", injectionFindings.Take(10).ToList()));
        }
        ConsoleUI.Log($"  Найдено: {injectionFindings.Count}", injectionFindings.Count == 0);

        // ==================== ИТОГОВЫЙ ОТЧЁТ ====================
        Console.WriteLine($"\n\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══════════════════════════════════════════{ConsoleUI.ColorReset}");
        Console.WriteLine($"{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}              ИТОГОВЫЙ ОТЧЁТ               {ConsoleUI.ColorReset}");
        Console.WriteLine($"{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══════════════════════════════════════════{ConsoleUI.ColorReset}\n");

        if (allResults.Count == 0)
        {
            Console.WriteLine($"{ConsoleUI.ColorGreen}{ConsoleUI.ColorBold}");
            Console.WriteLine("  ╔══════════════════════════════════════╗");
            Console.WriteLine("  ║    ПОДОЗРИТЕЛЬНАЯ АКТИВНОСТЬ НЕ     ║");
            Console.WriteLine("  ║           ОБНАРУЖЕНА                 ║");
            Console.WriteLine("  ╚══════════════════════════════════════╝");
            Console.WriteLine($"{ConsoleUI.ColorReset}");
        }
        else
        {
            Console.WriteLine($"{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}");
            Console.WriteLine("  ╔══════════════════════════════════════╗");
            Console.WriteLine($"  ║   ОБНАРУЖЕНО КАТЕГОРИЙ: {allResults.Count,-13}║");
            Console.WriteLine("  ╚══════════════════════════════════════╝");
            Console.WriteLine($"{ConsoleUI.ColorReset}");

            foreach (var (category, findings) in allResults)
            {
                Console.WriteLine($"\n{ConsoleUI.ColorOrange}▌ {category} ({findings.Count}):{ConsoleUI.ColorReset}");
                foreach (var finding in findings.Take(5))
                {
                    Console.WriteLine($"  • {finding}");
                }
                if (findings.Count > 5)
                {
                    Console.WriteLine($"  ... и ещё {findings.Count - 5}");
                }
            }

            // Общий уровень угрозы
            int threatLevel = allResults.Sum(r => r.findings.Count);
            string threatColor = threatLevel >= 10 ? ConsoleUI.ColorRed :
                                threatLevel >= 5 ? ConsoleUI.ColorOrange :
                                ConsoleUI.ColorYellow;

            Console.WriteLine($"\n{threatColor}");
            Console.WriteLine($"  Общий уровень угрозы: {GetThreatLevel(threatLevel)}");
            Console.WriteLine($"{ConsoleUI.ColorReset}");
        }

        ConsoleUI.Pause();
    }

    private static string GetThreatLevel(int findings)
    {
        if (findings == 0) return "ЧИСТО";
        if (findings <= 3) return "НИЗКИЙ";
        if (findings <= 7) return "СРЕДНИЙ";
        if (findings <= 15) return "ВЫСОКИЙ";
        return "КРИТИЧЕСКИЙ";
    }
}
