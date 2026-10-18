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
                  <enclosure url="https://media-onlinereports.wepublish.cloud/story.webp" type="image/webp" />
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
            if (request.RequestUri?.AbsoluteUri == "https://media-onlinereports.wepublish.cloud/story.webp")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent([1, 2, 3])
                    {
                        Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/webp") }
                    }
                };
            }

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
            Assert.Equal(4, handler.RequestCount);
            Assert.Equal("Full text", archive.Articles.Single().Body);
            Assert.Equal("Reporter", archive.Articles.Single().Author);
            Assert.NotNull(archive.LastSyncAt);
            Assert.Equal(1, archive.LoadedArticleCount);
            Assert.True(File.Exists(archive.Articles.Single().LocalImagePath));
            Assert.True(Assert.Single(firstRefresh.Articles).IsNew);
            Assert.Equal("Full text", Assert.Single(secondRefresh.Articles).Body);
            Assert.False(Assert.Single(secondRefresh.Articles).IsNew);
            Assert.Contains("story-1", firstRefresh.NewlyAddedIds);
            Assert.Empty(secondRefresh.NewlyAddedIds);
        }
        finally
        {
            if (Directory.Exists(archiveDirectory))
            {
                Directory.Delete(archiveDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RefreshAsync_ArchivedArticleWithoutImage_DownloadsFirstImageWithoutRefetchingPage()
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
                </item>
              </channel>
            </rss>
            """;
        const string article = """
            <html><script type="application/ld+json">
            {"@type":"NewsArticle","headline":"Story title","articleBody":"Full text",
             "image":[{"@type":"ImageObject","contentUrl":"https://media-onlinereports.wepublish.cloud/first.webp"},
                      "https://media-onlinereports.wepublish.cloud/second.webp"]}
            </script></html>
            """;

        var imageAvailable = false;
        var pageRequests = 0;
        var handler = new StubHttpMessageHandler(request =>
        {
            switch (request.RequestUri?.AbsoluteUri)
            {
                case "https://onlinereports.ch/api/rss-feed":
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(feed, Encoding.UTF8, "application/rss+xml") };
                case "https://onlinereports.ch/a/story":
                    pageRequests++;
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(article, Encoding.UTF8, "text/html") };
                case "https://media-onlinereports.wepublish.cloud/first.webp" when imageAvailable:
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent([1, 2, 3])
                        {
                            Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/webp") }
                        }
                    };
                case "https://media-onlinereports.wepublish.cloud/first.webp":
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                default:
                    throw new InvalidOperationException($"Unexpected request URI: {request.RequestUri}");
            }
        });
        using var httpClient = new HttpClient(handler);
        var archiveDirectory = Path.Combine(Path.GetTempPath(), $"onlinereports-tests-{Guid.NewGuid():N}");
        var service = new NewsService(httpClient, archiveDirectory);

        try
        {
            var firstRefresh = await service.RefreshAsync();
            imageAvailable = true;
            var secondRefresh = await service.RefreshAsync();

            Assert.False(Assert.Single(firstRefresh.Articles).HasLocalImage);
            var refreshed = Assert.Single(secondRefresh.Articles);
            Assert.Equal("https://media-onlinereports.wepublish.cloud/first.webp", refreshed.ImageUrl);
            Assert.True(refreshed.HasLocalImage);
            Assert.Equal("Full text", refreshed.Body);
            Assert.Equal(1, pageRequests);
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
