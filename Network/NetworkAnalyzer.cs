using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using CustosAC.UI;
using CustosAC.WinAPI;

namespace CustosAC.Network;

/// <summary>
/// Анализатор сетевых соединений
/// </summary>
public static class NetworkAnalyzer
{
    #region Result Classes

    public class ConnectionInfo
    {
        public string Protocol { get; set; } = "";
        public string LocalAddress { get; set; } = "";
        public int LocalPort { get; set; }
        public string RemoteAddress { get; set; } = "";
        public int RemotePort { get; set; }
        public string State { get; set; } = "";
        public int ProcessId { get; set; }
        public string? ProcessName { get; set; }
        public bool IsSuspicious { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public class NetworkScanResult
    {
        public List<ConnectionInfo> Connections { get; set; } = new();
        public List<ConnectionInfo> SuspiciousConnections { get; set; } = new();
        public int TotalConnections { get; set; }
    }

    #endregion

    // Подозрительные порты (часто используются C2 серверами читов)
    private static readonly int[] SuspiciousPorts = new[]
    {
        6666, 6667, 6668, 6669,  // IRC (иногда C2)
        31337, 31338,            // "Elite" порты
        12345, 54321,            // Распространенные backdoor порты
        1337, 13337,             // Хакерские порты
        4444, 5555,              // Metasploit/RAT порты
        8080, 8888,              // Нестандартные HTTP прокси
        9999, 7777,              // Игровые/читерские порты
    };

    // TCP состояния
    private static readonly string[] TcpStates = new[]
    {
        "", "CLOSED", "LISTEN", "SYN_SENT", "SYN_RCVD",
        "ESTABLISHED", "FIN_WAIT1", "FIN_WAIT2", "CLOSE_WAIT",
        "CLOSING", "LAST_ACK", "TIME_WAIT", "DELETE_TCB"
    };

    /// <summary>
    /// Получение активных TCP соединений
    /// </summary>
    public static List<ConnectionInfo> GetTcpConnections()
    {
        var connections = new List<ConnectionInfo>();

        try
        {
            // Используем .NET API для получения соединений
            var tcpConnections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();

            foreach (var conn in tcpConnections)
            {
                var info = new ConnectionInfo
                {
                    Protocol = "TCP",
                    LocalAddress = conn.LocalEndPoint.Address.ToString(),
                    LocalPort = conn.LocalEndPoint.Port,
                    RemoteAddress = conn.RemoteEndPoint.Address.ToString(),
                    RemotePort = conn.RemoteEndPoint.Port,
                    State = conn.State.ToString()
                };

                connections.Add(info);
            }

            // Также получаем слушающие порты
            var tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();

            foreach (var listener in tcpListeners)
            {
                var info = new ConnectionInfo
                {
                    Protocol = "TCP",
                    LocalAddress = listener.Address.ToString(),
                    LocalPort = listener.Port,
                    RemoteAddress = "*",
                    RemotePort = 0,
                    State = "LISTENING"
                };

                connections.Add(info);
            }
        }
        catch (Exception ex)
        {
            ConsoleUI.Log($"- Ошибка получения TCP соединений: {ex.Message}", false);
        }

        // Пытаемся привязать PID через netstat
        try
        {
            EnrichWithProcessInfo(connections);
        }
        catch { }

        return connections;
    }

    /// <summary>
    /// Получение UDP слушателей
    /// </summary>
    public static List<ConnectionInfo> GetUdpListeners()
    {
        var connections = new List<ConnectionInfo>();

        try
        {
            var udpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();

            foreach (var listener in udpListeners)
            {
                var info = new ConnectionInfo
                {
                    Protocol = "UDP",
                    LocalAddress = listener.Address.ToString(),
                    LocalPort = listener.Port,
                    RemoteAddress = "*",
                    RemotePort = 0,
                    State = "LISTENING"
                };

                connections.Add(info);
            }
        }
        catch (Exception ex)
        {
            ConsoleUI.Log($"- Ошибка получения UDP слушателей: {ex.Message}", false);
        }

        return connections;
    }

    /// <summary>
    /// Обогащение информации о процессах через netstat
    /// </summary>
    private static void EnrichWithProcessInfo(List<ConnectionInfo> connections)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null) return;

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) continue;

