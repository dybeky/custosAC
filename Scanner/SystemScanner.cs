using CustosAC.UI;

namespace CustosAC.Scanner;

public static class SystemScanner
{
    public static void ScanSystemFolders()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ СКАНИРОВАНИЕ СИСТЕМНЫХ ПАПОК ═══{ConsoleUI.ColorReset}\n");

        var userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var folders = new[]
        {
            (path: @"C:\Windows", name: @"C:\Windows", maxDepth: 2),
            (path: @"C:\Program Files (x86)", name: @"C:\Program Files (x86)", maxDepth: 3),
            (path: @"C:\Program Files", name: @"C:\Program Files", maxDepth: 3),
            (path: Path.Combine(userprofile, "Downloads"), name: "Downloads", maxDepth: 5),
            (path: Path.Combine(userprofile, "OneDrive"), name: "OneDrive", maxDepth: 5)
        };

        var extensions = new[] { ".exe", ".dll" };

        ConsoleUI.Log("Начинается сканирование системных папок...", true);
        Console.WriteLine($"{ConsoleUI.ColorYellow}⚠ Это может занять некоторое время...{ConsoleUI.ColorReset}\n");

        var allResults = new List<string>();
        int processedFolders = 0;

        for (int i = 0; i < folders.Length; i++)
        {
            var folder = folders[i];

            if (!Directory.Exists(folder.path))
            {
                Console.WriteLine($"{ConsoleUI.ColorYellow}[ПРОПУСК]{ConsoleUI.ColorReset} {folder.name} - папка не существует\n");
                continue;
            }

            if (processedFolders > 0)
            {
                Console.WriteLine($"\n{ConsoleUI.ColorCyan}{new string('─', 120)}{ConsoleUI.ColorReset}\n");
            }
            processedFolders++;

            Console.WriteLine($"{ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}[СКАНИРОВАНИЕ {i + 1}/{folders.Length}]{ConsoleUI.ColorReset} {ConsoleUI.ColorCyan}{folder.name}{ConsoleUI.ColorReset}");
            Console.WriteLine($"{ConsoleUI.ColorBlue}Путь: {folder.path}{ConsoleUI.ColorReset}\n");

            var results = Common.ScanFolderOptimized(folder.path, extensions, folder.maxDepth);

            if (results.Count > 0)
            {
                Console.WriteLine($"{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}  Найдено подозрительных файлов: {results.Count}{ConsoleUI.ColorReset}\n");

                allResults.AddRange(results);

                foreach (var result in results)
                {
                    Console.WriteLine($"  {ConsoleUI.ColorRed}►{ConsoleUI.ColorReset} {result}");
                }

                Console.WriteLine();
            }
            else
            {
                Console.WriteLine($"{ConsoleUI.ColorGreen}  Подозрительных файлов не найдено{ConsoleUI.ColorReset}\n");
            }
        }

        Console.WriteLine();
        if (allResults.Count > 0)
        {
            ConsoleUI.Log($"Всего найдено подозрительных файлов: {allResults.Count}", false);
            Console.WriteLine($"\n{ConsoleUI.ColorGreen}[V]{ConsoleUI.ColorReset} - Просмотреть все файлы постранично");
            Console.WriteLine($"{ConsoleUI.ColorCyan}[0]{ConsoleUI.ColorReset} - Продолжить");
            Console.Write($"\n{ConsoleUI.ColorGreen}{ConsoleUI.ColorBold}►{ConsoleUI.ColorReset} Выберите действие: ");

            var choice = Console.ReadLine()?.ToLower().Trim();

            if (choice == "v")
            {
                ConsoleUI.DisplayFilesWithPagination(allResults, 25);
            }
        }
        else
        {
            ConsoleUI.Log("Подозрительных файлов не найдено", true);
            ConsoleUI.Pause();
        }
    }
}
