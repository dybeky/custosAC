# CustosAC Complete Reform Script
$ErrorActionPreference = "Stop"

Write-Host "=== CustosAC Complete Reform ===" -ForegroundColor Green
Write-Host "Removing all Microsoft.Extensions.* dependencies..." -ForegroundColor Yellow

# Helper function to write files with UTF-8 encoding
function Write-FileUTF8 {
    param($Path, $Content)
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

# 1. AdminService - remove ILogger
Write-Host "1. Updating AdminService.cs..." -ForegroundColor Cyan
$adminServiceContent = @'
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace CustosAC.Services;

[SupportedOSPlatform("windows")]
public class AdminService
{
    private const int CTRL_C_EVENT = 0;
    private const int CTRL_CLOSE_EVENT = 2;

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);
    private delegate bool HandlerRoutine(int ctrlType);

    public bool IsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public void RunAsAdmin()
    {
        if (!IsAdmin())
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "CustosAC.exe",
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(processInfo);
                Environment.Exit(0);
            }
            catch
            {
                Console.WriteLine("Требуются права администратора!");
                Environment.Exit(1);
            }
        }
    }

    public void SetupCloseHandler(Action cleanupAction)
    {
        HandlerRoutine handler = (ctrlType) =>
        {
            if (ctrlType == CTRL_C_EVENT || ctrlType == CTRL_CLOSE_EVENT)
            {
                cleanupAction?.Invoke();
                return true;
            }
            return false;
        };

        SetConsoleCtrlHandler(handler, true);
    }
}
'@
Write-FileUTF8 "$PSScriptRoot\Services\AdminService.cs" $adminServiceContent

# 2. ProcessService
Write-Host "2. Updating ProcessService.cs..." -ForegroundColor Cyan
$processServiceContent = @'
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Versioning;
using CustosAC.Configuration;

namespace CustosAC.Services;

[SupportedOSPlatform("windows")]
public class ProcessService : IDisposable
{
    private readonly ConcurrentBag<Process> _trackedProcesses = new();
    private readonly object _lock = new();
    private readonly AppSettings _settings;

    public ProcessService(AppSettings settings)
    {
        _settings = settings;
    }

    public void TrackProcess(Process process)
    {
        lock (_lock)
        {
            _trackedProcesses.Add(process);
        }
    }

    public void KillAllTrackedProcesses()
    {
        var processesCopy = _trackedProcesses.ToArray();
        foreach (var process in processesCopy)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit(1000);
                }
                process.Dispose();
            }
            catch { }
        }
        _trackedProcesses.Clear();
    }

    public async Task<bool> RunCommandAsync(string command, string args, int timeoutMs)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process == null) return false;

            var completed = await Task.Run(() => process.WaitForExit(timeoutMs));
            return completed && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task OpenUrlAsync(string url)
    {
        var psi = new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        };
        var process = Process.Start(psi);
        if (process != null)
        {
            TrackProcess(process);
        }
        await Task.CompletedTask;
    }

    public async Task OpenFolderAsync(string path)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{path}\"",
            UseShellExecute = false
        };
        var process = Process.Start(psi);
        if (process != null)
        {
            TrackProcess(process);
        }
        await Task.CompletedTask;
    }

    public async Task CopyToClipboardAsync(string text)
    {
        var escapedText = text.Replace("\"", "`\"");
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -Command \"Set-Clipboard -Value \"\"{escapedText}\"\"\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi);
        if (process != null)
        {
            await Task.Run(() => process.WaitForExit(_settings.Timeouts.DefaultProcessTimeoutMs));
        }
    }

    public void Dispose()
    {
        KillAllTrackedProcesses();
    }
}
'@
Write-FileUTF8 "$PSScriptRoot\Services\ProcessService.cs" $processServiceContent

# 3. ConsoleUIService
Write-Host "3. Updating ConsoleUIService.cs..." -ForegroundColor Cyan
$consoleUIContent = @'
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using CustosAC.Configuration;
using CustosAC.Constants;

namespace CustosAC.Services;

