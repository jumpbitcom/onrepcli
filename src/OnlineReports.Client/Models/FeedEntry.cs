// Represents the small set of metadata required from an RSS item.
namespace OnlineReports.Client.Models;

public sealed record FeedEntry(
    string Id,
    string Title,
    string Url,
    string Description,
    DateTimeOffset PublishedAt,
    string[] Categories,
    string? ImageUrl);
