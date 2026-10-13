namespace OnRepCli.Models;

public sealed record NewsSnapshot(
    IReadOnlyList<NewsArticle> Articles,
    IReadOnlyList<NewsSection> Sections,
    DateTimeOffset UpdatedAt,
    bool IsOfflineCopy = false);
