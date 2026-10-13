using OnRepCli.Services;

namespace OnRepCli.Tests;

public sealed class NewsPageParserTests
{
    private static readonly Uri HomePage = new("https://www.onlinereports.ch/");

    [Fact]
    public void ParseReturnsThreeNewestInternalStoriesAndSections()
    {
        const string html = """
            <nav><a href="/rubrik/politik">Politik</a><a href="https://example.org">External</a></nav>
            <main>
              <article><h2><a href="/story/one">Story one headline</a></h2><p>First summary text.</p><time datetime="2026-06-01T10:00:00Z"></time></article>
              <article><h2><a href="/story/two">Story two headline</a></h2><p>Second summary text.</p><time datetime="2026-06-03T10:00:00Z"></time></article>
              <article><h2><a href="/story/three">Story three headline</a></h2><p>Third summary text.</p><time datetime="2026-06-02T10:00:00Z"></time></article>
              <article><h2><a href="/story/four">Story four headline</a></h2><p>Fourth summary text.</p><time datetime="2026-05-01T10:00:00Z"></time></article>
              <article><h2><a href="https://example.org/story">External story headline</a></h2></article>
            </main>
            """;

        var snapshot = NewsPageParser.Parse(HomePage, html);

        Assert.Equal(3, snapshot.Articles.Count);
        Assert.Equal(
            ["Story two headline", "Story three headline", "Story one headline"],
            snapshot.Articles.Select(article => article.Title));
        Assert.Equal("Second summary text.", snapshot.Articles[0].Summary);
        Assert.Equal("News", snapshot.Articles[0].Section);
        Assert.Contains(snapshot.Sections, section => section.Name == "Politik");
        Assert.DoesNotContain(snapshot.Articles, article => article.Title.StartsWith("External", StringComparison.Ordinal));
    }

    [Fact]
    public void ParseThrowsWhenPageHasNoNewsHeadlines()
    {
        const string html = "<html><body><h1>Online Reports</h1></body></html>";

        Assert.Throws<InvalidDataException>(() => NewsPageParser.Parse(HomePage, html));
    }
}
