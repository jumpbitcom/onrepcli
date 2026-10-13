using System.Globalization;
using HtmlAgilityPack;
using OnRepCli.Models;

namespace OnRepCli.Services;

public sealed class NewsPageParser
{
    private static readonly string[] CandidateClassNames = ["news", "article", "teaser", "story", "card"];
    private const string HomePage = "https://www.onlinereports.ch/";

    public NewsSnapshot Parse(Uri pageUri, string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var sections = new List<NewsSection> { new("All news", HomePage) };
        foreach (var link in document.DocumentNode.SelectNodes("//nav//a[@href]")?.Cast<HtmlNode>() ?? [])
        {
            var name = CleanText(link.InnerText);
            var uri = GetInternalUri(pageUri, link.GetAttributeValue("href", ""));
            if (name.Length is < 2 or > 50 || uri is null || IsHomePage(uri))
            {
                continue;
            }

            if (sections.All(section => !section.Url.Equals(uri.AbsoluteUri, StringComparison.OrdinalIgnoreCase)))
            {
                sections.Add(new NewsSection(name, uri.AbsoluteUri));
            }
        }

        var articles = new List<NewsArticle>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var heading in document.DocumentNode.SelectNodes("//h1|//h2|//h3|//h4")?.Cast<HtmlNode>() ?? [])
        {
            var title = CleanText(heading.InnerText);
            if (title.Length < 12)
            {
                continue;
            }

            var container = FindArticleContainer(heading);
            var link = heading.SelectSingleNode(".//a[@href]")
                ?? container?.SelectSingleNode(".//a[@href]");
            var uri = link is null ? null : GetInternalUri(pageUri, link.GetAttributeValue("href", ""));
            if (uri is null || !seenUrls.Add(uri.AbsoluteUri) || uri == pageUri)
            {
                continue;
            }

            var summary = container?.SelectSingleNode(".//p");
            var summaryText = summary is null ? "" : CleanText(summary.InnerText);
            if (summaryText.Equals(title, StringComparison.OrdinalIgnoreCase))
            {
                summaryText = "";
            }

            var publishedAt = ParsePublicationDate(container);
            articles.Add(new NewsArticle(
                title,
                summaryText,
                "News",
                publishedAt,
                uri.AbsoluteUri));
        }

        if (articles.Count == 0)
        {
            throw new InvalidDataException("No news articles were found on the Online Reports page.");
        }

        var currentSection = sections.FirstOrDefault(section =>
            section.Url.Equals(pageUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))?.Name ?? "News";
        var latestArticles = articles
            .Select((article, index) => (article, index))
            .OrderByDescending(item => item.article.PublishedAt ?? DateTimeOffset.MinValue)
            .ThenBy(item => item.index)
            .Take(3)
            .Select(item => item.article with { Section = currentSection })
            .ToArray();

        return new NewsSnapshot(latestArticles, sections, DateTimeOffset.UtcNow);
    }

    private static HtmlNode? FindArticleContainer(HtmlNode heading) =>
        heading.Ancestors().FirstOrDefault(node =>
            node.Name.Equals("article", StringComparison.OrdinalIgnoreCase)
            || CandidateClassNames.Any(className => HasClass(node, className)));

    private static DateTimeOffset? ParsePublicationDate(HtmlNode? container)
    {
        var dateValue = container?.SelectSingleNode(".//time[@datetime]")?.GetAttributeValue("datetime", "")
            ?? container?.SelectSingleNode(".//time")?.InnerText;

        return DateTimeOffset.TryParse(
            dateValue,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var publishedAt)
            ? publishedAt
            : null;
    }

    private static Uri? GetInternalUri(Uri pageUri, string href)
    {
        if (!Uri.TryCreate(pageUri, href, out var uri)
            || uri.Scheme is not ("https" or "http")
            || !uri.Host.Equals("onlinereports.ch", StringComparison.OrdinalIgnoreCase)
                && !uri.Host.Equals("www.onlinereports.ch", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri;
    }

    private static bool IsHomePage(Uri uri) =>
        uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query);

    private static bool HasClass(HtmlNode node, string className) =>
        node.GetAttributeValue("class", "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(value => value.Contains(className, StringComparison.OrdinalIgnoreCase));

    private static string CleanText(string text) =>
        HtmlEntity.DeEntitize(string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))).Trim();
}
