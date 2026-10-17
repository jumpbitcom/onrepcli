// Drives feed refreshes, section navigation and article reading.
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OnlineReports.Client.Localization;
using OnlineReports.Client.Models;
using OnlineReports.Client.Services;

namespace OnlineReports.Client.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly NewsService _newsService;
    private bool _initialized;

    public MainViewModel(NewsService newsService)
    {
        _newsService = newsService;
        RebuildSections();
    }

    public ObservableCollection<NewsArticle> Articles { get; } = [];
    public ObservableCollection<NewsArticle> VisibleArticles { get; } = [];
    public ObservableCollection<MediaLinkViewModel> MediaLinks { get; } = [];
    public ObservableCollection<SectionOption> Sections { get; } = [];
    public string[] Languages { get; } = ["Deutsch", "Français", "English"];

    [ObservableProperty]
    private ClientText text = ClientText.For("Deutsch");

    [ObservableProperty]
    private string language = "Deutsch";

    [ObservableProperty]
    private SectionOption? selectedSection;

    [ObservableProperty]
    private NewsArticle? selectedArticle;

    [ObservableProperty]
    private DateTimeOffset? lastSyncAt;

    [ObservableProperty]
    private int lastSyncCount;

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private bool isInitialized;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool isLoadingImage;

    public bool IsArticleSelected => SelectedArticle is not null;
    public string SyncInfo => LastSyncAt is null
        ? Text.NeverUpdated
        : $"{Text.LastUpdated}: {LastSyncAt.Value.ToLocalTime().ToString("f", CurrentCulture)}{Environment.NewLine}{string.Format(CurrentCulture, Text.ArticlesLoaded, LastSyncCount)}";
    public string PublishedText => SelectedArticle is null
        ? string.Empty
        : $"{Text.Published}: {SelectedArticle.PublishedAt.ToLocalTime().ToString("f", CurrentCulture)}";
    public string AuthorText => string.IsNullOrWhiteSpace(SelectedArticle?.Author)
        ? string.Empty
        : $"{Text.By}: {SelectedArticle.Author}";
    public string SelectedArticleBody => string.IsNullOrWhiteSpace(SelectedArticle?.Body)
        ? string.IsNullOrWhiteSpace(SelectedArticle?.Description)
            ? Text.FullTextUnavailable
            : SelectedArticle.Description
        : SelectedArticle.Body;
    public string SectionsText => SelectedArticle is null || SelectedArticle.Categories.Length == 0
        ? string.Empty
        : $"{Text.Sections}: {string.Join(", ", SelectedArticle.Categories)}";
    public bool HasArticleImage => SelectedArticle?.HasLocalImage == true;
    public bool CanLoadImage => !string.IsNullOrWhiteSpace(SelectedArticle?.ImageUrl) && !HasArticleImage;
    public bool HasMediaLinks => SelectedArticle?.MediaLinks.Length > 0;
    public string EmptyStateText => VisibleArticles.Count == 0 ? Text.NoStories : Text.SelectStory;

    private CultureInfo CurrentCulture => CultureInfo.GetCultureInfo(Language switch
    {
        "Français" => "fr-CH",
        "English" => "en-GB",
        _ => "de-CH"
    });

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        StatusText = Text.Updating;
        try
        {
            var archive = await _newsService.LoadArchiveAsync();
            ApplyArticles(archive.Articles);
            LastSyncAt = archive.LastSyncAt;
            LastSyncCount = archive.LoadedArticleCount;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            StatusText = string.Format(CurrentCulture, Text.UpdateFailed, exception.Message);
        }

        await RefreshCoreAsync(CancellationToken.None, startup: true);
        IsInitialized = true;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task RefreshAsync(CancellationToken cancellationToken) =>
        RefreshCoreAsync(cancellationToken, startup: false);

    [RelayCommand]
    private void BackToStories() => SelectedArticle = null;

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task LoadImageAsync(CancellationToken cancellationToken)
    {
        if (SelectedArticle is null)
        {
            return;
        }

        IsLoadingImage = true;
        try
        {
            SelectedArticle = await _newsService.LoadImageAsync(SelectedArticle, cancellationToken);
            UpdateDerivedProperties();
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            StatusText = string.Format(CurrentCulture, Text.UpdateFailed, exception.Message);
        }
        finally
        {
            IsLoadingImage = false;
        }
    }

    [RelayCommand]
    private void OpenArticle()
    {
        if (!Uri.TryCreate(SelectedArticle?.Url, UriKind.Absolute, out var articleUri)
            || articleUri.Scheme != Uri.UriSchemeHttps
            || !articleUri.Host.Equals("onlinereports.ch", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(articleUri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            StatusText = string.Format(CurrentCulture, Text.UpdateFailed, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            StatusText = string.Format(CurrentCulture, Text.UpdateFailed, exception.Message);
        }
    }

    [RelayCommand]
    private void OpenMedia(string? mediaUrl)
    {
        if (!Uri.TryCreate(mediaUrl, UriKind.Absolute, out var mediaUri)
            || mediaUri.Scheme != Uri.UriSchemeHttps)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(mediaUri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            StatusText = string.Format(CurrentCulture, Text.UpdateFailed, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            StatusText = string.Format(CurrentCulture, Text.UpdateFailed, exception.Message);
        }
    }

    partial void OnLanguageChanged(string value)
    {
        Text = ClientText.For(value);
        RebuildSections();
        RefreshVisibleArticles();
        UpdateDerivedProperties();
        if (!IsRefreshing && IsInitialized)
        {
            StatusText = LastSyncAt is not null
                ? $"{Text.LastUpdated}: {LastSyncAt.Value.ToLocalTime().ToString("f", CurrentCulture)}"
                : Text.NeverUpdated;
        }
    }

    partial void OnSelectedSectionChanged(SectionOption? value) => RefreshVisibleArticles();

    partial void OnSelectedArticleChanged(NewsArticle? value)
    {
        OnPropertyChanged(nameof(IsArticleSelected));
        UpdateDerivedProperties();
        RefreshMediaLinks();
    }

    partial void OnLastSyncAtChanged(DateTimeOffset? value) => UpdateDerivedProperties();

    partial void OnLastSyncCountChanged(int value) => UpdateDerivedProperties();

    private async Task RefreshCoreAsync(CancellationToken cancellationToken, bool startup)
    {
        IsRefreshing = true;
        StatusText = Text.Updating;
        try
        {
            var previousIds = Articles.Select(article => article.Id).ToHashSet(StringComparer.Ordinal);
            var result = await _newsService.RefreshAsync(cancellationToken);
            ApplyArticles(result.Articles, result.NewlyAddedIds);
            LastSyncAt = result.CompletedAt;
            LastSyncCount = result.FeedCount;
            var newCount = result.Articles.Count(article => !previousIds.Contains(article.Id));
            StatusText = string.Format(CurrentCulture, Text.Updated, newCount);
            if (result.ArticleErrors.Count > 0)
            {
                StatusText += $" {string.Format(CurrentCulture, Text.SomeArticlesFailed, result.ArticleErrors.Count, result.ArticleErrors[0])}";
            }
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException or JsonException or XmlException or InvalidOperationException or ArgumentException)
        {
            StatusText = string.Format(CurrentCulture, Text.UpdateFailed, exception.Message);
        }
        finally
        {
            IsRefreshing = false;
            IsInitialized = !startup || IsInitialized;
        }
    }

    private void ApplyArticles(IEnumerable<NewsArticle> articles, IReadOnlySet<string>? newlyAddedIds = null)
    {
        var selectedId = SelectedArticle?.Id;
        var ordered = articles
            .Select(article => article with { IsNew = newlyAddedIds?.Contains(article.Id) == true })
            .OrderByDescending(article => article.PublishedAt)
            .ToArray();

        Articles.Clear();
        foreach (var article in ordered)
        {
            Articles.Add(article);
        }

        RefreshVisibleArticles();
        if (selectedId is not null)
        {
            SelectedArticle = Articles.FirstOrDefault(article => article.Id == selectedId);
        }
    }

    private void RefreshVisibleArticles()
    {
        var key = SelectedSection?.Key ?? "all";
        var visible = Articles
            .Where(article => IsInSection(article, key))
            .OrderByDescending(article => article.PublishedAt)
            .Take(3)
            .ToArray();

        VisibleArticles.Clear();
        foreach (var article in visible)
        {
            VisibleArticles.Add(article);
        }

        OnPropertyChanged(nameof(EmptyStateText));
    }

    private void RebuildSections()
    {
        var selectedKey = SelectedSection?.Key ?? "all";
        Sections.Clear();
        Sections.Add(new SectionOption("all", Text.AllStories));
        Sections.Add(new SectionOption("news", Text.News));
        Sections.Add(new SectionOption("politics", Text.Politics));
        Sections.Add(new SectionOption("economy", Text.Economy));
        Sections.Add(new SectionOption("culture", Text.Culture));
        Sections.Add(new SectionOption("columns", Text.Columns));
        SelectedSection = Sections.FirstOrDefault(section => section.Key == selectedKey) ?? Sections[0];
    }

    private void UpdateDerivedProperties()
    {
        OnPropertyChanged(nameof(SyncInfo));
        OnPropertyChanged(nameof(PublishedText));
        OnPropertyChanged(nameof(AuthorText));
        OnPropertyChanged(nameof(SelectedArticleBody));
        OnPropertyChanged(nameof(SectionsText));
        OnPropertyChanged(nameof(HasArticleImage));
        OnPropertyChanged(nameof(CanLoadImage));
        OnPropertyChanged(nameof(HasMediaLinks));
        OnPropertyChanged(nameof(EmptyStateText));
    }

    private void RefreshMediaLinks()
    {
        MediaLinks.Clear();
        foreach (var url in SelectedArticle?.MediaLinks ?? [])
        {
            MediaLinks.Add(new MediaLinkViewModel(url, OpenMediaCommand));
        }
    }

    private static bool IsInSection(NewsArticle article, string sectionKey)
    {
        if (sectionKey == "all")
        {
            return true;
        }

        return article.Categories.Any(category => sectionKey switch
        {
            "news" => category.Equals("News", StringComparison.OrdinalIgnoreCase),
            "politics" => category.Equals("Politik", StringComparison.OrdinalIgnoreCase)
                || category.Equals("Politics", StringComparison.OrdinalIgnoreCase),
            "economy" => category.Equals("Wirtschaft", StringComparison.OrdinalIgnoreCase)
                || category.Equals("Economy", StringComparison.OrdinalIgnoreCase),
            "culture" => category.Equals("Kultur & Gesellschaft", StringComparison.OrdinalIgnoreCase)
                || category.Equals("Culture & Society", StringComparison.OrdinalIgnoreCase)
                || category.Equals("Culture", StringComparison.OrdinalIgnoreCase),
            "columns" => category.Equals("Kolumnen", StringComparison.OrdinalIgnoreCase)
                || category.Equals("Kolumne", StringComparison.OrdinalIgnoreCase)
                || category.Equals("Columns", StringComparison.OrdinalIgnoreCase)
                || category.Equals("Das Wort zum Bild", StringComparison.OrdinalIgnoreCase),
            _ => false
        });
    }
}

public sealed record MediaLinkViewModel(string Url, IRelayCommand<string?> OpenCommand);
