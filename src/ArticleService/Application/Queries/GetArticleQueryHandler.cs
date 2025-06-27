using ArticleService.Application.DTOs;
using ArticleService.Application.Mappers;
using ArticleService.Application.Repositories;
using ArticleService.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ArticleService.Application.Queries;

public sealed class GetArticleQueryHandler : IRequestHandler<GetArticleQuery, ArticleDto?>
{
    private readonly IArticleRepository _articleRepository;
    private readonly ILogger<GetArticleQueryHandler> _logger;

    public GetArticleQueryHandler(
        IArticleRepository articleRepository,
        ILogger<GetArticleQueryHandler> logger)
    {
        _articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ArticleDto?> Handle(GetArticleQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting article with ID: {ArticleId}", request.ArticleId);

        var article = await _articleRepository.GetByIdAsync(new ArticleId(request.ArticleId), cancellationToken);
        
        if (article == null)
        {
            _logger.LogWarning("Article not found with ID: {ArticleId}", request.ArticleId);
            return null;
        }

        return article.ToDto();
    }
}

public sealed class GetArticlesQueryHandler : IRequestHandler<GetArticlesQuery, ArticlesPageDto>
{
    private readonly IArticleRepository _articleRepository;
    private readonly ILogger<GetArticlesQueryHandler> _logger;

    public GetArticlesQueryHandler(
        IArticleRepository articleRepository,
        ILogger<GetArticlesQueryHandler> logger)
    {
        _articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ArticlesPageDto> Handle(GetArticlesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting articles with IDs count: {Count}", request.ArticleIds.Length);

        if (request.ArticleIds.Length == 0)
        {
            return new ArticlesPageDto { Articles = [], TotalCount = 0 };
        }

        var articleIds = request.ArticleIds.Select(id => new ArticleId(id));
        var articles = await _articleRepository.GetByIdsAsync(articleIds, cancellationToken);
        
        var articleDtos = articles.Select(a => a.ToDto()).ToArray();

        // Simple pagination logic - in a real app, you might want more sophisticated token-based pagination
        var skip = ParsePageToken(request.PageToken);
        var pageArticles = articleDtos.Skip(skip).Take(request.PageSize).ToArray();
        var nextPageToken = (skip + request.PageSize < articleDtos.Length) 
            ? GeneratePageToken(skip + request.PageSize) 
            : null;

        return new ArticlesPageDto
        {
            Articles = pageArticles,
            TotalCount = articleDtos.Length,
            NextPageToken = nextPageToken
        };
    }

    private static int ParsePageToken(string? pageToken)
    {
        if (string.IsNullOrWhiteSpace(pageToken))
            return 0;

        try
        {
            var bytes = Convert.FromBase64String(pageToken);
            var tokenString = Encoding.UTF8.GetString(bytes);
            return int.TryParse(tokenString, out var skip) ? skip : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string GeneratePageToken(int skip)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(skip.ToString());
        return Convert.ToBase64String(tokenBytes);
    }
} 