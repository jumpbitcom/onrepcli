// Extracts full article text and media references from publisher article HTML.
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using OnlineReports.Client.Models;

namespace OnlineReports.Client.Services;

public static partial class HtmlArticleParser
{
    public static ArticleDetails Parse(string html, string? fallbackImageUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        foreach (Match match in JsonLdScriptRegex().Matches(html))
        {
            using var document = JsonDocument.Parse(match.Groups["json"].Value);
            if (TryFindArticle(document.RootElement, out var article))
            {
                return new ArticleDetails(
                    GetString(article, "headline") ?? GetString(article, "name"),
                    GetString(article, "description"),
                    CleanText(GetString(article, "articleBody")),
                    GetAuthor(article),
                    GetImage(article) ?? GetOpenGraphImage(html) ?? fallbackImageUrl,
                    GetMediaLinks(article));
            }
        }

        return new ArticleDetails(
            GetTitle(html),
            GetMetaContent(html, "name", "description"),
            null,
            null,
            GetOpenGraphImage(html) ?? fallbackImageUrl,
            []);
    }

    private static bool TryFindArticle(JsonElement element, out JsonElement article)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            {
                if (TryFindArticle(child, out article))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            if (IsArticle(element))
            {
                article = element;
                return true;
            }

            foreach (var property in element.EnumerateObject())
            {
                if (property.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Object
                    && TryFindArticle(property.Value, out article))
                {
                    return true;
                }
            }
        }

        article = default;
        return false;
    }

    private static bool IsArticle(JsonElement element)
    {
        if (!element.TryGetProperty("@type", out var type))
        {
            return false;
        }

        return type.ValueKind == JsonValueKind.String
            ? IsArticleType(type.GetString())
            : type.ValueKind == JsonValueKind.Array && type.EnumerateArray()
                .Any(value => value.ValueKind == JsonValueKind.String && IsArticleType(value.GetString()));
    }

    private static bool IsArticleType(string? value) =>
        value is not null && (value.Equals("Article", StringComparison.OrdinalIgnoreCase)
            || value.Equals("NewsArticle", StringComparison.OrdinalIgnoreCase));

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static string? GetAuthor(JsonElement article)
    {
        if (!article.TryGetProperty("author", out var author))
        {
            return null;
        }

        if (author.ValueKind == JsonValueKind.String)
        {
            return author.GetString();
        }

        if (author.ValueKind == JsonValueKind.Object)
        {
            return GetString(author, "name");
        }

        if (author.ValueKind == JsonValueKind.Array)
        {
            return string.Join(", ", author.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : GetString(item, "name"))
                .Where(name => !string.IsNullOrWhiteSpace(name)));
        }

        return null;
    }

    private static string? GetImage(JsonElement article)
    {
        if (!article.TryGetProperty("image", out var image))
        {
            return null;
        }

        return image.ValueKind switch
        {
            JsonValueKind.String => image.GetString(),
            JsonValueKind.Array => image.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : GetString(item, "url"))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)),
            JsonValueKind.Object => GetString(image, "url"),
            _ => null
        };
    }

    private static string[] GetMediaLinks(JsonElement article)
    {
        if (!article.TryGetProperty("video", out var video))
        {
            return [];
        }

        var urls = video.ValueKind switch
        {
            JsonValueKind.String => [video.GetString()],
            JsonValueKind.Array => video.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : GetString(item, "contentUrl") ?? GetString(item, "url"))
                .ToArray(),
            JsonValueKind.Object => [GetString(video, "contentUrl") ?? GetString(video, "url")],
            _ => []
        };

        return urls
            .OfType<string>()
            .Where(value => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? GetOpenGraphImage(string html) =>
        GetMetaContent(html, "property", "og:image");

    private static string? GetMetaContent(string html, string attribute, string name)
    {
        var pattern = $@"<meta\b(?=[^>]*\b{Regex.Escape(attribute)}\s*=\s*['""]{Regex.Escape(name)}['""])(?=[^>]*\bcontent\s*=\s*['""](?<content>[^'""]*)['""])[^>]*>";
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["content"].Value) : null;
    }

    private static string? GetTitle(string html)
    {
        var match = Regex.Match(html, @"<title[^>]*>(?<title>.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? CleanText(match.Groups["title"].Value) : null;
    }

    private static string? CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var withoutTags = HtmlTagRegex().Replace(value, " ");
        return Regex.Replace(WebUtility.HtmlDecode(withoutTags), @"\s+", " ").Trim();
    }

    [GeneratedRegex(@"<script\b[^>]*\btype\s*=\s*['""]application/ld\+json['""][^>]*>(?<json>.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex JsonLdScriptRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.Singleline)]
    private static partial Regex HtmlTagRegex();
}
