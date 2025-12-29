using CustosAC.Keywords;
using CustosAC.UI;

namespace CustosAC.Scanner;

public static class PrefetchScanner
{
    public static void ScanPrefetch()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ АВТОМАТИЧЕСКОЕ СКАНИРОВАНИЕ PREFETCH ═══{ConsoleUI.ColorReset}\n");

        var prefetchPath = @"C:\Windows\Prefetch";

        if (!Directory.Exists(prefetchPath))
        {
            ConsoleUI.Log("Папка Prefetch не найдена или недоступна", false);
            ConsoleUI.Pause();
            return;
        }

        ConsoleUI.Log("Начинается сканирование Prefetch...", true);
        Console.WriteLine();

        var suspiciousFiles = new List<string>();

        try
        {
            var files = Directory.GetFiles(prefetchPath);

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (!fileName.ToLower().EndsWith(".pf"))
                    continue;

                if (KeywordMatcher.ContainsKeyword(fileName))
                {
                    suspiciousFiles.Add(file);

                    var fileInfo = new FileInfo(file);
                    Console.WriteLine($"{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}►{ConsoleUI.ColorReset} {fileName}");
                    Console.WriteLine($"   Последнее изменение: {fileInfo.LastWriteTime:dd.MM.yyyy HH:mm:ss}\n");
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleUI.Log($"Ошибка чтения Prefetch: {ex.Message}", false);
            ConsoleUI.Pause();
            return;
        }

        Console.WriteLine();
        if (suspiciousFiles.Count > 0)
        {
            ConsoleUI.Log($"Найдено подозрительных .pf файлов: {suspiciousFiles.Count}", false);
            Console.WriteLine($"\n{ConsoleUI.ColorGreen}[V]{ConsoleUI.ColorReset} - Просмотреть все файлы постранично");
            Console.WriteLine($"{ConsoleUI.ColorCyan}[0]{ConsoleUI.ColorReset} - Продолжить");
            Console.Write($"\n{ConsoleUI.ColorGreen}{ConsoleUI.ColorBold}►{ConsoleUI.ColorReset} Выберите действие: ");

            var choice = Console.ReadLine()?.ToLower().Trim();

            if (choice == "v")
            {
                ConsoleUI.DisplayFilesWithPagination(suspiciousFiles, 25);
            }
        }
        else
        {
            ConsoleUI.Log("Подозрительных .pf файлов не найдено", true);
            ConsoleUI.Pause();
        }
    }
}
