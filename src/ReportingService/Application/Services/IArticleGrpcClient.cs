using Shared.Contracts.Grpc;

namespace ReportingService.Application.Services;

public interface IArticleGrpcClient
{
    Task<GetArticleResponse?> GetArticleAsync(string articleId, CancellationToken cancellationToken = default);
    Task<GetArticlesResponse?> GetArticlesAsync(IEnumerable<string> articleIds, int pageSize = 10, string? pageToken = null, CancellationToken cancellationToken = default);
}

public sealed class ArticleGrpcClient : IArticleGrpcClient
{
    private readonly ArticleService.ArticleServiceClient _client;
    private readonly ILogger<ArticleGrpcClient> _logger;

    public ArticleGrpcClient(ArticleService.ArticleServiceClient client, ILogger<ArticleGrpcClient> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetArticleResponse?> GetArticleAsync(string articleId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Calling Article Service gRPC for article: {ArticleId}", articleId);

            var request = new GetArticleRequest { ArticleId = articleId };
            var response = await _client.GetArticleAsync(request, cancellationToken: cancellationToken);

            if (!response.Found)
            {
                _logger.LogWarning("Article not found in Article Service: {ArticleId}", articleId);
                return null;
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Article Service gRPC for article: {ArticleId}", articleId);
            throw;
        }
    }

    public async Task<GetArticlesResponse?> GetArticlesAsync(
        IEnumerable<string> articleIds, 
        int pageSize = 10, 
        string? pageToken = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ids = articleIds.ToArray();
            _logger.LogDebug("Calling Article Service gRPC for {Count} articles", ids.Length);

            var request = new GetArticlesRequest
            {
                PageSize = pageSize,
                PageToken = pageToken ?? string.Empty
            };
            request.ArticleIds.AddRange(ids);

            var response = await _client.GetArticlesAsync(request, cancellationToken: cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Article Service gRPC for multiple articles");
            throw;
        }
    }
} 