using System.Diagnostics;
using CustosAC.Keywords;
using CustosAC.UI;

namespace CustosAC.Scanner;

public static class RegistryScanner
{
    public static void SearchRegistry()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ПОИСК В РЕЕСТРЕ ПО КЛЮЧЕВЫМ СЛОВАМ ═══{ConsoleUI.ColorReset}\n");

        var registryKeys = new[]
        {
            (path: @"HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache", name: "MuiCache"),
            (path: @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched", name: "AppSwitched"),
            (path: @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\ShowJumpView", name: "ShowJumpView")
        };

        ConsoleUI.Log("Начинается поиск в реестре...", true);
        Console.WriteLine($"{ConsoleUI.ColorYellow}⚠ Для поиска используется временный экспорт{ConsoleUI.ColorReset}\n");

        var allFindings = new List<string>();
        var tempDir = Path.Combine(Path.GetTempPath(), "custosAC_temp");

        try
        {
            Directory.CreateDirectory(tempDir);
        }
        catch
        {
            ConsoleUI.Log("Не удалось создать временную папку", false);
            ConsoleUI.Pause();
            return;
        }

        int processedKeys = 0;

        try
        {
            for (int i = 0; i < registryKeys.Length; i++)
            {
                var regKey = registryKeys[i];

                if (processedKeys > 0)
                {
                    Console.WriteLine($"{ConsoleUI.ColorCyan}{new string('─', 80)}{ConsoleUI.ColorReset}");
                }

                Console.WriteLine($"\n{ConsoleUI.ColorYellow}[СКАНИРОВАНИЕ {i + 1}/{registryKeys.Length}]{ConsoleUI.ColorReset} {regKey.name}");
                Console.WriteLine($"{ConsoleUI.ColorBlue}Путь: {regKey.path}{ConsoleUI.ColorReset}\n");

                var outputFile = Path.Combine(tempDir, regKey.name + ".reg");

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "reg",
                        Arguments = $"export \"{regKey.path}\" \"{outputFile}\" /y",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    };

                    using var process = Process.Start(psi);
                    process?.WaitForExit();

                    if (process?.ExitCode != 0 || !File.Exists(outputFile))
                    {
                        Console.WriteLine($"{ConsoleUI.ColorYellow}✗{ConsoleUI.ColorReset} Ключ не существует или недоступен: {regKey.name}\n");
                        continue;
                    }

                    processedKeys++;

                    var content = File.ReadAllText(outputFile);
                    var lines = content.Split('\n');

                    var findings = new List<string>();
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (string.IsNullOrEmpty(trimmedLine) ||
                            trimmedLine.StartsWith("Windows Registry") ||
                            trimmedLine.StartsWith("[HKEY"))
                        {
                            continue;
                        }

                        if (KeywordMatcher.ContainsKeyword(line))
                        {
                            findings.Add(trimmedLine);
                        }
                    }

                    if (findings.Count > 0)
                    {
                        Console.WriteLine($"{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}  Найдено записей с ключевыми словами: {findings.Count}{ConsoleUI.ColorReset}\n");

                        foreach (var finding in findings)
                        {
                            allFindings.Add($"[{regKey.name}] {finding}");
                            Console.WriteLine($"  {ConsoleUI.ColorRed}►{ConsoleUI.ColorReset} {finding}");
                        }

                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine($"{ConsoleUI.ColorGreen}  Подозрительных записей не найдено{ConsoleUI.ColorReset}\n");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ConsoleUI.ColorRed}✗{ConsoleUI.ColorReset} Ошибка сканирования {regKey.name}: {ex.Message}\n");
                }
            }
        }
        finally
        {
            // Удаляем временную папку
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Игнорируем ошибки удаления
            }
        }

        Console.WriteLine();
        if (allFindings.Count > 0)
        {
            ConsoleUI.Log($"Всего найдено подозрительных записей: {allFindings.Count}", false);
            Console.WriteLine($"\n{ConsoleUI.ColorGreen}[V]{ConsoleUI.ColorReset} - Просмотреть все записи постранично");
            Console.WriteLine($"{ConsoleUI.ColorCyan}[0]{ConsoleUI.ColorReset} - Продолжить");
            Console.Write($"\n{ConsoleUI.ColorGreen}{ConsoleUI.ColorBold}►{ConsoleUI.ColorReset} Выберите действие: ");

            var choice = Console.ReadLine()?.ToLower().Trim();

            if (choice == "v")
            {
                ConsoleUI.DisplayFilesWithPagination(allFindings, 25);
            }
        }
        else
        {
            ConsoleUI.Log("Подозрительных записей в реестре не найдено", true);
            ConsoleUI.Pause();
        }
    }
}
