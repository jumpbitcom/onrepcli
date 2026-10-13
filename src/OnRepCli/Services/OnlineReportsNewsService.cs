using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OnRepCli.Models;

namespace OnRepCli.Services;

public sealed class OnlineReportsNewsService(HttpClient httpClient, NewsPageParser parser) : INewsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<NewsSnapshot> GetLatestAsync(string pageUrl, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri)
            || pageUri.Scheme != Uri.UriSchemeHttps
            || pageUri.Host is not ("onlinereports.ch" or "www.onlinereports.ch"))
        {
            throw new ArgumentException("News pages must use the Online Reports website.", nameof(pageUrl));
        }

        try
        {
            using var response = await httpClient.GetAsync(pageUri, cancellationToken);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var snapshot = parser.Parse(pageUri, html);
            await SaveCacheAsync(pageUrl, snapshot, cancellationToken);
            return snapshot;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or InvalidDataException
            || exception is TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            var cachedSnapshot = await ReadCacheAsync(pageUrl, cancellationToken);
            if (cachedSnapshot is not null)
            {
                return cachedSnapshot with { IsOfflineCopy = true };
            }

            throw;
        }
    }

    private static async Task SaveCacheAsync(
        string pageUrl,
        NewsSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        try
        {
            var path = GetCachePath(pageUrl);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            await File.WriteAllTextAsync(path, json, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static async Task<NewsSnapshot?> ReadCacheAsync(string pageUrl, CancellationToken cancellationToken)
    {
        try
        {
            var path = GetCachePath(pageUrl);
            if (!File.Exists(path))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(path, cancellationToken);
            return JsonSerializer.Deserialize<NewsSnapshot>(json, JsonOptions);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private static string GetCachePath(string pageUrl)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pageUrl)));
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OnRepCli",
            $"{hash}.json");
    }
}
