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
    private const string TopNewsKey = "top";
    private const string AllArticlesKey = "all";
    private const int TopNewsCount = 3;

    private readonly NewsService _newsService;
    private bool _initialized;
    private bool _isStartupLoading;
    private string? _statusError;
    private int _statusWarningCount;
    private string? _statusWarningDetail;

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
    public bool HasMediaLinks => SelectedArticle?.MediaLinks.Length > 0;
    public bool HasStatusText => !string.IsNullOrWhiteSpace(StatusText);
    public string EmptyStateText => Text.NoStories;
    public bool ShowEmptyState => !IsArticleSelected && VisibleArticles.Count == 0;

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
        _isStartupLoading = true;
        UpdateStatusText();
        try
        {
            var archive = await _newsService.LoadArchiveAsync();
            ApplyArticles(archive.Articles);
            LastSyncAt = archive.LastSyncAt;
            LastSyncCount = archive.LoadedArticleCount;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            SetStatusError(exception.Message);
        }

        await RefreshCoreAsync(CancellationToken.None, startup: true);
        IsInitialized = true;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task RefreshAsync(CancellationToken cancellationToken) =>
        RefreshCoreAsync(cancellationToken, startup: false);

    [RelayCommand]
    private void BackToStories() => SelectedArticle = null;

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
            SetStatusError(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            SetStatusError(exception.Message);
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
            SetStatusError(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            SetStatusError(exception.Message);
        }
    }

    partial void OnLanguageChanged(string value)
    {
        Text = ClientText.For(value);
        RebuildSections();
        RefreshVisibleArticles();
        UpdateDerivedProperties();
        UpdateStatusText();
    }

    partial void OnStatusTextChanged(string value) => OnPropertyChanged(nameof(HasStatusText));

    partial void OnSelectedSectionChanged(SectionOption? value) => RefreshVisibleArticles();

    partial void OnSelectedArticleChanged(NewsArticle? value)
    {
        OnPropertyChanged(nameof(IsArticleSelected));
        OnPropertyChanged(nameof(ShowEmptyState));
        UpdateDerivedProperties();
        RefreshMediaLinks();
    }

    partial void OnLastSyncAtChanged(DateTimeOffset? value) => UpdateDerivedProperties();

    partial void OnLastSyncCountChanged(int value) => UpdateDerivedProperties();

    private async Task RefreshCoreAsync(CancellationToken cancellationToken, bool startup)
    {
        IsRefreshing = true;
        _isStartupLoading = startup;
        _statusError = null;
        _statusWarningDetail = null;
        UpdateStatusText();
        try
        {
            var result = await _newsService.RefreshAsync(cancellationToken);
            ApplyArticles(result.Articles, result.NewlyAddedIds);
            LastSyncAt = result.CompletedAt;
            LastSyncCount = result.FeedCount;
            if (result.ArticleErrors.Count > 0)
            {
                _statusWarningCount = result.ArticleErrors.Count;
                _statusWarningDetail = result.ArticleErrors[0];
            }
            else
            {
                _statusWarningCount = 0;
                _statusWarningDetail = null;
            }

            UpdateStatusText();
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException or JsonException or XmlException or InvalidOperationException or ArgumentException)
        {
            SetStatusError(exception.Message);
        }
        finally
        {
            _isStartupLoading = false;
            UpdateStatusText();
            IsRefreshing = false;
            IsInitialized = !startup || IsInitialized;
        }
    }

    private void SetStatusError(string message)
    {
        _statusError = message;
        _statusWarningDetail = null;
        _isStartupLoading = false;
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        StatusText = _statusError is not null
            ? string.Format(CurrentCulture, Text.UpdateFailed, _statusError)
            : _statusWarningDetail is not null
                ? string.Format(CurrentCulture, Text.UpdateWarning, _statusWarningCount, _statusWarningDetail)
                : _isStartupLoading ? Text.Updating : string.Empty;
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
        var key = SelectedSection?.Key ?? TopNewsKey;
        var sectionArticles = Articles
            .Where(article => IsInSection(article, key))
            .OrderByDescending(article => article.PublishedAt);
        // Top News shows only the newest stories; the other sections list every archived article.
        var visible = key == TopNewsKey
            ? sectionArticles.Take(TopNewsCount).ToArray()
            : sectionArticles.ToArray();

        VisibleArticles.Clear();
        foreach (var article in visible)
        {
            VisibleArticles.Add(article);
        }

        OnPropertyChanged(nameof(EmptyStateText));
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    private void RebuildSections()
    {
        var selectedKey = SelectedSection?.Key ?? TopNewsKey;
        Sections.Clear();
        Sections.Add(new SectionOption(TopNewsKey, Text.TopNews));
        Sections.Add(new SectionOption("news", Text.News));
        Sections.Add(new SectionOption("politics", Text.Politics));
        Sections.Add(new SectionOption("economy", Text.Economy));
        Sections.Add(new SectionOption("culture", Text.Culture));
        Sections.Add(new SectionOption("columns", Text.Columns));
        Sections.Add(new SectionOption(AllArticlesKey, Text.AllArticles));
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
        OnPropertyChanged(nameof(HasMediaLinks));
        OnPropertyChanged(nameof(HasStatusText));
        OnPropertyChanged(nameof(EmptyStateText));
        OnPropertyChanged(nameof(ShowEmptyState));
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
        if (sectionKey is TopNewsKey or AllArticlesKey)
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
