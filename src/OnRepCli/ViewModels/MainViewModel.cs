using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OnRepCli.Models;
using OnRepCli.Resources;
using OnRepCli.Services;

namespace OnRepCli.ViewModels;

public partial class MainViewModel(INewsService newsService) : ViewModelBase
{
    private const string HomePageUrl = "https://www.onlinereports.ch/";
    private CancellationTokenSource? activeRefresh;
    private bool hasLoaded;
    private bool isUpdatingSections;

    public ObservableCollection<NewsArticle> Articles { get; } = [];
    public ObservableCollection<NewsSection> Sections { get; } = [new(Strings.AllNews, HomePageUrl)];

    [ObservableProperty]
    private NewsSection? selectedSection = new(Strings.AllNews, HomePageUrl);

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string statusMessage = Strings.WelcomeStatus;

    [ObservableProperty]
    private bool hasArticles;

    partial void OnSelectedSectionChanged(NewsSection? value)
    {
        if (hasLoaded && !isUpdatingSections && value is not null)
        {
            RefreshCommand.Execute(null);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    public async Task RefreshAsync()
    {
        using var refreshCancellation = new CancellationTokenSource();
        var previousRefresh = Interlocked.Exchange(ref activeRefresh, refreshCancellation);
        previousRefresh?.Cancel();

        IsRefreshing = true;
        var section = SelectedSection ?? Sections[0];
        StatusMessage = Strings.LoadingStatus(section.Name);

        try
        {
            var snapshot = await newsService.GetLatestAsync(section.Url, refreshCancellation.Token);
            refreshCancellation.Token.ThrowIfCancellationRequested();
            Articles.Clear();
            foreach (var article in snapshot.Articles.Take(3))
            {
                Articles.Add(article);
            }

            if (!Sections.SequenceEqual(snapshot.Sections))
            {
                isUpdatingSections = true;
                try
                {
                    Sections.Clear();
                    foreach (var newsSection in snapshot.Sections)
                    {
                        Sections.Add(newsSection);
                    }

                    SelectedSection = Sections.FirstOrDefault(item => item.Url == section.Url) ?? Sections[0];
                }
                finally
                {
                    isUpdatingSections = false;
                }
            }

            HasArticles = Articles.Count > 0;
            var updateTime = snapshot.UpdatedAt.ToLocalTime().ToString("t", CultureInfo.CurrentCulture);
            StatusMessage = snapshot.IsOfflineCopy
                ? Strings.OfflineStatus(updateTime)
                : Strings.UpdatedStatus(updateTime);
        }
        catch (OperationCanceledException) when (refreshCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (
            exception is HttpRequestException or InvalidDataException or TaskCanceledException)
        {
            Articles.Clear();
            HasArticles = false;
            StatusMessage = Strings.ErrorStatus;
        }
        finally
        {
            hasLoaded = true;
            if (ReferenceEquals(Interlocked.CompareExchange(ref activeRefresh, null, refreshCancellation), refreshCancellation))
            {
                IsRefreshing = false;
            }
        }
    }
}
