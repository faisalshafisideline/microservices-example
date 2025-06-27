using ArticleService.Application.DTOs;
using MediatR;

namespace ArticleService.Application.Queries;

public sealed record GetArticleQuery(string ArticleId) : IRequest<ArticleDto?>;

public sealed record GetArticlesQuery(
    string[] ArticleIds,
    int PageSize = 10,
    string? PageToken = null) : IRequest<ArticlesPageDto>; 