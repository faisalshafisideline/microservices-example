using ArticleService.Domain.Entities;
using ArticleService.Domain.ValueObjects;

namespace ArticleService.Application.Repositories;

public interface IArticleRepository
{
    Task<Article?> GetByIdAsync(ArticleId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Article>> GetByIdsAsync(IEnumerable<ArticleId> ids, CancellationToken cancellationToken = default);
    Task<IEnumerable<Article>> GetByAuthorAsync(AuthorId authorId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Article> Articles, int TotalCount)> GetPagedAsync(int pageSize, int skip = 0, ArticleStatus? status = null, CancellationToken cancellationToken = default);
    Task<Article> AddAsync(Article article, CancellationToken cancellationToken = default);
    Task UpdateAsync(Article article, CancellationToken cancellationToken = default);
    Task DeleteAsync(Article article, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(ArticleId id, CancellationToken cancellationToken = default);
} 