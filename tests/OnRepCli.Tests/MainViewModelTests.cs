using OnRepCli.Models;
using OnRepCli.Services;
using OnRepCli.ViewModels;

namespace OnRepCli.Tests;

public sealed class MainViewModelTests
{
    [Fact]
    public async Task RefreshAsyncShowsAtMostThreeStoriesAndSections()
    {
        var snapshot = new NewsSnapshot(
            [
                new("First headline", "First summary", "News", null, "https://www.onlinereports.ch/1"),
                new("Second headline", "Second summary", "News", null, "https://www.onlinereports.ch/2"),
                new("Third headline", "Third summary", "News", null, "https://www.onlinereports.ch/3"),
                new("Fourth headline", "Fourth summary", "News", null, "https://www.onlinereports.ch/4"),
            ],
            [
                new("All news", "https://www.onlinereports.ch/"),
                new("Politics", "https://www.onlinereports.ch/politics"),
            ],
            DateTimeOffset.UtcNow);
        var viewModel = new MainViewModel(new StubNewsService(snapshot));

        await viewModel.RefreshAsync();

        Assert.Equal(3, viewModel.Articles.Count);
        Assert.Equal("First headline", viewModel.Articles[0].Title);
        Assert.Equal(2, viewModel.Sections.Count);
        Assert.True(viewModel.HasArticles);
        Assert.False(viewModel.IsRefreshing);
    }

    private sealed class StubNewsService(NewsSnapshot snapshot) : INewsService
    {
        public Task<NewsSnapshot> GetLatestAsync(string pageUrl, CancellationToken cancellationToken) =>
            Task.FromResult(snapshot);
    }
}
