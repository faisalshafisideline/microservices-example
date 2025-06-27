using MassTransit;
using Microsoft.EntityFrameworkCore;
using ReportingService.Domain.Entities;
using ReportingService.Domain.ValueObjects;
using ReportingService.Infrastructure.Data;
using Shared.Contracts.Messages;

namespace ReportingService.Application.Consumers;

public sealed class ArticleCreatedEventConsumer : IConsumer<ArticleCreatedEvent>
{
    private readonly ReportingDbContext _context;
    private readonly ILogger<ArticleCreatedEventConsumer> _logger;

    public ArticleCreatedEventConsumer(
        ReportingDbContext context,
        ILogger<ArticleCreatedEventConsumer> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<ArticleCreatedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Processing ArticleCreatedEvent for Article ID: {ArticleId}, Event ID: {EventId}",
            message.ArticleId,
            message.EventId);

        try
        {
            // Check for idempotency - avoid processing the same event twice
            var existingReport = await _context.ArticleReports
                .FirstOrDefaultAsync(ar => ar.ArticleId == new ArticleId(message.ArticleId), context.CancellationToken);

            if (existingReport != null)
            {
                _logger.LogWarning(
                    "Article report already exists for Article ID: {ArticleId}. Skipping duplicate event.",
                    message.ArticleId);
                
                return; // Idempotent - already processed
            }

            // Create new article report
            var articleReport = new ArticleReport(
                new ArticleId(message.ArticleId),
                message.Title,
                message.AuthorId,
                message.AuthorName,
                message.CreatedAt.DateTime,
                message.Category,
                message.Tags,
                message.EstimatedReadTimeMinutes);

            await _context.ArticleReports.AddAsync(articleReport, context.CancellationToken);
            await _context.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Successfully created article report for Article ID: {ArticleId}",
                message.ArticleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing ArticleCreatedEvent for Article ID: {ArticleId}, Event ID: {EventId}",
                message.ArticleId,
                message.EventId);

            // Re-throw to trigger retry mechanism
            throw;
        }
    }
} 