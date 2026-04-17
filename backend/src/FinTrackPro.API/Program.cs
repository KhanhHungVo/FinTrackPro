using FinTrackPro.API.Infrastructure;
using FinTrackPro.API.Middleware;
using FinTrackPro.Application;
using FinTrackPro.Application.Common.Behaviors;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.BackgroundJobs;
using FinTrackPro.BackgroundJobs.Jobs;
using FinTrackPro.Infrastructure;
using FinTrackPro.Infrastructure.Auth;
using FinTrackPro.Infrastructure.Identity;
using FinTrackPro.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

const string CorsPolicyName = "AllowFrontend";
const string BearerScheme   = "Bearer";

var builder = WebApplication.CreateBuilder(args);

var iam      = builder.Configuration.GetSection(IdentityProviderOptions.SectionName).Get<IdentityProviderOptions>() ?? new();
var keycloak = builder.Configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>() ?? new();
var auth0    = builder.Configuration.GetSection(Auth0Options.SectionName).Get<Auth0Options>() ?? new();

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// Application & Infrastructure
builder.Services.AddApplicationServices();
builder.Services.AddOptions<LoggingBehaviorOptions>()
    .BindConfiguration(LoggingBehaviorOptions.SectionName);
builder.Services.AddInfrastructureServices(builder.Configuration);

// RFC 7807 Problem Details support (used by InvalidModelStateResponseFactory and ExceptionHandlingMiddleware)
builder.Services.AddProblemDetails();

// Controllers — API layer handles binding failures via InvalidModelStateResponseFactory (logged at [WRN]).
// Business-rule validation flows through ValidationBehavior → ExceptionHandlingMiddleware.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o =>
        o.InvalidModelStateResponseFactory = HandleInvalidModelState)
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

// OpenAPI (built-in .NET 10)
builder.Services.AddOpenApi();

// Authentication — JWT Bearer (Keycloak or Auth0)
builder.Services.AddAuthentication(BearerScheme)
    .AddJwtBearer(BearerScheme, ConfigureJwt);

builder.Services.AddAuthorization();

// CORS — allow frontend SPA
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicyName, policy =>
        policy.WithOrigins(builder.Configuration["Cors:Origins"]?.Split(',') ?? ["http://localhost:5173"])
              .AllowAnyHeader()
              .AllowAnyMethod()));

// Background jobs
builder.Services.AddBackgroundJobServices();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors(CorsPolicyName);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();            // /openapi/v1.json
    app.MapScalarApiReference(); // Scalar UI at /scalar
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseMiddleware<UserContextMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

// Hangfire Dashboard — Basic Auth
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireBasicAuthFilter(app.Configuration)]
});

// Run database migrations and seed system categories on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    await seeder.SeedAsync();
}

// Register recurring jobs
RecurringJob.AddOrUpdate<MarketSignalJob>(
    "market-signals",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 */4 * * *");   // every 4 hours

RecurringJob.AddOrUpdate<BudgetOverrunJob>(
    "budget-overrun",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Daily);

RecurringJob.AddOrUpdate<ExchangeRateSyncJob>(
    "exchange-rate-sync",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 */8 * * *");  // every 8 hours

// RecurringJob.AddOrUpdate<IamUserSyncJob>(
//     "iam-user-sync",
//     job => job.ExecuteAsync(CancellationToken.None),
//     Cron.Daily);

app.Run();

// ── Local functions ──────────────────────────────────────────────────────────

void ConfigureJwt(JwtBearerOptions options)
{
    if (iam.Provider.Equals(IdentityProviderOptions.Providers.Auth0, StringComparison.OrdinalIgnoreCase))
    {
        // Auth0 OIDC discovery lives at the standard path under the Authority.
        options.Authority            = auth0.Authority;
        options.Audience             = iam.Audience;
        options.RequireHttpsMetadata = true;
        options.MapInboundClaims     = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer   = auth0.Authority,
            ValidAudience = iam.Audience,
        };
    }
    else // keycloak (default)
    {
        if (string.IsNullOrWhiteSpace(keycloak.Authority))
            throw new InvalidOperationException("Keycloak:Authority is required");

        // Authority validates the `iss` claim in tokens (always the public URL).
        // ResolvedMetadataAddress differs in Docker (container hostname) vs hybrid dev (same as Authority).
        options.Authority            = keycloak.Authority;
        options.MetadataAddress      = keycloak.ResolvedMetadataAddress;
        options.Audience             = iam.Audience;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        // Preserve original claim names from the JWT (e.g. "sub", "realm_access")
        // Without this, ASP.NET Core remaps "sub" → ClaimTypes.NameIdentifier
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer   = keycloak.Authority,
            ValidAudience = iam.Audience,
        };
    }
}

IActionResult HandleInvalidModelState(ActionContext ctx)
{
    var logger = ctx.HttpContext.RequestServices
        .GetRequiredService<ILogger<Program>>();

    var errors = ctx.ModelState
        .Where(e => e.Value?.Errors.Count > 0)
        .ToDictionary(
            e => e.Key,
            e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

    logger.LogWarning(
        "Model binding failed for {Method} {Path}: {@Errors}",
        ctx.HttpContext.Request.Method,
        ctx.HttpContext.Request.Path,
        errors);

    var problem = new ValidationProblemDetails(errors)
    {
        Status   = StatusCodes.Status400BadRequest,
        Title    = "Validation failed",
        Instance = ctx.HttpContext.Request.Path
    };
    problem.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;

    return new BadRequestObjectResult(problem)
    {
        ContentTypes = { "application/problem+json" }
    };
}

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
