// Verifies feed parsing, category extraction and publisher URL validation.
using OnlineReports.Client.Services;

namespace OnlineReports.Client.Tests;

public sealed class RssFeedParserTests
{
    [Fact]
    public void Parse_ValidItem_ExtractsArticleMetadata()
    {
        const string xml = """
            <rss version="2.0">
              <channel>
                <item>
                  <title><![CDATA[Basel &amp; Umgebung]]></title>
                  <link>https://onlinereports.ch/a/basel</link>
                  <guid>story-1</guid>
                  <pubDate>Wed, 14 Oct 2026 16:25:03 GMT</pubDate>
                  <description><![CDATA[Ein Bericht &amp; mehr.]]></description>
                  <category>News</category>
                  <category>Politik</category>
                  <enclosure url="https://media-onlinereports.wepublish.cloud/image.webp" type="image/webp" />
                </item>
              </channel>
            </rss>
            """;

        var entry = Assert.Single(RssFeedParser.Parse(xml));

        Assert.Equal("story-1", entry.Id);
        Assert.Equal("Basel & Umgebung", entry.Title);
        Assert.Equal("Ein Bericht & mehr.", entry.Description);
        Assert.Equal(["News", "Politik"], entry.Categories);
        Assert.Equal("https://media-onlinereports.wepublish.cloud/image.webp", entry.ImageUrl);
        Assert.Equal(new DateTimeOffset(2026, 10, 14, 16, 25, 3, TimeSpan.Zero), entry.PublishedAt);
    }

    [Fact]
    public void Parse_ItemWithForeignHost_DoesNotReturnUnsafeLink()
    {
        const string xml = """
            <rss version="2.0">
              <channel>
                <item>
                  <title>External</title>
                  <link>https://example.com/article</link>
                  <guid>external-1</guid>
                </item>
              </channel>
            </rss>
            """;

        Assert.Empty(RssFeedParser.Parse(xml));
    }
}
