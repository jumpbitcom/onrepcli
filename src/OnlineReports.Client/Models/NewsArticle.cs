// Represents a feed story and its locally archived article details.
using System.Text.Json.Serialization;

namespace OnlineReports.Client.Models;

public sealed record NewsArticle
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? Body { get; init; }
    public string? Author { get; init; }
    public DateTimeOffset PublishedAt { get; init; }
    public string[] Categories { get; init; } = [];
    public string? ImageUrl { get; init; }
    public string? LocalImagePath { get; init; }
    public string[] MediaLinks { get; init; } = [];

    [JsonIgnore]
    public bool IsNew { get; init; }

    [JsonIgnore]
    public bool HasLocalImage => !string.IsNullOrWhiteSpace(LocalImagePath) && File.Exists(LocalImagePath);
}
