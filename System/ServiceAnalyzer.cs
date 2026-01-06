using System.ServiceProcess;
using Microsoft.Win32;
using CustosAC.UI;
using CustosAC.Scanner;

namespace CustosAC.SystemAnalysis;

/// <summary>
/// Анализатор системных сервисов
/// </summary>
public static class ServiceAnalyzer
{
    #region Result Classes

    public class ServiceInfo
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? ImagePath { get; set; }
        public string Status { get; set; } = "";
        public string StartType { get; set; } = "";
        public bool IsSuspicious { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public class ServiceScanResult
    {
        public List<ServiceInfo> Services { get; set; } = new();
        public List<ServiceInfo> SuspiciousServices { get; set; } = new();
        public int TotalServices { get; set; }
    }

    #endregion

    // Подозрительные ключевые слова в именах сервисов
    private static readonly string[] SuspiciousKeywords = new[]
    {
        "hack", "cheat", "inject", "bypass", "loader",
        "mapper", "exploit", "rootkit", "vulnerable",
        "external", "internal", "driver", "capcom"
    };

    /// <summary>
    /// Получение информации о сервисах через реестр
    /// </summary>
    public static ServiceScanResult GetAllServices()
    {
        var result = new ServiceScanResult();

        // Загружаем базу сигнатур
        HashScanner.LoadDatabase();

        try
        {
            // Получаем сервисы из реестра
            using var servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");

            if (servicesKey == null)
                return result;

            foreach (string serviceName in servicesKey.GetSubKeyNames())
            {
                try
                {
                    using var serviceKey = servicesKey.OpenSubKey(serviceName);
                    if (serviceKey == null) continue;

                    // Получаем тип сервиса
                    var type = serviceKey.GetValue("Type");
                    if (type == null) continue;

                    int typeValue = Convert.ToInt32(type);

                    // Фильтруем: только драйверы (1, 2) и сервисы (16, 32)
                    if (typeValue != 1 && typeValue != 2 && typeValue != 16 && typeValue != 32)
                        continue;

                    var service = new ServiceInfo
                    {
                        Name = serviceName,
                        DisplayName = serviceKey.GetValue("DisplayName")?.ToString() ?? serviceName,
                        ImagePath = serviceKey.GetValue("ImagePath")?.ToString()
                    };

                    // Тип запуска
                    var start = serviceKey.GetValue("Start");
                    if (start != null)
                    {
                        service.StartType = Convert.ToInt32(start) switch
                        {
                            0 => "Boot",
                            1 => "System",
                            2 => "Auto",
                            3 => "Manual",
                            4 => "Disabled",
                            _ => "Unknown"
                        };
                    }

                    // Проверяем статус через ServiceController
                    try
                    {
                        using var sc = new ServiceController(serviceName);
                        service.Status = sc.Status.ToString();
                    }
                    catch
                    {
                        service.Status = "Unknown";
                    }

                    // Анализируем на подозрительность
                    AnalyzeService(service);

                    result.Services.Add(service);
                    result.TotalServices++;

                    if (service.IsSuspicious)
                    {
                        result.SuspiciousServices.Add(service);
                    }
                }
                catch
                {
                    // Игнорируем ошибки доступа к отдельным сервисам
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleUI.Log($"- Ошибка чтения сервисов: {ex.Message}", false);
        }

        return result;
    }

    private static void AnalyzeService(ServiceInfo service)
    {
        string lowerName = service.Name.ToLowerInvariant();
        string lowerDisplayName = service.DisplayName?.ToLowerInvariant() ?? "";
        string lowerPath = service.ImagePath?.ToLowerInvariant() ?? "";

        // Проверка 1: Подозрительные ключевые слова в имени
        foreach (var keyword in SuspiciousKeywords)
        {
            if (lowerName.Contains(keyword) || lowerDisplayName.Contains(keyword))
            {
                service.IsSuspicious = true;
                service.Warnings.Add($"Подозрительное имя: содержит '{keyword}'");
                break;
            }
        }

        // Проверка 2: Проверяем по базе сигнатур
        var (match, matchedKeyword) = HashScanner.CheckKeywords(service.Name);
        if (match)
        {
            service.IsSuspicious = true;
            service.Warnings.Add($"Совпадение с базой: '{matchedKeyword}'");
        }

        // Проверка 3: Путь к исполняемому файлу
        if (!string.IsNullOrEmpty(service.ImagePath))
        {
            // Сервис из временной папки
            if (lowerPath.Contains(@"\temp\") || lowerPath.Contains(@"\tmp\"))
            {
                service.IsSuspicious = true;
                service.Warnings.Add("Запускается из временной папки");
            }

            // Сервис из AppData
            if (lowerPath.Contains(@"\appdata\") && !lowerPath.Contains(@"\microsoft\"))
            {
                service.Warnings.Add("Запускается из AppData");
            }

            // Сервис из Downloads
            if (lowerPath.Contains(@"\downloads\"))
            {
                service.IsSuspicious = true;
                service.Warnings.Add("Запускается из папки Downloads");
            }

            // Сервис без указания пути (только имя файла)
            if (!lowerPath.Contains(@"\") && !lowerPath.StartsWith("system32"))
            {
                service.Warnings.Add("Неполный путь к исполняемому файлу");
            }
        }

        // Проверка 4: Имя совпадает с известным системным, но путь другой
        var systemServices = new[] { "svchost", "services", "lsass", "csrss" };
        if (systemServices.Any(s => lowerName.Contains(s)))
        {
            if (!string.IsNullOrEmpty(lowerPath) &&
                !lowerPath.Contains(@"\windows\system32\"))
            {
                service.IsSuspicious = true;
                service.Warnings.Add("Возможная маскировка под системный сервис");
            }
        }
    }

    /// <summary>
    /// Сканирование сервисов
    /// </summary>
    public static void ScanServices()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ АНАЛИЗ СИСТЕМНЫХ СЕРВИСОВ ═══{ConsoleUI.ColorReset}\n");

        ConsoleUI.Log("+ Получение списка сервисов...", true);

        var result = GetAllServices();

        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Статистика:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  Всего сервисов/драйверов: {result.TotalServices}");
        Console.WriteLine($"  Подозрительных: {result.SuspiciousServices.Count}");

        if (result.SuspiciousServices.Count > 0)
        {
            Console.WriteLine($"\n{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}══ ПОДОЗРИТЕЛЬНЫЕ СЕРВИСЫ ══{ConsoleUI.ColorReset}\n");

            foreach (var service in result.SuspiciousServices)
            {
                Console.WriteLine($"{ConsoleUI.ColorRed}► {service.Name}{ConsoleUI.ColorReset}");
                Console.WriteLine($"  Отображаемое имя: {service.DisplayName}");
                Console.WriteLine($"  Путь: {service.ImagePath ?? "(не указан)"}");
                Console.WriteLine($"  Статус: {service.Status}");
                Console.WriteLine($"  Тип запуска: {service.StartType}");

                foreach (var warning in service.Warnings)
                {
                    Console.WriteLine($"  {ConsoleUI.ColorOrange}! {warning}{ConsoleUI.ColorReset}");
                }
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine($"\n{ConsoleUI.ColorGreen}+ Подозрительных сервисов не обнаружено{ConsoleUI.ColorReset}");
        }

        // Вывод недавно созданных/измененных сервисов
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Запущенные нестандартные сервисы:{ConsoleUI.ColorReset}");

        var runningSuspicious = result.Services
            .Where(s => s.Status == "Running" &&
                       s.Warnings.Count > 0 &&
                       !s.IsSuspicious)
            .Take(20);

        if (runningSuspicious.Any())
        {
            foreach (var service in runningSuspicious)
            {
                Console.WriteLine($"  {ConsoleUI.ColorYellow}?{ConsoleUI.ColorReset} {service.Name}: {string.Join(", ", service.Warnings)}");
            }
        }
        else
        {
            Console.WriteLine("  Нет");
        }

        ConsoleUI.Pause();
    }

    /// <summary>
    /// Быстрая проверка на подозрительные сервисы
    /// </summary>
    public static List<string> QuickSuspiciousCheck()
    {
        var suspicious = new List<string>();
        var result = GetAllServices();

        foreach (var service in result.SuspiciousServices)
        {
            suspicious.Add($"{service.Name}: {string.Join(", ", service.Warnings)}");
        }

        return suspicious;
    }
}
