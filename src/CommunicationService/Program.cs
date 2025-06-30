using Carter;
using CommunicationService.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Contracts.UserContext.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/communicationservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Carter
builder.Services.AddCarter();

// Add Entity Framework
builder.Services.AddDbContext<CommunicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Authentication & Authorization
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

// Add User Context
builder.Services.AddUserContext();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Add User Context Middleware
app.UseUserContext();

// Add Carter
app.MapCarter();

// Add Health Checks
app.MapHealthChecks("/health");

try
{
    Log.Information("Starting CommunicationService");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CommunicationService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
} 