using System.Diagnostics;
using CustosAC.Constants;
using CustosAC.Keywords;
using CustosAC.UI;
using CustosAC.WinAPI;

namespace CustosAC.Scanner;

public static class Common
{
    private static readonly HashSet<string> ExcludeDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "windows.old",
        "$recycle.bin",
        "system volume information",
        "recovery",
        "perflogs",
        "windowsapps",
        "winsxs",
        ".git",
        "node_modules"
    };

    // ═══════════════════════════════════════════════════════════════
    // РАБОТА С ПАПКАМИ И КОМАНДАМИ
    // ═══════════════════════════════════════════════════════════════

    public static bool OpenFolder(string path, string desc)
    {
        if (!Directory.Exists(path))
        {
            ConsoleUI.Log($"Папка не найдена: {path}", false);
            return false;
        }

        try
        {
            StartTrackedProcess(new ProcessStartInfo
            {
                FileName = "explorer",
                Arguments = path,
                UseShellExecute = true
            }, $"{desc}: {path}");
            return true;
        }
        catch (Exception ex)
        {
            ConsoleUI.Log($"Ошибка: {ex.Message}", false);
            return false;
        }
    }

    public static bool RunCommand(string command, string desc)
    {
        try
        {
            StartTrackedProcess(new ProcessStartInfo
            {
                FileName = command,
                UseShellExecute = true
            }, desc);
            return true;
        }
        catch (Exception ex)
        {
            ConsoleUI.Log($"Ошибка: {ex.Message}", false);
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // КОПИРОВАНИЕ В БУФЕР
    // ═══════════════════════════════════════════════════════════════

    public static void CopyKeywordsToClipboard()
    {
        try
        {
            CopyToClipboard(KeywordMatcher.GetKeywordsString());
            ConsoleUI.Log("Ключевые слова скопированы в буфер обмена!", true);
            Console.WriteLine($"\n{ConsoleUI.ColorYellow}Теперь можно вставить (Ctrl+V) в Everything, LastActivityView и другие программы{ConsoleUI.ColorReset}");
        }
        catch (Exception ex)
        {
            ConsoleUI.Log($"Ошибка копирования: {ex.Message}", false);
        }
    }

    /// <summary>Копирует текст в буфер обмена</summary>
    public static void CopyToClipboard(string text)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "clip",
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process is null) return;

        process.StandardInput.Write(text);
        process.StandardInput.Close();
        process.WaitForExit();
    }

    // ═══════════════════════════════════════════════════════════════
    // ПРОВЕРКА САЙТОВ
    // ═══════════════════════════════════════════════════════════════

    public static void CheckWebsites()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ПРОВЕРКА САЙТОВ ═══{ConsoleUI.ColorReset}\n");

        Console.WriteLine($"  {ConsoleUI.ColorBlue}[i]{ConsoleUI.ColorReset} Открываем сайты для проверки доступности...\n");

        foreach (var (url, name) in AppConstants.WebsitesToCheck)
        {
            if (RunCommand(url, name))
            {
                ConsoleUI.Log($"+ Открыт: {name}", true);
            }
            else
            {
                ConsoleUI.Log($"- Ошибка открытия: {name}", false);
            }
        }

        Console.WriteLine($"\n{ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}ЧТО ПРОВЕРИТЬ:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  {ConsoleUI.Arrow} Доступность сайтов (открываются ли страницы)");
        Console.WriteLine($"  {ConsoleUI.Arrow} Нет ли редиректов на подозрительные домены");
        Console.WriteLine($"  {ConsoleUI.Arrow} Корректность отображения сайтов");
        Console.WriteLine($"  {ConsoleUI.Arrow} Нет ли предупреждений браузера");

        ConsoleUI.Pause();
    }

    // ═══════════════════════════════════════════════════════════════
    // РАБОТА С РЕЕСТРОМ
    // ═══════════════════════════════════════════════════════════════

    public static bool OpenRegistry(string path)
    {
        try
        {
            CopyToClipboard(path);
            StartTrackedProcess(new ProcessStartInfo
            {
                FileName = "regedit.exe",
                UseShellExecute = true
            });
            ConsoleUI.Log($"Путь скопирован: {path}", true);
            Console.WriteLine($"{ConsoleUI.ColorYellow}Вставьте путь в regedit (Ctrl+V){ConsoleUI.ColorReset}");
            return true;
        }
        catch (Exception ex)
        {
            ConsoleUI.Log($"Ошибка: {ex.Message}", false);
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // ПРОВЕРКА TELEGRAM
    // ═══════════════════════════════════════════════════════════════

    public static void CheckTelegram()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ПРОВЕРКА TELEGRAM ═══{ConsoleUI.ColorReset}\n");

        Console.WriteLine($"  {ConsoleUI.ColorBlue}[i]{ConsoleUI.ColorReset} Открываем Telegram ботов для проверки...\n");

        foreach (var (username, name) in AppConstants.TelegramBots)
        {
            var telegramUrl = $"tg://resolve?domain={username.TrimStart('@')}";
            if (RunCommand(telegramUrl, name))
            {
                ConsoleUI.Log($"+ Открыт: {name} ({username})", true);
            }
            else
            {
                ConsoleUI.Log($"- Ошибка открытия: {name}", false);
            }
        }

        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.SeparatorShort}{ConsoleUI.ColorReset}");

        Console.WriteLine($"\n  {ConsoleUI.ColorBlue}[i]{ConsoleUI.ColorReset} Поиск папки загрузок Telegram...\n");

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var possiblePaths = new List<string>
        {
            Path.Combine(userProfile, "Downloads", "Telegram Desktop"),
            Path.Combine(userProfile, "Downloads"),
            Path.Combine(userProfile, "Documents", "Telegram Desktop"),
            Path.Combine(userProfile, "OneDrive", "Downloads", "Telegram Desktop")
        };

        var telegramDataPath = Path.Combine(appData, "Telegram Desktop");
        if (Directory.Exists(telegramDataPath))
        {
            ConsoleUI.Log($"+ Найдена папка данных Telegram: {telegramDataPath}", true);
            possiblePaths.Insert(0, Path.Combine(telegramDataPath, "tdata", "user_data"));
        }

        bool foundDownloads = false;
        foreach (var downloadPath in possiblePaths)
        {
            if (Directory.Exists(downloadPath))
            {
                foundDownloads = true;
                ConsoleUI.Log($"+ Найдена папка загрузок: {downloadPath}", true);
                OpenFolder(downloadPath, "Папка загрузок Telegram");
                break;
            }
        }

        if (!foundDownloads)
        {
            ConsoleUI.Log("- Папка загрузок Telegram не найдена", false);
            Console.WriteLine($"\n{ConsoleUI.Warning} {ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}Возможные причины:{ConsoleUI.ColorReset}");
            Console.WriteLine($"  {ConsoleUI.Arrow} Telegram не установлен");
            Console.WriteLine($"  {ConsoleUI.Arrow} Папка загрузок находится в другом месте");
            Console.WriteLine($"  {ConsoleUI.Arrow} Файлы не загружались через Telegram");
        }

        Console.WriteLine($"\n{ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}ЧТО ПРОВЕРИТЬ В TELEGRAM:{ConsoleUI.ColorReset}");
        Console.WriteLine($"  {ConsoleUI.Arrow} Историю переписки с ботами @MelonySolutionBot и @UndeadSellerBot");
        Console.WriteLine($"  {ConsoleUI.Arrow} Загруженные файлы (.exe, .dll, .bat, .zip)");
        Console.WriteLine($"  {ConsoleUI.Arrow} Подозрительные архивы и установщики");
        Console.WriteLine($"  {ConsoleUI.Arrow} Переданные платежи или транзакции");

        ConsoleUI.Pause();
    }

    // ═══════════════════════════════════════════════════════════════
    // СКАНИРОВАНИЕ ПАПОК
    // ═══════════════════════════════════════════════════════════════

    public static List<string> ScanFolderOptimized(string path, string[] extensions, int maxDepth, int currentDepth = 0)
    {
        var results = new List<string>();

        if (currentDepth > maxDepth)
            return results;

        try
        {
            var entries = Directory.GetFileSystemEntries(path);

            foreach (var entry in entries)
            {
                try
                {
                    var name = Path.GetFileName(entry);
                    var nameLower = name.ToLower();

                    if (Directory.Exists(entry))
                    {
                        if (ExcludeDirs.Contains(nameLower))
                            continue;

                        if (KeywordMatcher.ContainsKeyword(name))
                        {
                            results.Add(entry);
                        }

                        // Рекурсивное сканирование поддиректорий
                        results.AddRange(ScanFolderOptimized(entry, extensions, maxDepth, currentDepth + 1));
                    }
                    else if (File.Exists(entry))
                    {
                        if (KeywordMatcher.ContainsKeyword(name))
                        {
                            if (extensions.Length > 0)
                            {
                                var ext = Path.GetExtension(entry).ToLower();
                                if (extensions.Contains(ext))
                                {
                                    results.Add(entry);
                                }
                            }
                            else
                            {
                                results.Add(entry);
                            }
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Ожидаемо: нет доступа к файлу/папке
                }
                catch (IOException)
                {
                    // Ожидаемо: файл занят или недоступен
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Ожидаемо: нет доступа к директории
        }
        catch (IOException)
        {
            // Ожидаемо: директория недоступна
        }

        return results;
    }

    // ═══════════════════════════════════════════════════════════════
    // ОТСЛЕЖИВАНИЕ ПРОЦЕССОВ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Запускает процесс с отслеживанием для автоматического закрытия при выходе</summary>
    private static void StartTrackedProcess(ProcessStartInfo psi, string? logMessage = null)
    {
        var process = Process.Start(psi);
        if (process is null)
        {
            if (logMessage != null)
                ConsoleUI.Log($"Не удалось запустить: {logMessage}", false);
            return;
        }

        AdminHelper.TrackProcess(process);
        _ = Task.Run(() =>
        {
            try { process.WaitForExit(); }
            catch (InvalidOperationException)
            {
                // Процесс уже завершён
            }
            finally { AdminHelper.UntrackProcess(process); }
        });

        if (logMessage != null)
            ConsoleUI.Log(logMessage, true);
    }

    // ═══════════════════════════════════════════════════════════════
    // ОТОБРАЖЕНИЕ РЕЗУЛЬТАТОВ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Отображает результаты сканирования с пагинацией</summary>
    public static void DisplayScanResults(List<string> results, string itemName = "файлов")
    {
        if (results.Count > 0)
        {
            ConsoleUI.Log($"Всего найдено подозрительных {itemName}: {results.Count}", false);
            Console.WriteLine($"\n{ConsoleUI.ColorGreen}[V]{ConsoleUI.ColorReset} - Просмотреть все постранично");
            Console.WriteLine($"{ConsoleUI.ColorCyan}[0]{ConsoleUI.ColorReset} - Продолжить");
            Console.Write($"\n{ConsoleUI.ColorGreen}{ConsoleUI.ColorBold}[>]{ConsoleUI.ColorReset} Выберите действие: ");

            var choice = Console.ReadLine()?.ToLower().Trim();
            if (choice == "v")
            {
                ConsoleUI.DisplayFilesWithPagination(results, AppConstants.ItemsPerPage);
            }
        }
        else
        {
            ConsoleUI.Log($"Подозрительных {itemName} не найдено", true);
            ConsoleUI.Pause();
        }
    }
}
