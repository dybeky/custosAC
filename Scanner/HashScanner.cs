using System.Security.Cryptography;
using System.Text.Json;
using CustosAC.UI;

namespace CustosAC.Scanner;

/// <summary>
/// Сканер по хешам известных читов
/// </summary>
public static class HashScanner
{
    private static SignatureDatabase? _database;
    private static readonly object _lock = new();

    public class SignatureDatabase
    {
        public string Version { get; set; } = "";
        public string LastUpdated { get; set; } = "";
        public HashSection? Hashes { get; set; }
        public List<string>? SuspiciousImports { get; set; }
        public List<string>? SuspiciousSections { get; set; }
        public List<PackerSignature>? PackerSignatures { get; set; }
        public List<string>? SuspiciousDrivers { get; set; }
        public List<string>? Keywords { get; set; }
        public List<string>? WhitelistedPaths { get; set; }
        public List<string>? WhitelistedPublishers { get; set; }
    }

    public class HashSection
    {
        public List<HashEntry>? Sha256 { get; set; }
    }

    public class HashEntry
    {
        public string Hash { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class PackerSignature
    {
        public string Name { get; set; } = "";
        public List<string>? Signatures { get; set; }
        public string Description { get; set; } = "";
    }

    public class ScanResult
    {
        public string FilePath { get; set; } = "";
        public string Hash { get; set; } = "";
        public bool IsKnownMalicious { get; set; }
        public HashEntry? MatchedEntry { get; set; }
        public bool KeywordMatch { get; set; }
        public string? MatchedKeyword { get; set; }
    }

    /// <summary>
    /// Загрузка базы сигнатур
    /// </summary>
    public static bool LoadDatabase(string? customPath = null)
    {
        lock (_lock)
        {
            try
            {
                string dbPath = customPath ?? GetDefaultDatabasePath();

                if (!File.Exists(dbPath))
                {
                    ConsoleUI.Log($"- База сигнатур не найдена: {dbPath}", false);
                    return false;
                }

                string json = File.ReadAllText(dbPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                _database = JsonSerializer.Deserialize<SignatureDatabase>(json, options);

                if (_database != null)
                {
                    ConsoleUI.Log($"+ База сигнатур загружена (v{_database.Version})", true);
                    int hashCount = _database.Hashes?.Sha256?.Count ?? 0;
                    ConsoleUI.Log($"  Хешей: {hashCount}, Ключевых слов: {_database.Keywords?.Count ?? 0}", true);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ConsoleUI.Log($"- Ошибка загрузки базы: {ex.Message}", false);
                return false;
            }
        }
    }

    private static string GetDefaultDatabasePath()
    {
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(exeDir, "Data", "signatures.json");
    }

    /// <summary>
    /// Вычисление SHA256 хеша файла
    /// </summary>
    public static string ComputeFileHash(string filePath)
    {
        try
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Проверка хеша против базы
    /// </summary>
    public static HashEntry? CheckHash(string hash)
    {
        if (_database?.Hashes?.Sha256 == null || string.IsNullOrEmpty(hash))
            return null;

        return _database.Hashes.Sha256.FirstOrDefault(
            e => e.Hash.Equals(hash, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Проверка имени файла на ключевые слова
    /// </summary>
    public static (bool match, string? keyword) CheckKeywords(string fileName)
    {
        if (_database?.Keywords == null || string.IsNullOrEmpty(fileName))
            return (false, null);

        string lowerName = fileName.ToLowerInvariant();

        foreach (var keyword in _database.Keywords)
        {
            if (lowerName.Contains(keyword.ToLowerInvariant()))
                return (true, keyword);
        }

        return (false, null);
    }

    /// <summary>
    /// Полное сканирование файла
    /// </summary>
    public static ScanResult ScanFile(string filePath)
    {
        var result = new ScanResult { FilePath = filePath };

        if (!File.Exists(filePath))
            return result;

        // Вычисляем хеш
        result.Hash = ComputeFileHash(filePath);

        // Проверяем против базы хешей
        if (!string.IsNullOrEmpty(result.Hash))
        {
            var match = CheckHash(result.Hash);
            if (match != null)
            {
                result.IsKnownMalicious = true;
                result.MatchedEntry = match;
            }
        }

        // Проверяем имя файла
        string fileName = Path.GetFileName(filePath);
        var (keywordMatch, keyword) = CheckKeywords(fileName);
        result.KeywordMatch = keywordMatch;
        result.MatchedKeyword = keyword;

        return result;
    }

    /// <summary>
    /// Сканирование директории
    /// </summary>
    public static void ScanDirectory()
    {
        ConsoleUI.PrintHeader();
        Console.WriteLine($"\n{ConsoleUI.ColorCyan}{ConsoleUI.ColorBold}═══ СКАНИРОВАНИЕ ПО ХЕШАМ ═══{ConsoleUI.ColorReset}\n");

        // Загружаем базу если еще не загружена
        if (_database == null)
        {
            if (!LoadDatabase())
            {
                ConsoleUI.Log("- Не удалось загрузить базу сигнатур!", false);
                ConsoleUI.Pause();
                return;
            }
        }

        ConsoleUI.Log("+ Сканирование директорий...", true);

        var suspiciousFiles = new List<ScanResult>();
        int scannedCount = 0;

        // Директории для сканирования
        var directories = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads",
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };

        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
                continue;

            ConsoleUI.Log($"  Сканирование: {dir}", true);

            try
            {
                var files = Directory.EnumerateFiles(dir, "*.*", new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    MaxRecursionDepth = 5
                })
                .Where(f => f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

                foreach (var file in files)
                {
                    scannedCount++;

                    if (scannedCount % 100 == 0)
                        Console.Write($"\r  Проверено файлов: {scannedCount}");

                    var result = ScanFile(file);

                    if (result.IsKnownMalicious || result.KeywordMatch)
                    {
                        suspiciousFiles.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleUI.Log($"  - Ошибка: {ex.Message}", false);
            }
        }

        Console.WriteLine($"\r  Проверено файлов: {scannedCount}          ");

        // Вывод результатов
        Console.WriteLine();

        if (suspiciousFiles.Count > 0)
        {
            Console.WriteLine($"{ConsoleUI.ColorRed}{ConsoleUI.ColorBold}══ ОБНАРУЖЕНО ПОДОЗРИТЕЛЬНЫХ ФАЙЛОВ: {suspiciousFiles.Count} ══{ConsoleUI.ColorReset}\n");

            foreach (var file in suspiciousFiles)
            {
                Console.WriteLine($"{ConsoleUI.ColorOrange}► {file.FilePath}{ConsoleUI.ColorReset}");

                if (file.IsKnownMalicious && file.MatchedEntry != null)
                {
                    Console.WriteLine($"  {ConsoleUI.ColorRed}[ИЗВЕСТНЫЙ ЧИТ]{ConsoleUI.ColorReset} {file.MatchedEntry.Name}");
                    Console.WriteLine($"  Категория: {file.MatchedEntry.Category}, Критичность: {file.MatchedEntry.Severity}");
                }

                if (file.KeywordMatch)
                {
                    Console.WriteLine($"  {ConsoleUI.ColorYellow}[КЛЮЧЕВОЕ СЛОВО]{ConsoleUI.ColorReset} '{file.MatchedKeyword}'");
                }

                Console.WriteLine($"  SHA256: {file.Hash}");
                Console.WriteLine();
            }
        }
        else
        {
            ConsoleUI.Log("+ Подозрительных файлов не обнаружено", true);
        }

        ConsoleUI.Pause();
    }

    /// <summary>
    /// Получение списка подозрительных импортов
    /// </summary>
    public static List<string> GetSuspiciousImports()
    {
        return _database?.SuspiciousImports ?? new List<string>();
    }

    /// <summary>
    /// Получение списка подозрительных секций
    /// </summary>
    public static List<string> GetSuspiciousSections()
    {
        return _database?.SuspiciousSections ?? new List<string>();
    }

    /// <summary>
    /// Получение списка подозрительных драйверов
    /// </summary>
    public static List<string> GetSuspiciousDrivers()
    {
        return _database?.SuspiciousDrivers ?? new List<string>();
    }

    /// <summary>
    /// Проверка, является ли путь в белом списке
    /// </summary>
    public static bool IsWhitelistedPath(string path)
    {
        if (_database?.WhitelistedPaths == null)
            return false;

        return _database.WhitelistedPaths.Any(wp =>
            path.StartsWith(wp, StringComparison.OrdinalIgnoreCase));
    }
}
