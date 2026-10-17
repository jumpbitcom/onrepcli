// Coordinates RSS refreshes, article downloads and the local offline archive.
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OnlineReports.Client.Models;

namespace OnlineReports.Client.Services;

public sealed class NewsService(HttpClient httpClient, string archiveDirectory)
{
    private const string FeedUrl = "https://onlinereports.ch/api/rss-feed";
    private const int MaxImageBytes = 10 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _archivePath = Path.Combine(archiveDirectory, "articles.json");
    private readonly SemaphoreSlim _archiveLock = new(1, 1);

    public async Task<NewsArchive> LoadArchiveAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_archivePath))
        {
            return new NewsArchive(null, 0, []);
        }

        await using var stream = File.OpenRead(_archivePath);
        return await JsonSerializer.DeserializeAsync<NewsArchive>(stream, JsonOptions, cancellationToken)
            ?? new NewsArchive(null, 0, []);
    }

    public async Task<RefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        using var feedResponse = await httpClient.GetAsync(FeedUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        feedResponse.EnsureSuccessStatusCode();
        var feedXml = await feedResponse.Content.ReadAsStringAsync(cancellationToken);
        var feedEntries = RssFeedParser.Parse(feedXml);
        var archive = await LoadArchiveAsync(cancellationToken);
        var archived = new ConcurrentDictionary<string, NewsArticle>(
            archive.Articles
                .Select(article => article with { IsNew = false })
                .ToDictionary(article => article.Id, StringComparer.Ordinal),
            StringComparer.Ordinal);
        var newlyAddedIds = feedEntries
            .Where(entry => !archived.ContainsKey(entry.Id))
            .Select(entry => entry.Id)
            .ToHashSet(StringComparer.Ordinal);
        var pageErrors = new ConcurrentBag<string>();
        using var concurrencyLimit = new SemaphoreSlim(3);

        var fetchTasks = feedEntries
            .Where(entry => !archived.TryGetValue(entry.Id, out var existing) || string.IsNullOrWhiteSpace(existing.Body))
            .Select(async entry =>
            {
                await concurrencyLimit.WaitAsync(cancellationToken);
                try
                {
                    archived.TryGetValue(entry.Id, out var priorArticle);
                    var result = await FetchArticleAsync(entry, cancellationToken);
                    if (result.Error is not null)
                    {
                        pageErrors.Add($"{entry.Title}: {result.Error}");
                        if (priorArticle is not null)
                        {
                            return;
                        }
                    }

                    var article = result.Article;
                    if (priorArticle?.HasLocalImage == true)
                    {
                        article = article with { LocalImagePath = priorArticle.LocalImagePath };
                    }
                    else if (!string.IsNullOrWhiteSpace(article.ImageUrl))
                    {
                        try
                        {
                            var imagePath = await DownloadImageAsync(article.Id, article.ImageUrl, cancellationToken);
                            article = article with { LocalImagePath = imagePath };
                        }
                        catch (HttpRequestException exception)
                        {
                            pageErrors.Add($"{entry.Title}: {exception.Message}");
                        }
                        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
                        {
                            pageErrors.Add($"{entry.Title}: {exception.Message}");
                        }
                        catch (InvalidDataException exception)
                        {
                            pageErrors.Add($"{entry.Title}: {exception.Message}");
                        }
                        catch (InvalidOperationException exception)
                        {
                            pageErrors.Add($"{entry.Title}: {exception.Message}");
                        }
                    }

                    archived[entry.Id] = article with { IsNew = newlyAddedIds.Contains(entry.Id) };
                }
                finally
                {
                    concurrencyLimit.Release();
                }
            });

        await Task.WhenAll(fetchTasks);

        foreach (var entry in feedEntries.Where(entry => !archived.ContainsKey(entry.Id)))
        {
            archived[entry.Id] = ToArticle(entry);
        }

        var orderedArticles = archived.Values
            .OrderByDescending(article => article.PublishedAt)
            .ToArray();
        var completedAt = DateTimeOffset.UtcNow;
        await SaveArchiveAsync(new NewsArchive(completedAt, feedEntries.Count, orderedArticles), cancellationToken);

        return new RefreshResult(
            orderedArticles,
            feedEntries.Count,
            newlyAddedIds,
            pageErrors.ToArray(),
            completedAt);
    }

    public async Task<NewsArticle> LoadImageAsync(NewsArticle article, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(article);
        if (article.HasLocalImage)
        {
            return article;
        }

        var imagePath = await DownloadImageAsync(article.Id, article.ImageUrl, cancellationToken);

        var updatedArticle = article with { LocalImagePath = imagePath };
        var archive = await LoadArchiveAsync(cancellationToken);
        var articles = archive.Articles
            .Select(existing => existing.Id == article.Id ? updatedArticle : existing)
            .ToArray();
        if (articles.All(existing => existing.Id != article.Id))
        {
            articles = [.. articles, updatedArticle];
        }

        await SaveArchiveAsync(archive with { Articles = articles }, cancellationToken);
        return updatedArticle;
    }

    private async Task<string> DownloadImageAsync(
        string articleId,
        string? imageUrl,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var imageUri)
            || imageUri.Scheme != Uri.UriSchemeHttps
            || !imageUri.Host.Equals("media-onlinereports.wepublish.cloud", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This article does not have an available image from the publisher.");
        }

        using var response = await httpClient.GetAsync(imageUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not ("image/jpeg" or "image/png" or "image/gif" or "image/webp"))
        {
            throw new InvalidDataException("The publisher returned an unsupported article image format.");
        }

        if (response.Content.Headers.ContentLength is > MaxImageBytes)
        {
            throw new InvalidDataException("The article image is larger than the 10 MB download limit.");
        }

        var extension = mediaType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => ".img"
        };
        var imageDirectory = Path.Combine(archiveDirectory, "images");
        Directory.CreateDirectory(imageDirectory);
        var imageName = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(articleId))) + extension;
        var imagePath = Path.Combine(imageDirectory, imageName);

        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var target = File.Create(imagePath))
        {
            var buffer = new byte[81920];
            var totalBytes = 0;
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                totalBytes += bytesRead;
                if (totalBytes > MaxImageBytes)
                {
                    throw new InvalidDataException("The article image is larger than the 10 MB download limit.");
                }

                await target.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }
        }

        return imagePath;
    }

    private async Task<(NewsArticle Article, string? Error)> FetchArticleAsync(
        FeedEntry entry,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(entry.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var details = HtmlArticleParser.Parse(html, entry.ImageUrl);
            var article = ToArticle(entry) with
            {
                Title = details.Title ?? entry.Title,
                Description = details.Description ?? entry.Description,
                Body = details.Body,
                Author = details.Author,
                ImageUrl = details.ImageUrl ?? entry.ImageUrl,
                MediaLinks = details.MediaLinks
            };

            return string.IsNullOrWhiteSpace(article.Body)
                ? (article, "The publisher page did not include full article text.")
                : (article, null);
        }
        catch (HttpRequestException exception)
        {
            return (ToArticle(entry), exception.Message);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return (ToArticle(entry), exception.Message);
        }
    }

    private async Task SaveArchiveAsync(NewsArchive archive, CancellationToken cancellationToken)
    {
        await _archiveLock.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(archiveDirectory);
            var temporaryPath = _archivePath + ".tmp";
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, archive, JsonOptions, cancellationToken);
            }

            File.Move(temporaryPath, _archivePath, overwrite: true);
        }
        finally
        {
            _archiveLock.Release();
        }
    }

    private static NewsArticle ToArticle(FeedEntry entry) =>
        new()
        {
            Id = entry.Id,
            Title = entry.Title,
            Url = entry.Url,
            Description = entry.Description,
            PublishedAt = entry.PublishedAt,
            Categories = entry.Categories,
            ImageUrl = entry.ImageUrl
        };
}

public sealed record RefreshResult(
    IReadOnlyList<NewsArticle> Articles,
    int FeedCount,
    IReadOnlySet<string> NewlyAddedIds,
    IReadOnlyList<string> ArticleErrors,
    DateTimeOffset CompletedAt);
