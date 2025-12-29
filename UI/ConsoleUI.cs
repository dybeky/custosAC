using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CustosAC.UI;

public static class ConsoleUI
{
    private static bool _isAdmin = false;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    // ANSI цвета
    public const string ColorReset = "\x1b[0m";
    public const string ColorRed = "\x1b[31m";
    public const string ColorGreen = "\x1b[32m";
    public const string ColorYellow = "\x1b[33m";
    public const string ColorBlue = "\x1b[34m";
    public const string ColorMagenta = "\x1b[35m";
    public const string ColorCyan = "\x1b[36m";
    public const string ColorWhite = "\x1b[37m";
    public const string ColorBold = "\x1b[1m";

    public static void SetAdminStatus(bool isAdmin)
    {
        _isAdmin = isAdmin;
    }

    public static void SetupConsole()
    {
        try
        {
            // Устанавливаем размер окна консоли
            var psi = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = "/c mode con: cols=120 lines=40",
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
        catch
        {
            // Игнорируем ошибки настройки консоли
        }
    }

    public static void ClearScreen()
    {
        try
        {
            Console.Clear();
        }
        catch
        {
            // Если Clear не работает, используем ANSI
            Console.Write("\x1b[2J\x1b[H");
        }
    }

    public static void PrintHeader()
    {
        ClearScreen();
        Console.WriteLine(ColorCyan + ColorBold);
        Console.WriteLine();
        Console.WriteLine("                  █▀▀ █ █ █▀ ▀█▀ █▀█ █▀");
        Console.WriteLine("                  █▄▄ █▄█ ▄█  █  █▄█ ▄█");
        Console.WriteLine();
        Console.WriteLine("                   sdelano s lubovyu");
        Console.WriteLine();
        Console.WriteLine(ColorReset);

        if (_isAdmin)
        {
            Console.WriteLine($"  {ColorGreen}[✓]{ColorReset} Статус: {ColorGreen}{ColorBold}Администратор{ColorReset}");
        }
        else
        {
            Console.WriteLine($"  {ColorRed}[✗]{ColorReset} Статус: {ColorRed}{ColorBold}Отсутствуют права администратора!{ColorReset}");
        }
        Console.WriteLine($"  {ColorBlue}[i]{ColorReset} Дата: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        Console.WriteLine();
    }

    public static void PrintMenu(string title, string[] options, bool showBack)
    {
        Console.WriteLine($"\n{ColorYellow}{ColorBold}{title}{ColorReset}\n");

        for (int i = 0; i < options.Length; i++)
        {
            Console.WriteLine($"  {ColorCyan}{ColorBold}[{i + 1}]{ColorReset} ➤ {options[i]}");
        }

        if (showBack)
        {
            Console.WriteLine($"\n  {ColorMagenta}{ColorBold}[0]{ColorReset} ← {ColorMagenta}Назад{ColorReset}");
        }
        else
        {
            Console.WriteLine($"\n  {ColorRed}{ColorBold}[0]{ColorReset} ✖ {ColorRed}Выход{ColorReset}");
        }
        Console.WriteLine();
    }

    public static int GetChoice(int maxOpt)
    {
        while (true)
        {
            Console.Write($"\n{ColorGreen}{ColorBold}►{ColorReset} Выберите опцию [0-{maxOpt}]: ");

            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
                return 0;

            if (int.TryParse(input.Trim(), out int choice) && choice >= 0 && choice <= maxOpt)
            {
                return choice;
            }

            Console.WriteLine($"\n{ColorRed}{ColorBold}⚠ Ошибка: Введите число от 0 до {maxOpt}{ColorReset}");
        }
    }

    public static void Log(string msg, bool ok)
    {
        // Логирование отключено (как в Go версии)
    }

    public static void Pause()
    {
        Console.WriteLine($"\n{ColorGreen}{ColorBold}►{ColorReset} Нажмите Enter для продолжения...");
        Console.ReadLine();
    }

    public static void DisplayFilesWithPagination(List<string> files, int itemsPerPage)
    {
        if (files.Count == 0)
        {
            Console.WriteLine($"\n{ColorYellow}  Нет файлов для отображения{ColorReset}");
            return;
        }

        int totalPages = (files.Count + itemsPerPage - 1) / itemsPerPage;
        int currentPage = 0;

        while (true)
        {
            ClearScreen();

            Console.WriteLine($"\n{ColorCyan}{ColorBold}═══ ПРОСМОТР ФАЙЛОВ (Страница {currentPage + 1} из {totalPages}) ═══{ColorReset}");
            Console.WriteLine($"{ColorYellow}Всего файлов: {files.Count}{ColorReset}\n");

            int start = currentPage * itemsPerPage;
            int end = Math.Min(start + itemsPerPage, files.Count);

            for (int i = start; i < end; i++)
            {
                Console.WriteLine($"  {ColorCyan}[{i + 1}]{ColorReset} {files[i]}");
            }

            Console.WriteLine($"\n{ColorCyan}{new string('─', 120)}{ColorReset}");
            Console.WriteLine($"\n{ColorYellow}{ColorBold}Навигация:{ColorReset}");

            if (currentPage > 0)
            {
                Console.WriteLine($"  {ColorGreen}[P]{ColorReset} - Предыдущая страница");
            }
            if (currentPage < totalPages - 1)
            {
                Console.WriteLine($"  {ColorGreen}[N]{ColorReset} - Следующая страница");
            }
            Console.WriteLine($"  {ColorRed}[0]{ColorReset} - Вернуться назад");

            Console.Write($"\n{ColorGreen}{ColorBold}►{ColorReset} Выберите действие: ");
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
                        Console.WriteLine($"{ColorYellow}⚠ Это последняя страница{ColorReset}");
                        Thread.Sleep(500);
                    }
                    break;
                case "p":
                    if (currentPage > 0)
                    {
                        currentPage--;
                    }
                    else
                    {
                        Console.WriteLine($"{ColorYellow}⚠ Это первая страница{ColorReset}");
                        Thread.Sleep(500);
                    }
                    break;
                case "0":
                case "":
                case null:
                    return;
                default:
                    Console.WriteLine($"{ColorRed}⚠ Неверная команда{ColorReset}");
                    Thread.Sleep(500);
                    break;
            }
        }
    }

