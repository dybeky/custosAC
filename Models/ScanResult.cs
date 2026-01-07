namespace CustosAC.Models;

/// <summary>
/// Результат сканирования
/// </summary>
public class ScanResult
{
    /// <summary>Имя сканера</summary>
    public string ScannerName { get; set; } = string.Empty;

    /// <summary>Успешно ли завершено сканирование</summary>
    public bool Success { get; set; }

    /// <summary>Найденные подозрительные элементы</summary>
    public List<string> Findings { get; set; } = new();

    /// <summary>Сообщение об ошибке (если есть)</summary>
    public string? Error { get; set; }

    /// <summary>Время начала сканирования</summary>
    public DateTime StartTime { get; set; }

    /// <summary>Время окончания сканирования</summary>
    public DateTime EndTime { get; set; }

    /// <summary>Длительность сканирования</summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>Количество найденных элементов</summary>
    public int Count => Findings.Count;

    /// <summary>Есть ли находки</summary>
    public bool HasFindings => Findings.Count > 0;
}

/// <summary>
/// Прогресс сканирования
/// </summary>
public class ScanProgress
{
    /// <summary>Имя сканера</summary>
    public string ScannerName { get; set; } = string.Empty;

    /// <summary>Текущий элемент</summary>
    public int Current { get; set; }

    /// <summary>Всего элементов</summary>
    public int Total { get; set; }

    /// <summary>Текущий путь</summary>
    public string? CurrentPath { get; set; }

    /// <summary>Процент выполнения</summary>
    public int Percentage => Total > 0 ? (Current * 100) / Total : 0;
}
