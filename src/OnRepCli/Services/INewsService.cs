using OnRepCli.Models;

namespace OnRepCli.Services;

public interface INewsService
{
    Task<NewsSnapshot> GetLatestAsync(string pageUrl, CancellationToken cancellationToken);
}
