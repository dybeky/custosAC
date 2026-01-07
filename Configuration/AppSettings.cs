namespace CustosAC.Configuration;

/// <summary>
/// Основные настройки приложения
/// </summary>
public class AppSettings
{
    public TimeoutSettings Timeouts { get; set; } = new();
    public ConsoleSettings Console { get; set; } = new();
}

/// <summary>
/// Настройки таймаутов
/// </summary>
public class TimeoutSettings
{
    public int DefaultProcessTimeoutMs { get; set; } = 10000;
    public int ServiceTimeoutMs { get; set; } = 5000;
    public int PowerShellTimeoutMs { get; set; } = 15000;
    public int ExitDelayMs { get; set; } = 800;
    public int CleanupDelayMs { get; set; } = 1500;
    public int UiDelayMs { get; set; } = 500;
}

/// <summary>
/// Настройки консоли
/// </summary>
public class ConsoleSettings
{
    public int Width { get; set; } = 120;
    public int Height { get; set; } = 40;
    public int MenuPadding { get; set; } = 10;
    public int ItemsPerPage { get; set; } = 25;
}