                // Парсим формат: Protocol LocalAddr ForeignAddr State PID
                string protocol = parts[0];
                if (protocol != "TCP" && protocol != "UDP") continue;

                string localAddr = parts[1];
                int pid;

                if (protocol == "TCP" && parts.Length >= 5)
                {
                    if (!int.TryParse(parts[4], out pid)) continue;
                }
                else if (protocol == "UDP" && parts.Length >= 4)
                {
                    if (!int.TryParse(parts[3], out pid)) continue;
                }
                else continue;

                // Ищем соответствующее соединение
                var localParts = localAddr.Split(':');
                if (localParts.Length != 2) continue;

                if (!int.TryParse(localParts[1], out int localPort)) continue;

                var conn = connections.FirstOrDefault(c =>
                    c.LocalPort == localPort && c.ProcessId == 0);

                if (conn != null)
                {
                    conn.ProcessId = pid;
                    try
                    {
                        conn.ProcessName = System.Diagnostics.Process.GetProcessById(pid).ProcessName;
                    }
                    catch { }
                }
            }
        }
        catch
        {
            // Игнорируем ошибки netstat
        }
    }

    /// <summary>
    /// Анализ соединений на подозрительность
    /// </summary>
    public static NetworkScanResult AnalyzeConnections()
    {
        var result = new NetworkScanResult();

        // Получаем TCP и UDP соединения
        result.Connections.AddRange(GetTcpConnections());
        result.Connections.AddRange(GetUdpListeners());
        result.TotalConnections = result.Connections.Count;

        // Загружаем подозрительные имена процессов
        Scanner.HashScanner.LoadDatabase();

        foreach (var conn in result.Connections)
        {
            bool suspicious = false;

            // Проверка 1: Подозрительный порт
            if (SuspiciousPorts.Contains(conn.LocalPort) || SuspiciousPorts.Contains(conn.RemotePort))
            {
                conn.Warnings.Add($"Подозрительный порт");
                suspicious = true;
            }

            // Проверка 2: Подозрительное имя процесса
            if (!string.IsNullOrEmpty(conn.ProcessName))
            {
                var (match, keyword) = Scanner.HashScanner.CheckKeywords(conn.ProcessName);
                if (match)
                {
                    conn.Warnings.Add($"Подозрительный процесс: '{keyword}'");
                    suspicious = true;
                }
            }

            // Проверка 3: Нестандартные исходящие соединения от системных процессов
            if (conn.ProcessName != null &&
                conn.State == "Established" &&
                !string.IsNullOrEmpty(conn.RemoteAddress) &&
                conn.RemoteAddress != "127.0.0.1" &&
                conn.RemoteAddress != "::1")
            {
                var systemProcesses = new[] { "svchost", "services", "lsass", "csrss" };
                if (systemProcesses.Contains(conn.ProcessName.ToLowerInvariant()))
                {
                    // Системные процессы могут иметь соединения, но это стоит отметить
                    conn.Warnings.Add("Исходящее соединение от системного процесса");
                }
            }

            // Проверка 4: Соединения на высокие порты от неизвестных процессов
            if (conn.LocalPort > 49152 && // Динамические порты
                conn.State == "LISTENING" &&
                string.IsNullOrEmpty(conn.ProcessName))
            {
                conn.Warnings.Add("Слушающий порт без известного процесса");
                suspicious = true;
            }

            if (suspicious)
            {
                conn.IsSuspicious = true;
                result.SuspiciousConnections.Add(conn);
            }
        }

        return result;
    }

    /// <summary>
    /// Сканирование сетевых соединений
    /// </summary>
    public static void ScanNetwork()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ АНАЛИЗ СЕТЕВЫХ СОЕДИНЕНИЙ ═══{ConsoleUI.ColorReset}\n");

        ConsoleUI.Log("+ Получение активных соединений...", true);

        var result = AnalyzeConnections();

        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Статистика:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  Всего соединений: {result.TotalConnections}");
        Console.WriteLine($"  Подозрительных: {result.SuspiciousConnections.Count}");

        if (result.SuspiciousConnections.Count > 0)
        {
            Console.WriteLine($"\n{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}══ ПОДОЗРИТЕЛЬНЫЕ СОЕДИНЕНИЯ ══{ConsoleUI.ColorReset}\n");

            foreach (var conn in result.SuspiciousConnections)
            {
                Console.WriteLine($"{ConsoleUI.ColorRed}► {conn.Protocol} {conn.LocalAddress}:{conn.LocalPort}{ConsoleUI.ColorReset}");

                if (!string.IsNullOrEmpty(conn.RemoteAddress) && conn.RemoteAddress != "*")
                {
                    Console.WriteLine($"  -> {conn.RemoteAddress}:{conn.RemotePort}");
                }

                Console.WriteLine($"  Состояние: {conn.State}");

                if (!string.IsNullOrEmpty(conn.ProcessName))
                {
                    Console.WriteLine($"  Процесс: {conn.ProcessName} (PID: {conn.ProcessId})");
                }
                else if (conn.ProcessId > 0)
                {
                    Console.WriteLine($"  PID: {conn.ProcessId}");
                }

                foreach (var warning in conn.Warnings)
                {
                    Console.WriteLine($"  {ConsoleUI.ColorOrange}! {warning}{ConsoleUI.ColorReset}");
                }
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine($"\n{ConsoleUI.ColorGreen}+ Подозрительных соединений не обнаружено{ConsoleUI.ColorReset}");
        }

        // Вывод активных соединений (топ по процессам)
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Активные ESTABLISHED соединения:{ConsoleUI.ColorReset}");

        var established = result.Connections
            .Where(c => c.State == "Established")
            .GroupBy(c => c.ProcessName ?? $"PID:{c.ProcessId}")
            .OrderByDescending(g => g.Count())
            .Take(10);

        foreach (var group in established)
        {
            Console.WriteLine($"  {group.Key}: {group.Count()} соединений");
        }

        // Слушающие порты
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}Слушающие порты (LISTENING):{ConsoleUI.ColorReset}");

        var listening = result.Connections
            .Where(c => c.State == "LISTENING" || c.State == "Listen")
            .OrderBy(c => c.LocalPort)
            .Take(20);

        foreach (var conn in listening)
        {
            string proc = !string.IsNullOrEmpty(conn.ProcessName) ? conn.ProcessName : $"PID:{conn.ProcessId}";
            string indicator = conn.IsSuspicious ? $"{ConsoleUI.ColorRed}!{ConsoleUI.ColorReset}" : " ";
            Console.WriteLine($"  {indicator} :{conn.LocalPort,-6} {conn.Protocol,-4} {proc}");
        }

        ConsoleUI.Pause();
    }

    /// <summary>
    /// Быстрая проверка подозрительных соединений
    /// </summary>
    public static List<string> QuickSuspiciousCheck()
    {
        var suspicious = new List<string>();
        var result = AnalyzeConnections();

        foreach (var conn in result.SuspiciousConnections)
        {
            string desc = $"{conn.Protocol} {conn.LocalAddress}:{conn.LocalPort}";
            if (!string.IsNullOrEmpty(conn.ProcessName))
                desc += $" ({conn.ProcessName})";
            desc += $": {string.Join(", ", conn.Warnings)}";
            suspicious.Add(desc);
        }

        return suspicious;
    }
}
