namespace CustosAC.Constants;

/// <summary>
/// Централизованные ANSI escape-коды для консольного вывода
/// </summary>
public static class AnsiColors
{
    // Базовые цвета
    public const string Reset = "\x1b[0m";
    public const string Red = "\x1b[31m";
    public const string Green = "\x1b[32m";
    public const string Yellow = "\x1b[33m";
    public const string Blue = "\x1b[34m";
    public const string Magenta = "\x1b[35m";
    public const string Cyan = "\x1b[36m";
    public const string White = "\x1b[37m";
    public const string Orange = "\x1b[38;5;208m";

    // Стили
    public const string Bold = "\x1b[1m";
    public const string Dim = "\x1b[2m";

    // Методы форматирования
    public static string Success(string text) => $"{Green}{text}{Reset}";
    public static string Error(string text) => $"{Red}{text}{Reset}";
    public static string Warning(string text) => $"{Yellow}{text}{Reset}";
    public static string Info(string text) => $"{Blue}{text}{Reset}";
    public static string Highlight(string text) => $"{Cyan}{text}{Reset}";
    public static string BoldText(string text) => $"{Bold}{text}{Reset}";

    // Комбинированные стили
    public static string BoldCyan(string text) => $"{Cyan}{Bold}{text}{Reset}";
    public static string BoldYellow(string text) => $"{Yellow}{Bold}{text}{Reset}";
    public static string BoldGreen(string text) => $"{Green}{Bold}{text}{Reset}";
    public static string BoldRed(string text) => $"{Red}{Bold}{text}{Reset}";
    public static string BoldOrange(string text) => $"{Orange}{Bold}{text}{Reset}";

    // Префиксы для логов
    public static string SuccessPrefix => $"{Green}[+]{Reset}";
    public static string ErrorPrefix => $"{Red}[-]{Reset}";
    public static string InfoPrefix => $"{Blue}[i]{Reset}";
    public static string WarningPrefix => $"{Yellow}[!]{Reset}";
    public static string ArrowPrefix => $"{Cyan}[>]{Reset}";
    public static string ScanPrefix => $"{Magenta}[*]{Reset}";

    // Разделители
    public const string SeparatorLong = "────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────";
    public const string SeparatorMedium = "────────────────────────────────────────────────────────────────────────────────";
    public const string SeparatorShort = "─────────────────────────────────────────";
}
