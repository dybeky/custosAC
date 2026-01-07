namespace CustosAC.Abstractions;

/// <summary>
/// Интерфейс для консольного UI
/// </summary>
public interface IConsoleUI
{
    /// <summary>Установить статус администратора</summary>
    void SetAdminStatus(bool isAdmin);

    /// <summary>Настроить консоль (размер, ANSI)</summary>
    void SetupConsole();

    /// <summary>Очистить экран</summary>
    void ClearScreen();

    /// <summary>Вывести заголовок приложения</summary>
    void PrintHeader();

    /// <summary>Вывести меню</summary>
    void PrintMenu(string title, string[] options, bool showBack);

    /// <summary>Получить выбор пользователя</summary>
    int GetChoice(int maxOption);

    /// <summary>Вывести сообщение лога</summary>
    void Log(string message, bool success);

    /// <summary>Пауза с ожиданием нажатия клавиши</summary>
    void Pause();

    /// <summary>Отобразить файлы с пагинацией</summary>
    void DisplayFilesWithPagination(List<string> files, int itemsPerPage);

    /// <summary>Вывести сообщение об очистке</summary>
    void PrintCleanupMessage();

    /// <summary>Вывести прогресс операции</summary>
    void PrintProgress(string operation, int current, int total);

    /// <summary>Вывести пустую строку</summary>
    void PrintEmptyLine();

    /// <summary>Вывести разделитель</summary>
    void PrintSeparator();
}
