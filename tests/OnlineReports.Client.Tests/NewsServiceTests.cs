// Verifies refresh archives full text once and reads subsequent updates from the cache.
using System.Net;
using System.Text;
using OnlineReports.Client.Services;

namespace OnlineReports.Client.Tests;

public sealed class NewsServiceTests
{
    [Fact]
    public async Task RefreshAsync_ArchivesNewArticleAndDoesNotRedownloadCachedFullText()
    {
        const string feed = """
            <rss version="2.0">
              <channel>
                <item>
                  <title>Story title</title>
                  <link>https://onlinereports.ch/a/story</link>
                  <guid>story-1</guid>
                  <pubDate>Wed, 14 Oct 2026 16:25:03 GMT</pubDate>
                  <description>Story summary</description>
                  <category>News</category>
                </item>
              </channel>
            </rss>
            """;
        const string article = """
            <html><script type="application/ld+json">
            {"@type":"NewsArticle","headline":"Story title","articleBody":"Full text","author":{"name":"Reporter"}}
            </script></html>
            """;

        var handler = new StubHttpMessageHandler(request =>
        {
            var responseContent = request.RequestUri?.AbsoluteUri switch
            {
                "https://onlinereports.ch/api/rss-feed" => feed,
                "https://onlinereports.ch/a/story" => article,
                _ => throw new InvalidOperationException($"Unexpected request URI: {request.RequestUri}")
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "text/html")
            };
        });
        using var httpClient = new HttpClient(handler);
        var archiveDirectory = Path.Combine(Path.GetTempPath(), $"onlinereports-tests-{Guid.NewGuid():N}");
        var service = new NewsService(httpClient, archiveDirectory);

        try
        {
            var firstRefresh = await service.RefreshAsync();
            var secondRefresh = await service.RefreshAsync();
            var archive = await service.LoadArchiveAsync();

            Assert.Single(firstRefresh.Articles);
            Assert.Equal(3, handler.RequestCount);
            Assert.Equal("Full text", archive.Articles.Single().Body);
            Assert.Equal("Reporter", archive.Articles.Single().Author);
            Assert.NotNull(archive.LastSyncAt);
            Assert.Equal("Full text", Assert.Single(secondRefresh.Articles).Body);
        }
        finally
        {
            if (Directory.Exists(archiveDirectory))
            {
                Directory.Delete(archiveDirectory, recursive: true);
            }
        }
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> createResponse)
        : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(createResponse(request));
        }
    }
}
