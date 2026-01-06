using CustosAC.UI;

namespace CustosAC.Scanner;

public static class AppDataScanner
{
    public static void ScanAppData()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ АВТОМАТИЧЕСКОЕ СКАНИРОВАНИЕ APPDATA ═══{ConsoleUI.ColorReset}\n");

        var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var localappdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var locallow = Path.Combine(userprofile, "AppData", "LocalLow");

        var folders = new[]
        {
            (path: appdata, name: "AppData\\Roaming"),
            (path: localappdata, name: "AppData\\Local"),
            (path: locallow, name: "AppData\\LocalLow")
        };

        var extensions = new[] { ".exe", ".dll" };

        ConsoleUI.Log("Начинается сканирование AppData...", true);
        Console.WriteLine();

        var allResults = new List<string>();

        for (int i = 0; i < folders.Length; i++)
        {
            var folder = folders[i];

            if (i > 0)
            {
                Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.SeparatorLong}{ConsoleUI.ColorReset}\n");
            }

            Console.WriteLine($"{ConsoleUI.ColorYellow}{ConsoleUI.ColorBold}[СКАНИРОВАНИЕ {i + 1}/{folders.Length}]{ConsoleUI.ColorReset} {ConsoleUI.ColorCyan}{folder.name}{ConsoleUI.ColorReset}");
            Console.WriteLine($"{ConsoleUI.ColorBlue}Путь: {folder.path}{ConsoleUI.ColorReset}\n");

            var results = Common.ScanFolderOptimized(folder.path, extensions, 10);

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
