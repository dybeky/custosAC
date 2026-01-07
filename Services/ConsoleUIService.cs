using System.Diagnostics;
using System.Runtime.InteropServices;
using CustosAC.Abstractions;
using CustosAC.Configuration;
using CustosAC.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustosAC.Services;

/// <summary>
/// Реализация консольного UI
/// </summary>
public class ConsoleUIService : IConsoleUI
{
    private bool _isAdmin;
    private readonly AppSettings _settings;
    private readonly ILogger<ConsoleUIService> _logger;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    public ConsoleUIService(IOptions<AppSettings> settings, ILogger<ConsoleUIService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public void SetAdminStatus(bool isAdmin)
    {
        _isAdmin = isAdmin;
    }

    public void SetupConsole()
    {
        try
        {
            // Устанавливаем размер окна консоли
            var psi = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = $"/c mode con: cols={_settings.Console.Width} lines={_settings.Console.Height}",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi)?.WaitForExit();

            // Включаем обработку ANSI escape-последовательностей
            var handle = GetStdHandle(STD_OUTPUT_HANDLE);
            if (GetConsoleMode(handle, out uint mode))
            {
                mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                SetConsoleMode(handle, mode);
            }

            // Устанавливаем кодировку UTF-8
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Console setup partially failed - some features may not be supported");
        }
    }

