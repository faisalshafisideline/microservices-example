using ArticleService.Application.Common;
using ArticleService.Application.Repositories;
using ArticleService.Infrastructure;
using ArticleService.Infrastructure.Data;
using ArticleService.Infrastructure.Repositories;
using ArticleService.Services;
using Carter;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;
using Shared.Contracts.Extensions;
using Shared.Contracts.UserContext.Extensions;
using UnitOfWork = ArticleService.Infrastructure.UnitOfWork;

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

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Add Entity Framework
builder.Services.AddDbContext<ArticleDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add gRPC
builder.Services.AddGrpc();

// Add repositories and services
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Add User Context services
// builder.Services.AddUserContext();

// Add Scalability and Robustness services
// builder.Services.AddScalabilityServices(builder.Configuration);

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
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

        cfg.ConfigureEndpoints(context);
    });
});

// Add health checks
builder.Services.AddHealthChecks();

// Add scalability health checks
// builder.Services.AddScalabilityHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

// Use User Context middleware
// app.UseUserContext();

// Map Carter endpoints
app.MapCarter();

// Map gRPC services
app.MapGrpcService<ArticleGrpcService>();

// Map health checks
app.MapHealthChecks("/health");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ArticleDbContext>();
    await context.Database.EnsureCreatedAsync();
}

try
{
    Log.Information("Starting Article Service");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Article Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
} 