using ArticleService.Application.Repositories;
using ArticleService.Domain.Entities;
using ArticleService.Domain.ValueObjects;
using ArticleService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ArticleService.Infrastructure.Repositories;

public sealed class ArticleRepository : IArticleRepository
{
    private readonly ArticleDbContext _context;

    public ArticleRepository(ArticleDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Article?> GetByIdAsync(ArticleId id, CancellationToken cancellationToken = default)
    {
        return await _context.Articles
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Article>> GetByIdsAsync(IEnumerable<ArticleId> ids, CancellationToken cancellationToken = default)
    {
        var idValues = ids.Select(id => id.Value).ToList();
        return await _context.Articles
            .Where(a => idValues.Contains(a.Id.Value))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Article>> GetByAuthorAsync(AuthorId authorId, CancellationToken cancellationToken = default)
    {
        return await _context.Articles
            .Where(a => a.AuthorId == authorId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Article> Articles, int TotalCount)> GetPagedAsync(
        int pageSize, 
        int skip = 0, 
        ArticleStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Articles.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        
        var articles = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (articles, totalCount);
    }

    public async Task<Article> AddAsync(Article article, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Articles.AddAsync(article, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(Article article, CancellationToken cancellationToken = default)
    {
        _context.Articles.Update(article);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Article article, CancellationToken cancellationToken = default)
    {
        _context.Articles.Remove(article);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(ArticleId id, CancellationToken cancellationToken = default)
    {
        return await _context.Articles
            .AnyAsync(a => a.Id == id, cancellationToken);
    }
} 