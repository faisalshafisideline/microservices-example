using Carter;
using Grpc.Net.Client;

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReportingService.Application.Consumers;
using ReportingService.Application.Services;
using ReportingService.Endpoints;
using ReportingService.Infrastructure.Data;
using Serilog;
using Shared.Contracts.Grpc;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Carter for minimal APIs
builder.Services.AddCarter();

// Add Entity Framework
builder.Services.AddDbContext<ReportingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add gRPC client for Article Service (simplified for now)
var articleServiceEndpoint = builder.Configuration.GetValue<string>("ArticleService:GrpcEndpoint") ?? "http://localhost:8081";
// Note: AddGrpcClient requires Grpc.Net.ClientFactory package
// For now, we'll register a simple gRPC client
builder.Services.AddSingleton(provider =>
{
    var channel = GrpcChannel.ForAddress(articleServiceEndpoint);
    return new ArticleService.ArticleServiceClient(channel);
});

// Register gRPC client wrapper
builder.Services.AddScoped<IArticleGrpcClient, ArticleGrpcClient>();

// Add MassTransit with RabbitMQ and consumers
builder.Services.AddMassTransit(x =>
{
    // Add consumers
    x.AddConsumer<ArticleCreatedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqHost = builder.Configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost";
        var rabbitMqUsername = builder.Configuration.GetValue<string>("RabbitMQ:Username") ?? "guest";
        var rabbitMqPassword = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest";

        cfg.Host(rabbitMqHost, h =>
        {
            h.Username(rabbitMqUsername);
            h.Password(rabbitMqPassword);
        });

        // Configure message topology
        cfg.Message<Shared.Contracts.Messages.ArticleCreatedEvent>(configTopology =>
        {
            configTopology.SetEntityName("article.created");
        });

        cfg.Message<Shared.Contracts.Messages.ArticleViewedEvent>(configTopology =>
        {
            configTopology.SetEntityName("article.viewed");
        });

        // Configure consumers
        cfg.ReceiveEndpoint("reporting-service-article-created", e =>
        {
            e.ConfigureConsumer<ArticleCreatedEventConsumer>(context);
            
            // Configure retry policy
            e.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15)));
                
            // Configure error handling
            e.UseInMemoryOutbox(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Register simple publisher wrapper
builder.Services.AddScoped<IPublisher>(provider =>
    new MassTransitPublisher(provider.GetRequiredService<IPublishEndpoint>()));

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

// Map Carter endpoints
app.MapCarter();

// Map health checks
app.MapHealthChecks("/health");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
    await context.Database.EnsureCreatedAsync();
}

try
{
    Log.Information("Starting Reporting Service");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Reporting Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Simple MassTransit publisher wrapper
public sealed class MassTransitPublisher : IPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    }

    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        return _publishEndpoint.Publish(message, cancellationToken);
    }
} 