    public void ClearScreen()
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            Console.Write("\x1b[2J\x1b[H");
        }
    }

    public void PrintHeader()
    {
        ClearScreen();
        Console.WriteLine();

        Console.WriteLine($"  {AnsiColors.Orange}╭──────────────────────────────────────────────────────────╮{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│                                                          │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│     ██████╗██╗   ██╗███████╗████████╗ ██████╗ ███████╗   │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│    ██╔════╝██║   ██║██╔════╝╚══██╔══╝██╔═══██╗██╔════╝   │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│    ██║     ██║   ██║███████╗   ██║   ██║   ██║███████╗   │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│    ██║     ██║   ██║╚════██║   ██║   ██║   ██║╚════██║   │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│    ╚██████╗╚██████╔╝███████║   ██║   ╚██████╔╝███████║   │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│     ╚═════╝ ╚═════╝ ╚══════╝   ╚═╝    ╚═════╝ ╚══════╝   │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│                                                          │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│               ✦ sdelano s lubovyu ✦                      │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│                                                          │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}╰──────────────────────────────────────────────────────────╯{AnsiColors.Reset}");
        Console.WriteLine();

        if (_isAdmin)
        {
            Console.WriteLine($"  {AnsiColors.SuccessPrefix} Статус: {AnsiColors.BoldGreen("Администратор")}");
        }
        else
        {
            Console.WriteLine($"  {AnsiColors.ErrorPrefix} Статус: {AnsiColors.BoldRed("Отсутствуют права администратора!")}");
        }
        Console.WriteLine($"  {AnsiColors.InfoPrefix} Дата: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        Console.WriteLine();
    }

    public void PrintMenu(string title, string[] options, bool showBack)
    {
        string centeredTitle = new string(' ', _settings.Console.MenuPadding) + title;
        Console.WriteLine($"\n{AnsiColors.BoldYellow(centeredTitle)}\n");

        int menuNumber = 1;
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i].Contains("────"))
            {
                Console.WriteLine($"      {AnsiColors.Dim}{options[i]}{AnsiColors.Reset}");
            }
            else
            {
                Console.WriteLine($"  {AnsiColors.BoldCyan($"[{menuNumber}]")} {AnsiColors.ArrowPrefix} {options[i]}");
                menuNumber++;
            }
        }

        if (showBack)
        {
            Console.WriteLine($"\n  {AnsiColors.Magenta}{AnsiColors.Bold}[0]{AnsiColors.Reset} {AnsiColors.Magenta}< Назад{AnsiColors.Reset}");
        }
        else
        {
            Console.WriteLine($"\n  {AnsiColors.Red}{AnsiColors.Bold}[0]{AnsiColors.Reset} {AnsiColors.Red}X Выход{AnsiColors.Reset}");
        }
        Console.WriteLine();
    }

    public int GetChoice(int maxOption)
    {
        while (true)
        {
            Console.Write($"\n{AnsiColors.BoldGreen("[>]")} Выберите опцию [0-{maxOption}]: ");

            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
                return 0;

            if (int.TryParse(input.Trim(), out int choice) && choice >= 0 && choice <= maxOption)
            {
                return choice;
            }

            Console.WriteLine($"\n{AnsiColors.WarningPrefix} {AnsiColors.BoldRed($"Ошибка: Введите число от 0 до {maxOption}")}");
        }
    }

    public void Log(string message, bool success)
    {
        string prefix = success ? AnsiColors.SuccessPrefix : AnsiColors.ErrorPrefix;
        Console.WriteLine($"  {prefix} {message}");
    }

    public void Pause()
    {
        Console.WriteLine($"\n{AnsiColors.BoldGreen("[>]")} Нажмите Enter для продолжения...");
        Console.ReadLine();
    }

    public void DisplayFilesWithPagination(List<string> files, int itemsPerPage)
    {
        if (files.Count == 0)
        {
            Console.WriteLine($"\n{AnsiColors.Warning("Нет файлов для отображения")}");
            return;
        }

        int totalPages = (files.Count + itemsPerPage - 1) / itemsPerPage;
        int currentPage = 0;

        while (true)
        {
            ClearScreen();

            Console.WriteLine($"\n{AnsiColors.BoldCyan($"═══ ПРОСМОТР ФАЙЛОВ (Страница {currentPage + 1} из {totalPages}) ═══")}");
            Console.WriteLine($"{AnsiColors.Warning($"Всего файлов: {files.Count}")}\n");

            int start = currentPage * itemsPerPage;
            int end = Math.Min(start + itemsPerPage, files.Count);

            for (int i = start; i < end; i++)
            {
                Console.WriteLine($"  {AnsiColors.Highlight($"[{i + 1}]")} {files[i]}");
            }

            Console.WriteLine($"\n{AnsiColors.Cyan}{AnsiColors.SeparatorLong}{AnsiColors.Reset}");
            Console.WriteLine($"\n{AnsiColors.BoldYellow("Навигация:")}");

            if (currentPage > 0)
            {
                Console.WriteLine($"  {AnsiColors.Success("[P]")} - Предыдущая страница");
            }
            if (currentPage < totalPages - 1)
            {
                Console.WriteLine($"  {AnsiColors.Success("[N]")} - Следующая страница");
            }
            Console.WriteLine($"  {AnsiColors.Error("[0]")} - Вернуться назад");

            Console.Write($"\n{AnsiColors.BoldGreen("[>]")} Выберите действие: ");
            string? input = Console.ReadLine()?.ToLower().Trim();

            switch (input)
            {
                case "n":
                    if (currentPage < totalPages - 1)
                    {
                        currentPage++;
                    }
                    else
                    {
                        Console.WriteLine($"{AnsiColors.WarningPrefix} {AnsiColors.Warning("Это последняя страница")}");
                        Thread.Sleep(_settings.Timeouts.UiDelayMs);
                    }
                    break;
                case "p":
                    if (currentPage > 0)
                    {
                        currentPage--;
                    }
                    else
                    {
                        Console.WriteLine($"{AnsiColors.WarningPrefix} {AnsiColors.Warning("Это первая страница")}");
                        Thread.Sleep(_settings.Timeouts.UiDelayMs);
                    }
                    break;
                case "0":
                case "":
                case null:
                    return;
                default:
                    Console.WriteLine($"{AnsiColors.ErrorPrefix} {AnsiColors.Error("Неверная команда")}");
                    Thread.Sleep(_settings.Timeouts.UiDelayMs);
                    break;
            }
        }
    }

    public void PrintCleanupMessage()
    {
        Console.WriteLine();
        Console.WriteLine($"  {AnsiColors.Orange}╭──────────────────────────────────────────────────────────╮{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│                                                          │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│      ██████╗ ██╗   ██╗███████╗                           │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│      ██╔══██╗╚██╗ ██╔╝██╔════╝                           │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│      ██████╔╝ ╚████╔╝ █████╗                             │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│      ██╔══██╗  ╚██╔╝  ██╔══╝                             │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│      ██████╔╝   ██║   ███████╗                           │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│      ╚═════╝    ╚═╝   ╚══════╝                           │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}│                                                          │{AnsiColors.Reset}");
        Console.WriteLine($"  {AnsiColors.Orange}╰──────────────────────────────────────────────────────────╯{AnsiColors.Reset}");
        Console.WriteLine();
    }

    public void PrintProgress(string operation, int current, int total)
    {
        int percentage = total > 0 ? (current * 100) / total : 0;
        Console.Write($"\r  {AnsiColors.ScanPrefix} {operation}: [{percentage}%] {current}/{total}    ");
    }

    public void PrintEmptyLine()
    {
        Console.WriteLine();
    }

    public void PrintSeparator()
    {
        Console.WriteLine($"  {AnsiColors.Dim}{AnsiColors.SeparatorMedium}{AnsiColors.Reset}");
    }

    public void PrintSuccess(string message)
    {
        Console.WriteLine($"  {AnsiColors.Success(message)}");
    }

    public void PrintError(string message)
    {
        Console.WriteLine($"  {AnsiColors.Error(message)}");
    }

    public void PrintWarning(string message)
    {
        Console.WriteLine($"  {AnsiColors.WarningPrefix} {AnsiColors.Warning(message)}");
    }

    public void PrintInfo(string message)
    {
        Console.WriteLine($"  {AnsiColors.InfoPrefix} {message}");
    }

    public void PrintHighlight(string message)
    {
        Console.WriteLine($"  {AnsiColors.Highlight(message)}");
    }

    public void PrintSectionHeader(string title)
    {
        Console.WriteLine($"\n{AnsiColors.BoldCyan($"═══ {title.ToUpper()} ═══")}\n");
    }

    public void PrintListItem(string text)
    {
        Console.WriteLine($"  {AnsiColors.ArrowPrefix} {text}");
    }

    public void PrintHint(string title)
    {
        Console.WriteLine($"\n{AnsiColors.BoldYellow(title)}");
    }

    public void PrintBox(string[] lines, bool success)
    {
        string color = success ? AnsiColors.Green : AnsiColors.Red;
        string bold = AnsiColors.Bold;
        string reset = AnsiColors.Reset;

        Console.WriteLine($"{color}{bold}╔══════════════════════════════════════════════════╗{reset}");
        foreach (var line in lines)
        {
            Console.WriteLine($"{color}║  {line,-48}║{reset}");
        }
        Console.WriteLine($"{color}{bold}╚══════════════════════════════════════════════════╝{reset}");
    }

    public void PrintBoxOrange(string[] lines)
    {
        string color = AnsiColors.Orange;
        string bold = AnsiColors.Bold;
        string reset = AnsiColors.Reset;

        Console.WriteLine($"{color}{bold}╔══════════════════════════════════════════════════╗{reset}");
        foreach (var line in lines)
        {
            Console.WriteLine($"{color}║  {line,-48}║{reset}");
        }
        Console.WriteLine($"{color}{bold}╚══════════════════════════════════════════════════╝{reset}");
    }
}