[SupportedOSPlatform("windows")]
public class ConsoleUIService
{
    private readonly AppSettings _settings;
    private bool _isAdmin;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, int mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr handle, out int mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int handle);

    public ConsoleUIService(AppSettings settings)
    {
        _settings = settings;
    }

    public void SetAdminStatus(bool isAdmin)
    {
        _isAdmin = isAdmin;
    }

    public void SetupConsole()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var handle = GetStdHandle(-11);
        GetConsoleMode(handle, out int mode);
        mode |= 0x0004;
        SetConsoleMode(handle, mode);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c mode con cols={_settings.Console.Width} lines={_settings.Console.Height}",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi)?.WaitForExit();
        }
        catch { }
    }

    public void ClearScreen() => Console.Clear();
    public void PrintEmptyLine() => Console.WriteLine();

    public void PrintHeader()
    {
        ClearScreen();
        Console.WriteLine(AnsiColors.CYAN + "╔═══════════════════════════════════════════════════════════════════════════════╗" + AnsiColors.RESET);
        Console.WriteLine(AnsiColors.CYAN + "║" + AnsiColors.RESET + "                           " + AnsiColors.BRIGHT_WHITE + "CustosAC - Anti-Cheat Checker" + AnsiColors.RESET + "                        " + AnsiColors.CYAN + "║" + AnsiColors.RESET);
        Console.WriteLine(AnsiColors.CYAN + "║" + AnsiColors.RESET + "                     " + (_isAdmin ? AnsiColors.GREEN + "[ADMIN]" : AnsiColors.RED + "[USER]") + AnsiColors.RESET + "                                           " + AnsiColors.CYAN + "║" + AnsiColors.RESET);
        Console.WriteLine(AnsiColors.CYAN + "╚═══════════════════════════════════════════════════════════════════════════════╝" + AnsiColors.RESET);
        PrintEmptyLine();
    }

    public void PrintMenu(string title, string[] options, bool showExit)
    {
        PrintSectionHeader(title);
        PrintEmptyLine();

        if (showExit)
        {
            Console.WriteLine($"  {AnsiColors.ORANGE}[0]{AnsiColors.RESET} Назад");
        }

        for (int i = 0; i < options.Length; i++)
        {
            Console.WriteLine($"  {AnsiColors.ORANGE}[{i + 1}]{AnsiColors.RESET} {options[i]}");
        }

        PrintEmptyLine();
        Console.Write($"{AnsiColors.BRIGHT_WHITE}Выбор: {AnsiColors.RESET}");
    }

    public int GetChoice(int maxChoice)
    {
        while (true)
        {
            var input = Console.ReadLine()?.Trim();
            if (int.TryParse(input, out int choice) && choice >= 0 && choice <= maxChoice)
            {
                return choice;
            }
            Console.Write($"{AnsiColors.RED}Неверный выбор. Попробуйте снова: {AnsiColors.RESET}");
        }
    }

    public void PrintSectionHeader(string text)
    {
        Console.WriteLine($"{AnsiColors.BRIGHT_CYAN}>>> {text}{AnsiColors.RESET}");
    }

    public void PrintSuccess(string text)
    {
        Console.WriteLine($"{AnsiColors.GREEN}✓ {text}{AnsiColors.RESET}");
    }

    public void PrintError(string text)
    {
        Console.WriteLine($"{AnsiColors.RED}✗ {text}{AnsiColors.RESET}");
    }

    public void PrintWarning(string text)
    {
        Console.WriteLine($"{AnsiColors.ORANGE}{text}{AnsiColors.RESET}");
    }

    public void PrintInfo(string text)
    {
        Console.WriteLine($"{AnsiColors.CYAN}ℹ {text}{AnsiColors.RESET}");
    }

    public void PrintHighlight(string text)
    {
        Console.WriteLine($"{AnsiColors.BRIGHT_WHITE}{text}{AnsiColors.RESET}");
    }

    public void PrintListItem(string text)
    {
        Console.WriteLine($"  • {text}");
    }

    public void PrintHint(string text)
    {
        Console.WriteLine($"{AnsiColors.YELLOW}💡 {text}{AnsiColors.RESET}");
    }

    public void PrintSeparator()
    {
        Console.WriteLine($"{AnsiColors.GRAY}{'─',80}{AnsiColors.RESET}");
    }

    public void PrintBox(string[] lines, bool success = true)
    {
        var color = success ? AnsiColors.GREEN : AnsiColors.RED;
        Console.WriteLine($"{color}╔{'═',78}╗{AnsiColors.RESET}");
        foreach (var line in lines)
        {
            Console.WriteLine($"{color}║{AnsiColors.RESET} {line,-77} {color}║{AnsiColors.RESET}");
        }
        Console.WriteLine($"{color}╚{'═',78}╝{AnsiColors.RESET}");
    }

    public void PrintBoxOrange(string[] lines)
    {
        Console.WriteLine($"{AnsiColors.ORANGE}╔{'═',78}╗{AnsiColors.RESET}");
        foreach (var line in lines)
        {
            Console.WriteLine($"{AnsiColors.ORANGE}║{AnsiColors.RESET} {line,-77} {AnsiColors.ORANGE}║{AnsiColors.RESET}");
        }
        Console.WriteLine($"{AnsiColors.ORANGE}╚{'═',78}╝{AnsiColors.RESET}");
    }

    public void Log(string message, bool success)
    {
        if (success)
            PrintSuccess(message);
        else
            PrintError(message);
    }

    public void Pause()
    {
        PrintEmptyLine();
        Console.Write($"{AnsiColors.GRAY}Нажмите любую клавишу для продолжения...{AnsiColors.RESET}");
        Console.ReadKey(true);
    }

    public void DisplayFilesWithPagination(List<string> files, int itemsPerPage)
    {
        int currentPage = 0;
        int totalPages = (int)Math.Ceiling(files.Count / (double)itemsPerPage);

        while (true)
        {
            ClearScreen();
            PrintHeader();
            PrintSectionHeader($"Результаты (Страница {currentPage + 1}/{totalPages})");
            PrintEmptyLine();

            var pageItems = files.Skip(currentPage * itemsPerPage).Take(itemsPerPage);
            foreach (var item in pageItems)
            {
                PrintListItem(item);
            }

            PrintEmptyLine();
            PrintInfo("[N] Следующая | [P] Предыдущая | [Q] Выход");

            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.N && currentPage < totalPages - 1) currentPage++;
            else if (key == ConsoleKey.P && currentPage > 0) currentPage--;
            else if (key == ConsoleKey.Q) break;
        }
    }

    public void PrintCleanupMessage()
    {
        PrintSuccess("Все процессы закрыты. До свидания!");
    }
}
'@
Write-FileUTF8 "$PSScriptRoot\Services\ConsoleUIService.cs" $consoleUIContent

Write-Host "`n✓ Services updated (AdminService, ProcessService, ConsoleUIService)" -ForegroundColor Green
Write-Host "`nContinue with part 2..." -ForegroundColor Yellow
'@
