using CustosAC.Configuration;
using CustosAC.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace CustosAC.Tests;

public class KeywordMatcherTests
{
    private readonly KeywordMatcherService _matcher;

    public KeywordMatcherTests()
    {
        var settings = Options.Create(new KeywordSettings
        {
            Patterns = new[] { "undead", "melony", "hack", "cheat", "inject" }
        });
        _matcher = new KeywordMatcherService(settings);
    }

    [Theory]
    [InlineData("undead_loader.exe", true)]
    [InlineData("melony_solution.dll", true)]
    [InlineData("hacktool.exe", true)]
    [InlineData("cheat_engine.zip", true)]
    [InlineData("inject_dll.exe", true)]
    [InlineData("notepad.exe", false)]
    [InlineData("chrome.exe", false)]
    [InlineData("system32.dll", false)]
    public void ContainsKeyword_ShouldMatchCorrectly(string input, bool expected)
    {
        var result = _matcher.ContainsKeyword(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ContainsKeyword_EmptyString_ReturnsFalse()
    {
        Assert.False(_matcher.ContainsKeyword(""));
    }

    [Fact]
    public void ContainsKeyword_NullString_ReturnsFalse()
    {
        Assert.False(_matcher.ContainsKeyword(null!));
    }

    [Fact]
    public void ContainsKeyword_IsCaseInsensitive()
    {
        Assert.True(_matcher.ContainsKeyword("UNDEAD.exe"));
        Assert.True(_matcher.ContainsKeyword("Melony.dll"));
        Assert.True(_matcher.ContainsKeyword("HACK"));
        Assert.True(_matcher.ContainsKeyword("ChEaT"));
    }

    [Fact]
    public void ContainsKeyword_PartialMatch_ReturnsTrue()
    {
        Assert.True(_matcher.ContainsKeyword("my_undead_loader_v2.exe"));
        Assert.True(_matcher.ContainsKeyword("super_hack_tool.dll"));
    }

    [Fact]
    public void GetKeywords_ReturnsAllConfiguredKeywords()
    {
        var keywords = _matcher.GetKeywords();
        Assert.Equal(5, keywords.Count);
        Assert.Contains("undead", keywords);
        Assert.Contains("melony", keywords);
        Assert.Contains("hack", keywords);
        Assert.Contains("cheat", keywords);
        Assert.Contains("inject", keywords);
    }

    [Fact]
    public void GetKeywordsString_ReturnsSpaceSeparatedKeywords()
    {
        var keywordsString = _matcher.GetKeywordsString();
        Assert.Contains("undead", keywordsString);
        Assert.Contains("melony", keywordsString);
        Assert.Contains(" ", keywordsString);
    }
}
