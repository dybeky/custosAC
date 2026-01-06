using System.Diagnostics;
using CustosAC.Constants;
using CustosAC.Keywords;
using CustosAC.UI;

namespace CustosAC.Scanner;

public static class RegistryScanner
{
    public static void SearchRegistry()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ПОИСК В РЕЕСТРЕ ПО КЛЮЧЕВЫМ СЛОВАМ ═══{ConsoleUI.ColorReset}\n");

        ConsoleUI.Log("Начинается поиск в реестре...", true);
        Console.WriteLine($"{ConsoleUI.Warning} {ConsoleUI.ColorYellow}Для поиска используется временный экспорт{ConsoleUI.ColorReset}\n");

        var allFindings = new List<string>();
        var tempDir = Path.Combine(Path.GetTempPath(), "custosAC_temp");

        try
        {
            Directory.CreateDirectory(tempDir);
        }
        catch (Exception ex)
        {
            ConsoleUI.Log($"Не удалось создать временную папку: {ex.Message}", false);
            ConsoleUI.Pause();
            return;
        }

        try
        {
            for (int i = 0; i < RegistryConstants.ScanKeys.Length; i++)
            {
                var regKey = RegistryConstants.ScanKeys[i];

                if (i > 0)
                {
                    Console.WriteLine($"{ConsoleUI.ColorCyan}{ConsoleUI.SeparatorMedium}{ConsoleUI.ColorReset}");
                }

                Console.WriteLine($"\n{ConsoleUI.ColorYellow}[СКАНИРОВАНИЕ {i + 1}/{RegistryConstants.ScanKeys.Length}]{ConsoleUI.ColorReset} {regKey.name}");
                Console.WriteLine($"{ConsoleUI.ColorBlue}Путь: {regKey.path}{ConsoleUI.ColorReset}\n");

                var outputFile = Path.Combine(tempDir, regKey.name + ".reg");

                try
                {
                    // Используем ArgumentList для безопасной передачи аргументов
                    var psi = new ProcessStartInfo
                    {
                        FileName = "reg",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    };
                    psi.ArgumentList.Add("export");
                    psi.ArgumentList.Add(regKey.path);
                    psi.ArgumentList.Add(outputFile);
                    psi.ArgumentList.Add("/y");

                    using var process = Process.Start(psi);
                    process?.WaitForExit();

                    if (process?.ExitCode != 0 || !File.Exists(outputFile))
                    {
                        Console.WriteLine($"{ConsoleUI.Warning} {ConsoleUI.ColorYellow}Ключ не существует или недоступен: {regKey.name}{ConsoleUI.ColorReset}\n");
                        continue;
                    }

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
                            Console.WriteLine($"  {ConsoleUI.Arrow} {finding}");
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
                    Console.WriteLine($"{ConsoleUI.Error} {ConsoleUI.ColorRed}Ошибка сканирования {regKey.name}: {ex.Message}{ConsoleUI.ColorReset}\n");
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
            catch (Exception ex)
            {
                // Логируем ошибку удаления временной папки
                ConsoleUI.Log($"Не удалось удалить временную папку: {ex.Message}", false);
            }
        }

        Console.WriteLine();
        Common.DisplayScanResults(allFindings, "записей");
    }
}
