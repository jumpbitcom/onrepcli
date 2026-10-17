// Parses the publisher's RSS 2.0 feed without downloading its media.
using System.Globalization;
using System.Net;
using System.Xml.Linq;
using OnlineReports.Client.Models;

namespace OnlineReports.Client.Services;

public static partial class RssFeedParser
{
    private static readonly XNamespace ContentNamespace = "http://purl.org/rss/1.0/modules/content/";
    private static readonly Uri PublisherBaseUri = new("https://onlinereports.ch/");

    public static IReadOnlyList<FeedEntry> Parse(string xml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);

        var document = XDocument.Parse(xml);
        var entries = new List<FeedEntry>();

        foreach (var item in document.Descendants("item"))
        {
            var title = Decode(item.Element("title")?.Value);
            var link = item.Element("link")?.Value.Trim();
            if (string.IsNullOrWhiteSpace(title) || !TryGetPublisherUrl(link, out var articleUri))
            {
                continue;
            }

            var description = Decode(item.Element("description")?.Value);
            var content = Decode(item.Element(ContentNamespace + "encoded")?.Value);
            var categories = item.Elements("category")
                .Select(category => Decode(category.Value))
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var enclosureUrl = item.Element("enclosure")?.Attribute("url")?.Value;
            var imageUrl = TryGetHttpsUrl(enclosureUrl, out var parsedImageUrl) ? parsedImageUrl.AbsoluteUri : null;
            var date = DateTimeOffset.TryParse(
                item.Element("pubDate")?.Value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var publishedAt)
                ? publishedAt
                : DateTimeOffset.MinValue;

            entries.Add(new FeedEntry(
                item.Element("guid")?.Value.Trim() is { Length: > 0 } guid ? guid : articleUri.AbsoluteUri,
                title,
                articleUri.AbsoluteUri,
                string.IsNullOrWhiteSpace(description) ? content : description,
                date,
                categories,
                imageUrl));
        }

        return entries;
    }

    private static bool TryGetPublisherUrl(string? value, out Uri articleUri)
    {
        articleUri = PublisherBaseUri;
        if (!Uri.TryCreate(PublisherBaseUri, value, out var candidate)
            || candidate.Scheme != Uri.UriSchemeHttps
            || !candidate.Host.Equals(PublisherBaseUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        articleUri = candidate;
        return true;
    }

    private static bool TryGetHttpsUrl(string? value, out Uri result)
    {
        result = PublisherBaseUri;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var candidate) || candidate.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        result = candidate;
        return true;
    }

    private static string Decode(string? value) =>
        WebUtility.HtmlDecode(value ?? string.Empty).Trim();
}
