using CustosAC.UI;

namespace CustosAC.Scanner;

public static class SteamScanner
{
    public static void ParseSteamAccountsFromPath(string vdfPath)
    {
        try
        {
            var content = File.ReadAllText(vdfPath);
            var lines = content.Split('\n');

            var accounts = new List<string>();
            string? currentSteamId = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("\"7656") && trimmedLine.Contains("\""))
                {
                    var parts = trimmedLine.Split('"');
                    if (parts.Length >= 2)
                    {
                        currentSteamId = parts[1];
                    }
                }

                if (trimmedLine.Contains("\"AccountName\"") && currentSteamId != null)
                {
                    var parts = trimmedLine.Split('"');
                    if (parts.Length >= 4)
                    {
                        var accountName = parts[3];
                        var displayStr = $"SteamID: {currentSteamId} | Имя: {accountName}";
                        Console.WriteLine($"{ConsoleUI.ColorCyan}►{ConsoleUI.ColorReset} {displayStr}");
                        accounts.Add(displayStr);
                        currentSteamId = null;
                    }
                }
            }

            Console.WriteLine();
            if (accounts.Count > 0)
            {
                ConsoleUI.Log($"Найдено аккаунтов Steam: {accounts.Count}", true);
            }
            else
            {
                ConsoleUI.Log("Аккаунты не найдены", false);
            }
        }
        catch (Exception ex)
        {
            ConsoleUI.Log($"Ошибка чтения файла: {ex.Message}", false);
        }
    }

    public static void ParseSteamAccounts()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ ПАРСИНГ STEAM АККАУНТОВ ═══{ConsoleUI.ColorReset}\n");

        var possiblePaths = new List<string>
        {
            @"C:\Program Files (x86)\Steam\config\loginusers.vdf",
            @"C:\Program Files\Steam\config\loginusers.vdf"
        };

        var drives = new[] { "D:", "E:", "F:" };
        foreach (var drive in drives)
        {
            possiblePaths.Add(Path.Combine(drive, "Steam", "config", "loginusers.vdf"));
            possiblePaths.Add(Path.Combine(drive, "Program Files (x86)", "Steam", "config", "loginusers.vdf"));
            possiblePaths.Add(Path.Combine(drive, "Program Files", "Steam", "config", "loginusers.vdf"));
        }

        string? vdfPath = null;
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                vdfPath = path;
                break;
            }
        }

        if (vdfPath == null)
        {
            ConsoleUI.Log("Файл loginusers.vdf не найден", false);
            ConsoleUI.Pause();
            return;
        }

        ConsoleUI.Log($"Найден файл: {vdfPath}", true);
        Console.WriteLine();

        ParseSteamAccountsFromPath(vdfPath);

        ConsoleUI.Pause();
    }
}
