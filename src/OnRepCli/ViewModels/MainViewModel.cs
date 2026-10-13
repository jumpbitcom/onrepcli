using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OnRepCli.Models;
using OnRepCli.Services;

namespace OnRepCli.ViewModels;

public partial class MainViewModel(INewsService newsService) : ViewModelBase
{
    private const string HomePageUrl = "https://www.onlinereports.ch/";
    private CancellationTokenSource? activeRefresh;
    private bool hasLoaded;

    public ObservableCollection<NewsArticle> Articles { get; } = [];
    public ObservableCollection<NewsSection> Sections { get; } = [new("All news", HomePageUrl)];

    [ObservableProperty]
    private NewsSection? selectedSection = new("All news", HomePageUrl);

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string statusMessage = "The latest from Basel and the surrounding region.";

    [ObservableProperty]
    private bool hasArticles;

    partial void OnSelectedSectionChanged(NewsSection? value)
    {
        if (hasLoaded && value is not null)
        {
            RefreshCommand.Execute(null);
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        using var refreshCancellation = new CancellationTokenSource();
        var previousRefresh = Interlocked.Exchange(ref activeRefresh, refreshCancellation);
        previousRefresh?.Cancel();

        IsRefreshing = true;
        var section = SelectedSection ?? Sections[0];
        StatusMessage = $"Loading {section.Name}…";

        try
        {
            var snapshot = await newsService.GetLatestAsync(section.Url, refreshCancellation.Token);
            Articles.Clear();
            foreach (var article in snapshot.Articles.Take(3))
            {
                Articles.Add(article);
            }

            Sections.Clear();
            foreach (var newsSection in snapshot.Sections)
            {
                Sections.Add(newsSection);
            }

            SelectedSection = Sections.FirstOrDefault(item => item.Url == section.Url) ?? Sections[0];
            HasArticles = Articles.Count > 0;
            var updateTime = snapshot.UpdatedAt.ToLocalTime().ToString("t");
            StatusMessage = snapshot.IsOfflineCopy
                ? $"Offline — showing saved headlines from {updateTime}."
                : $"Updated at {updateTime}.";
        }
        catch (OperationCanceledException) when (refreshCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (
            exception is HttpRequestException or InvalidDataException or TaskCanceledException)
        {
            Articles.Clear();
            HasArticles = false;
            StatusMessage = "News could not be loaded. Check your connection and try again.";
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
