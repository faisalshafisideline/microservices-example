using AuthService.Application.Services;
using AuthService.Endpoints;
using AuthService.Infrastructure.Data;
using AuthService.Infrastructure.Services;
using Carter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Shared.Contracts.Extensions;
using Shared.Contracts.UserContext.Extensions;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "LISA AI - Auth Service", 
        Version = "v1",
        Description = "OAuth2 Authentication Service for LISA AI Platform"
    });
    
    // Add JWT Bearer authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Application Services
builder.Services.AddScoped<IAuthService, AuthService.Infrastructure.Services.AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Carter for minimal APIs
builder.Services.AddCarter();

// User Context services
builder.Services.AddUserContext();

// Scalability and Robustness services
builder.Services.AddScalabilityServices(builder.Configuration);

// Health checks
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Note: AddScalabilityHealthChecks() will be implemented in shared contracts
// builder.Services.AddScalabilityHealthChecks();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LISA AI - Auth Service v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseSerilogRequestLogging();

app.UseCors();

// Use User Context middleware
app.UseUserContext();

app.UseAuthentication();
app.UseAuthorization();

// Map Carter endpoints
app.MapCarter();

// Health checks
app.MapHealthChecks("/health");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await context.Database.EnsureCreatedAsync();
    
    // Seed initial data if needed
    await SeedInitialDataAsync(context);
}

app.Run();

static async Task SeedInitialDataAsync(AuthDbContext context)
{
    // Check if we already have users
    if (await context.Users.AnyAsync())
        return;

    // Create system admin user
    var adminUser = new AuthService.Domain.Entities.User(
        email: "admin@lisa.ai",
        username: "admin",
        passwordHash: BCrypt.Net.BCrypt.HashPassword("admin123"),
        firstName: "System",
        lastName: "Administrator"
    );
    
    adminUser.VerifyEmail(); // Admin starts verified
    
    context.Users.Add(adminUser);
    await context.SaveChangesAsync();
    
    Log.Information("Seeded initial admin user: admin@lisa.ai / admin123");
} 