using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CustosAC.Core.Services;
using CustosAC.WPF.ViewModels.Base;

namespace CustosAC.WPF.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private const string GitHubRepo = "dybeky/custosAC";
    private const string ChangelogUrl = "https://raw.githubusercontent.com/dybeky/custosAC/master/change.md";

    [ObservableProperty]
    private string _changelog = "Loading changelog...";

    [ObservableProperty]
    private string _releaseVersion = "";

    [ObservableProperty]
    private string _releaseDate = "";

    [ObservableProperty]
    private bool _isLoadingChangelog = true;

    [ObservableProperty]
    private string _infoText = "Программа написана для серверов COBRA, для выявления стороннего ПО.";

    public DashboardViewModel(CheatHashDatabase hashDatabase, KeywordMatcherService keywordMatcher)
    {
        LoadChangelogAsync();
    }

    private async void LoadChangelogAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "custosAC");
            client.Timeout = TimeSpan.FromSeconds(15);

            // Load version from GitHub releases
            var releaseTask = client.GetStringAsync($"https://api.github.com/repos/{GitHubRepo}/releases/latest");

            // Load changelog from change.md
            var changelogTask = client.GetStringAsync(ChangelogUrl);

            await Task.WhenAll(releaseTask, changelogTask);

            // Parse release info
            using var doc = JsonDocument.Parse(await releaseTask);
            var tagName = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            var publishedAt = doc.RootElement.GetProperty("published_at").GetString();

            // Get changelog content
            var changelogContent = await changelogTask;
            changelogContent = CleanMarkdown(changelogContent);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ReleaseVersion = tagName;
                Changelog = changelogContent;

                if (DateTime.TryParse(publishedAt, out var date))
                {
                    ReleaseDate = date.ToString("dd.MM.yyyy");
                }

                IsLoadingChangelog = false;
            });
        }
        catch
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Changelog = "Failed to load changelog. Check your internet connection.";
                IsLoadingChangelog = false;
            });
        }
    }

    private static string CleanMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Remove markdown headers (## Header)
        text = Regex.Replace(text, @"^#{1,6}\s*", "", RegexOptions.Multiline);

        // Remove bullet points (- item, * item, • item)
        text = Regex.Replace(text, @"^[\-\*•]\s*", "", RegexOptions.Multiline);

        // Remove numbered lists (1. item)
        text = Regex.Replace(text, @"^\d+\.\s*", "", RegexOptions.Multiline);

        // Remove bold/italic (**text** or *text* or __text__ or _text_)
        text = Regex.Replace(text, @"\*\*([^*]+)\*\*", "$1");
        text = Regex.Replace(text, @"\*([^*]+)\*", "$1");
        text = Regex.Replace(text, @"__([^_]+)__", "$1");
        text = Regex.Replace(text, @"_([^_]+)_", "$1");

        // Remove inline code (`code`)
        text = Regex.Replace(text, @"`([^`]+)`", "$1");

        // Remove links [text](url) -> text
        text = Regex.Replace(text, @"\[([^\]]+)\]\([^)]+\)", "$1");

        // Clean up multiple newlines
        text = Regex.Replace(text, @"\n{3,}", "\n\n");

        return text.Trim();
    }
}
