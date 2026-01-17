using System.Text.Json;

namespace Custos.Core.Configuration;

/// <summary>
/// Root configuration class loaded from appsettings.json
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
    /// Load configuration from JSON file
    /// </summary>
    /// <param name="jsonPath">Path to appsettings.json</param>
    /// <returns>Configuration root</returns>
    public static ConfigRoot Load(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            return new ConfigRoot();
        }

        try
        {
            var json = File.ReadAllText(jsonPath);
            return JsonSerializer.Deserialize<ConfigRoot>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            }) ?? new ConfigRoot();
        }
        catch (JsonException)
        {
            return new ConfigRoot();
        }
        catch (IOException)
        {
            return new ConfigRoot();
        }
    }
}