    public static void PrintCleanupMessage()
    {
        Console.WriteLine();
        Console.WriteLine($"{ColorCyan}{ColorBold}╔═══════════════════════════════════════════════════════════╗{ColorReset}");
        Console.WriteLine($"{ColorCyan}{ColorBold}║{ColorReset}                                                           {ColorCyan}{ColorBold}║{ColorReset}");
        Console.WriteLine($"{ColorCyan}{ColorBold}║{ColorReset}    {ColorMagenta}{ColorBold}░█▀▀░█░█░█▀▀░▀█▀░█▀█░█▀▀░░░░░█▀▀░█░█░▀█▀░▀█▀{ColorReset}    {ColorCyan}{ColorBold}║{ColorReset}");
        Console.WriteLine($"{ColorCyan}{ColorBold}║{ColorReset}    {ColorMagenta}{ColorBold}░█░░░█░█░▀▀█░░█░░█░█░▀▀█░░░░░█▀▀░▄▀▄░░█░░░█░{ColorReset}    {ColorCyan}{ColorBold}║{ColorReset}");
        Console.WriteLine($"{ColorCyan}{ColorBold}║{ColorReset}    {ColorMagenta}{ColorBold}░▀▀▀░▀▀▀░▀▀▀░░▀░░▀▀▀░▀▀▀░▀░░░▀▀▀░▀░▀░▀▀▀░░▀░{ColorReset}    {ColorCyan}{ColorBold}║{ColorReset}");
        Console.WriteLine($"{ColorCyan}{ColorBold}║{ColorReset}                                                           {ColorCyan}{ColorBold}║{ColorReset}");
        Console.WriteLine($"{ColorCyan}{ColorBold}║{ColorReset}          {ColorYellow}{ColorBold}✨ Спасибо за использование! ✨{ColorReset}              {ColorCyan}{ColorBold}║{ColorReset}");
        Console.WriteLine($"{ColorCyan}{ColorBold}║{ColorReset}                                                           {ColorCyan}{ColorBold}║{ColorReset}");
        Console.WriteLine($"{ColorCyan}{ColorBold}║{ColorReset}    {ColorGreen}⚡ Ваша система проверена на безопасность ⚡{ColorReset}     {ColorCyan}{ColorBold}║{ColorReset}");
        Console.WriteLine($"{ColorCyan}{ColorBold}║{ColorReset}         🛡️  Будьте бдительны и осторожны! 🛡️          {ColorCyan}{ColorBold}║{ColorReset}");
        Console.WriteLine($"{ColorCyan}{ColorBold}║{ColorReset}                                                           {ColorCyan}{ColorBold}║{ColorReset}");
        Console.WriteLine($"{ColorCyan}{ColorBold}╚═══════════════════════════════════════════════════════════╝{ColorReset}");
        Console.WriteLine();
    }
}
