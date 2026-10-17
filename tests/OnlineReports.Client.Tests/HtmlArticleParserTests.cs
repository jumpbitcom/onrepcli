// Verifies extraction of text and article metadata without downloading media.
using OnlineReports.Client.Services;

namespace OnlineReports.Client.Tests;

public sealed class HtmlArticleParserTests
{
    [Fact]
    public void Parse_ArticleJsonLd_ExtractsTextAndMediaMetadata()
    {
        const string html = """
            <html>
              <head><meta property="og:image" content="https://media-onlinereports.wepublish.cloud/cover.webp" /></head>
              <body>
                <script type="application/ld+json">
                  {
                    "@type": "NewsArticle",
                    "headline": "Article title",
                    "description": "Article summary",
                    "articleBody": "First paragraph.\nSecond paragraph.",
                    "author": { "@type": "Person", "name": "Reporter" },
                    "video": { "contentUrl": "https://media.example.com/video.mp4" }
                  }
                </script>
              </body>
            </html>
            """;

        var details = HtmlArticleParser.Parse(html);

        Assert.Equal("Article title", details.Title);
        Assert.Equal("Article summary", details.Description);
        Assert.Equal("First paragraph. Second paragraph.", details.Body);
        Assert.Equal("Reporter", details.Author);
        Assert.Equal("https://media-onlinereports.wepublish.cloud/cover.webp", details.ImageUrl);
        Assert.Equal(["https://media.example.com/video.mp4"], details.MediaLinks);
    }

    [Fact]
    public void Parse_PageWithoutStructuredArticle_UsesMetadataAndFallbackImage()
    {
        const string html = """
            <html>
              <head>
                <title>Page title</title>
                <meta name="description" content="Page summary" />
              </head>
              <body></body>
            </html>
            """;

        var details = HtmlArticleParser.Parse(html, "https://media-onlinereports.wepublish.cloud/fallback.webp");

        Assert.Equal("Page title", details.Title);
        Assert.Equal("Page summary", details.Description);
        Assert.Equal("https://media-onlinereports.wepublish.cloud/fallback.webp", details.ImageUrl);
        Assert.Null(details.Body);
    }
}
