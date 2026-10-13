using System.Globalization;

namespace OnRepCli.Models;

public sealed record NewsArticle(
    string Title,
    string Summary,
    string Section,
    DateTimeOffset? PublishedAt,
    string Url)
{
    public string DisplayDate => PublishedAt?.ToLocalTime().ToString("d MMM yyyy", CultureInfo.CurrentCulture) ?? "";
}
