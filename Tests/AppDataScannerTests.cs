using CustosAC.Abstractions;
using CustosAC.Configuration;
using CustosAC.Scanner;
using CustosAC.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CustosAC.Tests;

public class AppDataScannerTests
{
    private readonly MockFileSystemService _fileSystem;
    private readonly MockConsoleUI _consoleUI;
    private readonly Mock<IKeywordMatcher> _keywordMatcher;
    private readonly ScanSettings _scanSettings;

    public AppDataScannerTests()
    {
        _fileSystem = new MockFileSystemService();
        _consoleUI = new MockConsoleUI();
        _keywordMatcher = new Mock<IKeywordMatcher>();

        _scanSettings = new ScanSettings
        {
            AppDataScanDepth = 3,
            ExecutableExtensions = new[] { ".exe", ".dll" },
            ExcludedDirectories = new[] { "node_modules", "$recycle.bin" },
            ParallelScanEnabled = false,
            MaxDegreeOfParallelism = 1
        };
    }

    private AppDataScannerAsync CreateScanner()
    {
        return new AppDataScannerAsync(
            _fileSystem,
            _keywordMatcher.Object,
            _consoleUI,
            NullLogger<AppDataScannerAsync>.Instance,
            Options.Create(_scanSettings));
    }

    [Fact]
    public async Task ScanAsync_EmptyDirectories_ReturnsNoFindings()
    {
        // Arrange
        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanAsync();

        // Assert
        Assert.True(result.Success);
        Assert.False(result.HasFindings);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public async Task ScanAsync_IgnoresNonExecutableFiles()
    {
        // Arrange
        _keywordMatcher.Setup(k => k.ContainsKeyword(It.IsAny<string>())).Returns(true);
        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanAsync();

        // Assert - even with keyword matcher returning true,
        // without actual exe files in mock, should find nothing
        Assert.True(result.Success);
    }

    [Fact]
    public void Scanner_HasCorrectNameAndDescription()
    {
        // Arrange
        var scanner = CreateScanner();

        // Assert
        Assert.Equal("AppData Scanner", scanner.Name);
        Assert.NotEmpty(scanner.Description);
    }

    [Fact]
    public async Task ScanAsync_SupportsCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var scanner = CreateScanner();

        // Act
        var result = await scanner.ScanAsync(cts.Token);

        // Assert - should handle cancellation gracefully
        Assert.NotNull(result);
    }
}
