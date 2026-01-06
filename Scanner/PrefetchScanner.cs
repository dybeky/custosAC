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
                    Console.WriteLine($"  {ConsoleUI.Arrow} {fileName}");
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
        Common.DisplayScanResults(suspiciousFiles, ".pf файлов");
    }
}
