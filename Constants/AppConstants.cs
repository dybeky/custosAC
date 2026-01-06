namespace CustosAC.Constants;

/// <summary>
/// Централизованные константы приложения
/// </summary>
public static class AppConstants
{
    // ═══════════════════════════════════════════════════════════════
    // ПУТИ WINDOWS
    // ═══════════════════════════════════════════════════════════════

    public const string PrefetchPath = @"C:\Windows\Prefetch";
    public const string WindowsPath = @"C:\Windows";
    public const string ProgramFilesX86 = @"C:\Program Files (x86)";
    public const string ProgramFiles = @"C:\Program Files";

    // ═══════════════════════════════════════════════════════════════
    // ТАЙМАУТЫ (миллисекунды)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Таймаут по умолчанию для процессов</summary>
    public const int DefaultProcessTimeout = 10000;

    /// <summary>Таймаут для операций со службами</summary>
    public const int ServiceTimeout = 5000;

    /// <summary>Таймаут для PowerShell команд</summary>
    public const int PowerShellTimeout = 15000;

    /// <summary>Задержка при выходе из приложения</summary>
    public const int ExitDelay = 800;

    /// <summary>Задержка при завершении очистки</summary>
    public const int CleanupDelay = 1500;

    /// <summary>Задержка для UI обратной связи</summary>
    public const int UiDelay = 500;

    // ═══════════════════════════════════════════════════════════════
    // КОНСОЛЬ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Ширина консоли в символах</summary>
    public const int ConsoleWidth = 120;

    /// <summary>Высота консоли в строках</summary>
    public const int ConsoleHeight = 40;

    /// <summary>Отступ для заголовков меню</summary>
    public const int MenuPadding = 10;

    /// <summary>Количество элементов на странице при пагинации</summary>
    public const int ItemsPerPage = 25;

    // ═══════════════════════════════════════════════════════════════
    // СКАНИРОВАНИЕ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Максимальная глубина сканирования AppData</summary>
    public const int AppDataScanDepth = 10;

    /// <summary>Глубина сканирования системной папки Windows</summary>
    public const int WindowsScanDepth = 2;

    /// <summary>Глубина сканирования Program Files</summary>
    public const int ProgramFilesScanDepth = 3;

    /// <summary>Глубина сканирования пользовательских папок</summary>
    public const int UserFoldersScanDepth = 5;

    /// <summary>Расширения исполняемых файлов для поиска</summary>
    public static readonly string[] ExecutableExtensions = { ".exe", ".dll" };

    // ═══════════════════════════════════════════════════════════════
    // ВНЕШНИЕ РЕСУРСЫ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Сайты для проверки</summary>
    public static readonly (string url, string name)[] WebsitesToCheck =
    {
        ("https://oplata.info", "Oplata.info"),
        ("https://funpay.com", "FunPay.com")
    };

    /// <summary>Telegram боты для проверки</summary>
    public static readonly (string username, string name)[] TelegramBots =
    {
        ("@MelonySolutionBot", "Melony Solution Bot"),
        ("@UndeadSellerBot", "Undead Seller Bot")
    };

    // ═══════════════════════════════════════════════════════════════
    // СЕТЕВЫЕ СЛУЖБЫ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Службы для перезапуска при разблокировке сети</summary>
    public static readonly string[] NetworkServices = { "netprofm", "NlaSvc", "Dhcp", "Dnscache" };
}
