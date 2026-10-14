// Defines the durable snapshot written to the user's application data folder.
namespace OnlineReports.Client.Models;

public sealed record NewsArchive(
    DateTimeOffset? LastSyncAt,
    IReadOnlyList<NewsArticle> Articles);
