using ApiGateway.Authentication;
using ApiGateway.Authorization;
using ApiGateway.Endpoints;
using ApiGateway.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Serilog;
using Shared.Contracts.UserContext.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Microservices API Gateway", 
        Version = "v1",
        Description = "API Gateway for Article and Reporting Services with Basic Authentication"
    });

    // Configure Basic Authentication for Swagger
    c.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        In = ParameterLocation.Header,
        Description = "Basic Authorization header using the Bearer scheme."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Basic"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add Authentication Services
builder.Services.AddScoped<IUserStore, HardcodedUserStore>();
builder.Services.AddAuthentication(HardcodedAuthenticationSchemeOptions.SchemeName)
    .AddScheme<HardcodedAuthenticationSchemeOptions, HardcodedAuthenticationHandler>(
        HardcodedAuthenticationSchemeOptions.SchemeName, 
        options => { });

// Add Authorization Services
builder.Services.AddAuthorization(Policies.ConfigurePolicies);

// Add User Context Services
builder.Services.AddUserContext();

// Add CORS
var corsSettings = builder.Configuration.GetSection("Cors");
if (corsSettings.GetValue<bool>("EnableCors"))
{
    var policyName = corsSettings.GetValue<string>("PolicyName") ?? "ApiGatewayPolicy";
    var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? [];
    var allowedMethods = corsSettings.GetSection("AllowedMethods").Get<string[]>() ?? [];
    var allowedHeaders = corsSettings.GetSection("AllowedHeaders").Get<string[]>() ?? [];
    var allowCredentials = corsSettings.GetValue<bool>("AllowCredentials");

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(policyName, policy =>
        {
            if (allowedOrigins.Length > 0)
                policy.WithOrigins(allowedOrigins);
            else
                policy.AllowAnyOrigin();

            if (allowedMethods.Length > 0)
                policy.WithMethods(allowedMethods);
            else
                policy.AllowAnyMethod();

            if (allowedHeaders.Length > 0)
                policy.WithHeaders(allowedHeaders);
            else
                policy.AllowAnyHeader();

            if (allowCredentials)
                policy.AllowCredentials();
        });
    });
}

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
        c.RoutePrefix = "swagger";
    });
}

// Add Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());
        
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("Username", httpContext.User.Identity.Name);
            diagnosticContext.Set("UserRoles", string.Join(",", httpContext.User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)));
        }
    };
});

// Enable CORS
if (corsSettings.GetValue<bool>("EnableCors"))
{
    var policyName = corsSettings.GetValue<string>("PolicyName") ?? "ApiGatewayPolicy";
    app.UseCors(policyName);
}

// Add Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Add User Context middleware (after authentication, before authorization)
app.UseMiddleware<UserContextEnrichmentMiddleware>();

// Add custom authorization middleware for route-based policies
app.UseMiddleware<RouteBasedAuthorizationMiddleware>();

// Map Gateway endpoints
app.MapGatewayEndpoints();

// Map Health Checks
app.MapHealthChecks("/health");

// Map YARP Reverse Proxy
app.MapReverseProxy();

try
{
    Log.Information("Starting API Gateway");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
} 