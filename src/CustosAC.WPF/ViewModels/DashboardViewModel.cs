using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CustosAC.Core.Services;
using CustosAC.WPF.ViewModels.Base;

namespace CustosAC.WPF.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private const string ChangelogUrl = "https://raw.githubusercontent.com/dybeky/custosAC/master/change.md";
    private readonly VersionService _versionService;

    [ObservableProperty]
    private string _changelog = "";

    [ObservableProperty]
    private string _releaseVersion = "...";

    [ObservableProperty]
    private string _releaseDate = "";

    [ObservableProperty]
    private bool _isLoadingChangelog = true;

    public LocalizationService Localization => LocalizationService.Instance;

    public DashboardViewModel(CheatHashDatabase hashDatabase, KeywordMatcherService keywordMatcher, VersionService versionService)
    {
        _versionService = versionService;
        Changelog = Localization.Strings.Loading;
        LoadDataAsync();
    }

    private async void LoadDataAsync()
    {
        // Load version from centralized service
        await _versionService.LoadVersionAsync();

        string changelog = "";

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "custosAC");
            client.Timeout = TimeSpan.FromSeconds(30);

            // Load changelog from change.md
            var changelogResponse = await client.GetAsync(ChangelogUrl);

            if (changelogResponse.IsSuccessStatusCode)
            {
                changelog = await changelogResponse.Content.ReadAsStringAsync();
                changelog = CleanMarkdown(changelog);
            }
            else
            {
                changelog = Localization.Strings.ChangelogNotFound;
            }
        }
        catch
        {
            changelog = Localization.Strings.FailedToLoad;
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ReleaseVersion = _versionService.Version;
            ReleaseDate = _versionService.ReleaseDate;
            Changelog = changelog;
            IsLoadingChangelog = false;
        });
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
