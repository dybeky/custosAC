using CustosAC.Constants;
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
            (path: AppConstants.WindowsPath, name: AppConstants.WindowsPath, maxDepth: AppConstants.WindowsScanDepth),
            (path: AppConstants.ProgramFilesX86, name: AppConstants.ProgramFilesX86, maxDepth: AppConstants.ProgramFilesScanDepth),
            (path: AppConstants.ProgramFiles, name: AppConstants.ProgramFiles, maxDepth: AppConstants.ProgramFilesScanDepth),
            (path: Path.Combine(userprofile, "Downloads"), name: "Downloads", maxDepth: AppConstants.UserFoldersScanDepth),
            (path: Path.Combine(userprofile, "OneDrive"), name: "OneDrive", maxDepth: AppConstants.UserFoldersScanDepth)
        };

        var extensions = AppConstants.ExecutableExtensions;

        ConsoleUI.Log("Начинается сканирование системных папок...", true);
        Console.WriteLine($"{ConsoleUI.Warning} {ConsoleUI.ColorYellow}Это может занять некоторое время...{ConsoleUI.ColorReset}\n");

        var allResults = new List<string>();
        bool isFirst = true;

        for (int i = 0; i < folders.Length; i++)
        {
            var folder = folders[i];

            if (!Directory.Exists(folder.path))
            {
                Console.WriteLine($"{ConsoleUI.ColorYellow}[ПРОПУСК]{ConsoleUI.ColorReset} {folder.name} - папка не существует\n");
                continue;
            }

            if (!isFirst)
            {
                Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.SeparatorLong}{ConsoleUI.ColorReset}\n");
            }
            isFirst = false;

            Console.WriteLine($"{ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}[СКАНИРОВАНИЕ {i + 1}/{folders.Length}]{ConsoleUI.ColorReset} {ConsoleUI.ColorCyan}{folder.name}{ConsoleUI.ColorReset}");
            Console.WriteLine($"{ConsoleUI.ColorBlue}Путь: {folder.path}{ConsoleUI.ColorReset}\n");

            var results = Common.ScanFolderOptimized(folder.path, extensions, folder.maxDepth);

            if (results.Count > 0)
            {
                Console.WriteLine($"{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}  Найдено подозрительных файлов: {results.Count}{ConsoleUI.ColorReset}\n");

                allResults.AddRange(results);

                foreach (var result in results)
                {
                    Console.WriteLine($"  {ConsoleUI.Arrow} {result}");
                }

                Console.WriteLine();
            }
            else
            {
                Console.WriteLine($"{ConsoleUI.ColorGreen}  Подозрительных файлов не найдено{ConsoleUI.ColorReset}\n");
            }
        }

        Console.WriteLine();
        Common.DisplayScanResults(allResults);
    }
}
