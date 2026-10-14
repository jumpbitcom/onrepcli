// Contains article metadata extracted from the publisher's structured page data.
namespace OnlineReports.Client.Models;

public sealed record ArticleDetails(
    string? Title,
    string? Description,
    string? Body,
    string? Author,
    string? ImageUrl,
    string[] MediaLinks);
