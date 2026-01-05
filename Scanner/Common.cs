using System.Diagnostics;
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

    public static bool OpenFolder(string path, string desc)
    {
        if (!Directory.Exists(path))
        {
            ConsoleUI.Log($"Папка не найдена: {path}", false);
            return false;
        }

        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "explorer",
                Arguments = path,
                UseShellExecute = true
            });

            if (process != null)
            {
                AdminHelper.TrackProcess(process);
                Task.Run(() =>
                {
                    try
                    {
                        process.WaitForExit();
                    }
                    catch (InvalidOperationException)
                    {
                        // Процесс уже завершился или недоступен
                    }
                    finally
                    {
                        AdminHelper.UntrackProcess(process);
                    }
                });
            }

            ConsoleUI.Log($"{desc}: {path}", true);
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
            // Используем UseShellExecute для безопасного открытия URL и команд
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = command,
                UseShellExecute = true
            });

            if (process != null)
            {
                AdminHelper.TrackProcess(process);
                Task.Run(() =>
                {
                    try
                    {
                        process.WaitForExit();
                    }
                    catch (InvalidOperationException)
                    {
                        // Процесс уже завершился или недоступен
                    }
                    finally
                    {
                        AdminHelper.UntrackProcess(process);
                    }
                });
            }

            ConsoleUI.Log(desc, true);
            return true;
        }
        catch (Exception ex)
        {
            ConsoleUI.Log($"Ошибка: {ex.Message}", false);
            return false;
        }
    }

    public static void CopyKeywordsToClipboard()
    {
        try
        {
            var keywordsStr = KeywordMatcher.GetKeywordsString();

            var psi = new ProcessStartInfo
            {
                FileName = "clip",
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.StandardInput.Write(keywordsStr);
                process.StandardInput.Close();
                process.WaitForExit();
            }

            ConsoleUI.Log("Ключевые слова скопированы в буфер обмена!", true);
            Console.WriteLine($"\n{ConsoleUI.ColorYellow}Теперь можно вставить (Ctrl+V) в Everything, LastActivityView и другие программы{ConsoleUI.ColorReset}");
        }
        catch (Exception ex)
        {
            ConsoleUI.Log($"Ошибка копирования: {ex.Message}", false);
        }
    }

    public static void CheckWebsites()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ПРОВЕРКА САЙТОВ ═══{ConsoleUI.ColorReset}\n");

        var websites = new[]
        {
            ("https://oplata.info", "Oplata.info"),
            ("https://funpay.com", "FunPay.com")
        };

        Console.WriteLine($"  {ConsoleUI.ColorBlue}[i]{ConsoleUI.ColorReset} Открываем сайты для проверки доступности...\n");

        foreach (var (url, name) in websites)
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

    public static bool OpenRegistry(string path)
    {
        try
        {
            // Копируем путь в буфер обмена
            var psi = new ProcessStartInfo
            {
                FileName = "clip",
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                if (process != null)
                {
                    process.StandardInput.Write(path);
                    process.StandardInput.Close();
                    process.WaitForExit();
                }
            }

            // Открываем regedit
            var regProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "regedit.exe",
                UseShellExecute = true
            });

            if (regProcess != null)
            {
                AdminHelper.TrackProcess(regProcess);
                Task.Run(() =>
                {
                    try
                    {
                        regProcess.WaitForExit();
                    }
                    catch (InvalidOperationException)
                    {
                        // Процесс уже завершился или недоступен
                    }
                    finally
                    {
                        AdminHelper.UntrackProcess(regProcess);
                    }
                });
            }

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

    public static void CheckTelegram()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ПРОВЕРКА TELEGRAM ═══{ConsoleUI.ColorReset}\n");

        var bots = new[]
        {
            ("@MelonySolutionBot", "Melony Solution Bot"),
            ("@UndeadSellerBot", "Undead Seller Bot")
        };

        Console.WriteLine($"  {ConsoleUI.ColorBlue}[i]{ConsoleUI.ColorReset} Открываем Telegram ботов для проверки...\n");

        foreach (var (username, name) in bots)
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

    public static async Task<List<string>> ScanFolderOptimizedAsync(string path, string[] extensions, int maxDepth, int currentDepth = 0)
    {
        return await Task.Run(() => ScanFolderOptimized(path, extensions, maxDepth, currentDepth));
    }
}
