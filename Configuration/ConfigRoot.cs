using System.Text.Json;

namespace CustosAC.Configuration;

/// <summary>
/// Корневой класс конфигурации, загружаемый из appsettings.json
/// </summary>
public class ConfigRoot
{
    public AppSettings App { get; set; } = new();
    public ScanSettings Scanning { get; set; } = new();
    public KeywordSettings Keywords { get; set; } = new();
    public PathSettings Paths { get; set; } = new();
    public RegistrySettings Registry { get; set; } = new();
    public ExternalResourceSettings ExternalResources { get; set; } = new();

    /// <summary>
    /// Загрузить конфигурацию из JSON файла
    /// </summary>
    public static ConfigRoot Load(string jsonPath)
    {
        try
        {
            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"Конфигурационный файл не найден: {jsonPath}");
                Console.WriteLine("Используются настройки по умолчанию.");
                return new ConfigRoot();
            }

            var json = File.ReadAllText(jsonPath);
            return JsonSerializer.Deserialize<ConfigRoot>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            }) ?? new ConfigRoot();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Ошибка парсинга конфигурации: {ex.Message}");
            Console.WriteLine("Используются настройки по умолчанию.");
            return new ConfigRoot();
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Ошибка чтения конфигурации: {ex.Message}");
            Console.WriteLine("Используются настройки по умолчанию.");
            return new ConfigRoot();
        }
    }
}
