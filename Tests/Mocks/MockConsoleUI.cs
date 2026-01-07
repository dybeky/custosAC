using CustosAC.Abstractions;

namespace CustosAC.Tests.Mocks;

/// <summary>
/// Mock реализация IConsoleUI для тестирования
/// </summary>
public class MockConsoleUI : IConsoleUI
{
    public List<string> LogMessages { get; } = new();
    public List<(string Message, bool Success)> LogEntries { get; } = new();
    public List<(string Operation, int Current, int Total)> ProgressReports { get; } = new();
    public bool IsAdmin { get; private set; }
    public int LastChoice { get; set; } = 0;
    public Queue<int> ChoiceQueue { get; } = new();

    public void SetAdminStatus(bool isAdmin)
    {
        IsAdmin = isAdmin;
    }

    public void SetupConsole()
    {
        // No-op в тестах
    }

    public void ClearScreen()
    {
        // No-op в тестах
    }

    public void PrintHeader()
    {
        // No-op в тестах
    }

    public void PrintMenu(string title, string[] options, bool showBack)
    {
        // No-op в тестах
    }

    public int GetChoice(int maxOption)
    {
        if (ChoiceQueue.Count > 0)
        {
            return ChoiceQueue.Dequeue();
        }
        return LastChoice;
    }

    public void Log(string message, bool success)
    {
        LogMessages.Add(message);
        LogEntries.Add((message, success));
    }

    public void Pause()
    {
        // No-op в тестах
    }

    public void PrintCleanupMessage()
    {
        // No-op в тестах
    }

    public void DisplayFilesWithPagination(List<string> files, int itemsPerPage)
    {
        // No-op в тестах
    }

    public void PrintProgress(string operation, int current, int total)
    {
        ProgressReports.Add((operation, current, total));
    }

    public void PrintEmptyLine()
    {
        // No-op в тестах
    }

    public void PrintSeparator()
    {
        // No-op в тестах
    }

    // Вспомогательные методы для тестов
    public void EnqueueChoices(params int[] choices)
    {
        foreach (var choice in choices)
        {
            ChoiceQueue.Enqueue(choice);
        }
    }

    public bool HasLogMessage(string substring) =>
        LogMessages.Any(m => m.Contains(substring, StringComparison.OrdinalIgnoreCase));

    public int SuccessLogCount => LogEntries.Count(e => e.Success);
    public int FailureLogCount => LogEntries.Count(e => !e.Success);
}
