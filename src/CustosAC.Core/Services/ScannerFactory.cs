using CustosAC.Core.Abstractions;
using CustosAC.Core.Configuration;
using CustosAC.Core.Scanner;

namespace CustosAC.Core.Services;

/// <summary>
/// Factory for creating scanners
/// </summary>
public class ScannerFactory
{
    private readonly KeywordMatcherService _keywordMatcher;
    private readonly IUIService _uiService;
    private readonly RegistryService _registryService;
    private readonly ScanSettings _scanSettings;
    private readonly PathSettings _pathSettings;
    private readonly RegistrySettings _registrySettings;
    private readonly ExternalResourceSettings _externalSettings;
    private readonly CheatHashDatabase _hashDatabase;
    private readonly LogService? _logService;

    public ScannerFactory(
        KeywordMatcherService keywordMatcher,
        IUIService uiService,
        RegistryService registryService,
        ScanSettings scanSettings,
        PathSettings pathSettings,
        RegistrySettings registrySettings,
        ExternalResourceSettings externalSettings,
        CheatHashDatabase? hashDatabase = null,
        LogService? logService = null)
    {
        _keywordMatcher = keywordMatcher;
        _uiService = uiService;
        _registryService = registryService;
        _scanSettings = scanSettings;
        _pathSettings = pathSettings;
        _registrySettings = registrySettings;
        _externalSettings = externalSettings;
        _hashDatabase = hashDatabase ?? new CheatHashDatabase();
        _logService = logService;
    }

    public AppDataScannerAsync CreateAppDataScanner()
        => new(_keywordMatcher, _uiService, _scanSettings);

    public SystemScannerAsync CreateSystemScanner()
        => new(_keywordMatcher, _uiService, _scanSettings, _pathSettings);

    public PrefetchScannerAsync CreatePrefetchScanner()
        => new(_keywordMatcher, _uiService, _scanSettings, _pathSettings);

    public RegistryScannerAsync CreateRegistryScanner()
        => new(_keywordMatcher, _uiService, _registryService, _scanSettings, _registrySettings);

    public SteamScannerAsync CreateSteamScanner()
        => new(_keywordMatcher, _uiService, _scanSettings, _pathSettings, _logService);

    public ProcessScannerAsync CreateProcessScanner()
        => new(_keywordMatcher, _uiService, _scanSettings, _hashDatabase, _logService);

    public RecentFileScannerAsync CreateRecentFileScanner()
        => new(_keywordMatcher, _uiService, _scanSettings, _hashDatabase, _scanSettings.RecentFilesDays);

    public BrowserHistoryScannerAsync CreateBrowserHistoryScanner()
        => new(_keywordMatcher, _uiService, _scanSettings, _externalSettings);

    public DnsCacheScannerAsync CreateDnsCacheScanner()
        => new(_keywordMatcher, _uiService, _scanSettings, _externalSettings);

    public IEnumerable<BaseScannerAsync> CreateAllScanners()
    {
        yield return CreateAppDataScanner();
        yield return CreateSystemScanner();
        yield return CreatePrefetchScanner();
        yield return CreateRegistryScanner();
        yield return CreateSteamScanner();
        yield return CreateProcessScanner();
        yield return CreateRecentFileScanner();
        yield return CreateBrowserHistoryScanner();
        yield return CreateDnsCacheScanner();
    }

    public CheatHashDatabase GetHashDatabase() => _hashDatabase;

    /// <summary>
    /// Gets the total number of available scanners
    /// </summary>
    public int GetScannerCount() => 9; // AppData, System, Prefetch, Registry, Steam, Process, RecentFile, BrowserHistory, DnsCache
}
