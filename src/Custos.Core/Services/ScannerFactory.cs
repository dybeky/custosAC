using Custos.Core.Abstractions;
using Custos.Core.Configuration;
using Custos.Core.Scanner;

namespace Custos.Core.Services;

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
    private readonly GamePathFinderService _gamePathFinder;
    private readonly LogService? _logService;

    public ScannerFactory(
        KeywordMatcherService keywordMatcher,
        IUIService uiService,
        RegistryService registryService,
        ScanSettings scanSettings,
        PathSettings pathSettings,
        RegistrySettings registrySettings,
        ExternalResourceSettings externalSettings,
        GamePathFinderService gamePathFinder,
        LogService? logService = null)
    {
        _keywordMatcher = keywordMatcher;
        _uiService = uiService;
        _registryService = registryService;
        _scanSettings = scanSettings;
        _pathSettings = pathSettings;
        _registrySettings = registrySettings;
        _externalSettings = externalSettings;
        _gamePathFinder = gamePathFinder;
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
        => new(_keywordMatcher, _uiService, _scanSettings, _logService);

    public RecentFileScannerAsync CreateRecentFileScanner()
        => new(_keywordMatcher, _uiService, _scanSettings, _scanSettings.RecentFilesDays);

    public BrowserHistoryScannerAsync CreateBrowserHistoryScanner()
        => new(_keywordMatcher, _uiService, _scanSettings, _externalSettings);

    public DnsCacheScannerAsync CreateDnsCacheScanner()
        => new(_keywordMatcher, _uiService, _scanSettings, _externalSettings);

    public GameFolderScannerAsync CreateGameFolderScanner()
        => new(_keywordMatcher, _uiService, _scanSettings, _pathSettings, _gamePathFinder);

    public AmcacheScannerAsync CreateAmcacheScanner()
        => new(_keywordMatcher, _uiService, _scanSettings, _pathSettings);

    public ShimCacheScannerAsync CreateShimCacheScanner()
        => new(_keywordMatcher, _uiService, _scanSettings);

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
        yield return CreateGameFolderScanner();
        yield return CreateAmcacheScanner();
        yield return CreateShimCacheScanner();
    }

    /// <summary>
    /// Gets the total number of available scanners
    /// </summary>
    public int GetScannerCount() => 12;
}
