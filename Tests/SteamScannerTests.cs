using CustosAC.Abstractions;
using CustosAC.Configuration;
using CustosAC.Scanner;
using CustosAC.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CustosAC.Tests;

public class SteamScannerTests
{
    private readonly MockFileSystemService _fileSystem;
    private readonly MockConsoleUI _consoleUI;
    private readonly Mock<IKeywordMatcher> _keywordMatcher;
    private readonly ScanSettings _scanSettings;
    private readonly PathSettings _pathSettings;

    private const string SteamConfigContent = @"
""users""
{
    ""76561198012345678""
    {
        ""AccountName""		""player1""
        ""PersonaName""		""Player One""
        ""RememberPassword""		""1""
        ""Timestamp""		""1609459200""
    }
    ""76561198087654321""
    {
        ""AccountName""		""player2""
        ""PersonaName""		""Player Two""
        ""RememberPassword""		""0""
        ""Timestamp""		""1609545600""
    }
}";

    public SteamScannerTests()
    {
        _fileSystem = new MockFileSystemService();
        _consoleUI = new MockConsoleUI();
        _keywordMatcher = new Mock<IKeywordMatcher>();

        _scanSettings = new ScanSettings
        {
            ParallelScanEnabled = false,
            MaxDegreeOfParallelism = 1
        };

        _pathSettings = new PathSettings
        {
            Windows = new WindowsPathSettings
            {
                ProgramFilesX86 = @"C:\Program Files (x86)",
                ProgramFiles = @"C:\Program Files"
            },
            Steam = new SteamPathSettings
            {
                LoginUsersRelativePath = @"Steam\config\loginusers.vdf",
                AdditionalDrives = new[] { @"D:\", @"E:\" }
            }
        };
    }

    private SteamScannerAsync CreateScanner()
    {
        return new SteamScannerAsync(
            _fileSystem,
            _keywordMatcher.Object,
            _consoleUI,
            NullLogger<SteamScannerAsync>.Instance,
            Options.Create(_scanSettings),
            Options.Create(_pathSettings));
    }

    [Fact]
    public async Task ScanAsync_NoSteamInstalled_ReturnsNoFindings()
    {
        // Arrange
        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanAsync();

        // Assert
        Assert.False(result.HasFindings);
    }

    [Fact]
    public async Task ScanAsync_FindsSteamAccounts_ReturnsAccountNames()
    {
        // Arrange
        var configPath = @"C:\Program Files (x86)\Steam\config\loginusers.vdf";

        _fileSystem.AddFile(configPath, SteamConfigContent);

        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(result.HasFindings);
        Assert.Equal(2, result.Count);
        Assert.Contains(result.Findings, f => f.Contains("player1"));
        Assert.Contains(result.Findings, f => f.Contains("player2"));
    }

    [Fact]
    public async Task ScanAsync_FindsSteamOnAlternativeDrive()
    {
        // Arrange
        var configPath = @"D:\Steam\config\loginusers.vdf";

        _fileSystem.AddFile(configPath, SteamConfigContent);

        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(result.HasFindings);
    }

    [Fact]
    public async Task ScanAsync_EmptyConfigFile_ReturnsNoFindings()
    {
        // Arrange
        var configPath = @"C:\Program Files (x86)\Steam\config\loginusers.vdf";

        _fileSystem.AddFile(configPath, "");

        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanAsync();

        // Assert
        Assert.True(result.Success);
        Assert.False(result.HasFindings);
    }

    [Fact]
    public async Task ScanAsync_InvalidConfigFormat_HandlesGracefully()
    {
        // Arrange
        var configPath = @"C:\Program Files (x86)\Steam\config\loginusers.vdf";

        _fileSystem.AddFile(configPath, "invalid content { not vdf format }}}");

        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanAsync();

        // Assert
        Assert.True(result.Success);
        // Должен gracefully обработать ошибку парсинга
    }

    [Fact]
    public void Scanner_HasCorrectNameAndDescription()
    {
        // Arrange
        var scanner = CreateScanner();

        // Assert
        Assert.Equal("Steam Scanner", scanner.Name);
        Assert.NotEmpty(scanner.Description);
        Assert.Contains("Steam", scanner.Description);
    }

    [Fact]
    public async Task ScanAsync_SupportsCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var scanner = CreateScanner();

        // Act & Assert
        // Должен корректно обработать отмену
        var result = await scanner.ScanAsync(cancellationToken: cts.Token);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ScanAsync_ParsesAccountDetails()
    {
        // Arrange
        var configPath = @"C:\Program Files (x86)\Steam\config\loginusers.vdf";

        _fileSystem.AddFile(configPath, SteamConfigContent);

        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanAsync();

        // Assert
        Assert.True(result.HasFindings);
        Assert.Contains(result.Findings, f => f.Contains("76561198012345678"));
        Assert.Contains(result.Findings, f => f.Contains("Player One"));
    }
}
