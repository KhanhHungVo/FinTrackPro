using FinTrackPro.API.Infrastructure;
using FinTrackPro.API.Middleware;
using FinTrackPro.Application;
using FinTrackPro.BackgroundJobs;
using FinTrackPro.BackgroundJobs.Jobs;
using FinTrackPro.Infrastructure.Auth;
using FinTrackPro.Infrastructure;
using FinTrackPro.Infrastructure.Identity;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// Application & Infrastructure
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// RFC 7807 Problem Details support (used by InvalidModelStateResponseFactory and ExceptionHandlingMiddleware)
builder.Services.AddProblemDetails();

// Controllers — API layer handles binding failures via InvalidModelStateResponseFactory (logged at [WRN]).
// Business-rule validation flows through ValidationBehavior → ExceptionHandlingMiddleware.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = ctx =>
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
                Status = StatusCodes.Status400BadRequest,
                Title  = "Validation failed",
                Instance = ctx.HttpContext.Request.Path
            };
            problem.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problem)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    })
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

// OpenAPI (built-in .NET 10)
builder.Services.AddOpenApi();

// Authentication — JWT Bearer (Keycloak or Auth0)
var iamProvider = builder.Configuration["IdentityProvider:Provider"] ?? "keycloak";
var audience    = builder.Configuration["IdentityProvider:Audience"]!;

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        if (iamProvider == "auth0")
        {
            var domain    = builder.Configuration["Auth0:Domain"];
            var authority = $"https://{domain}/";
            // Auth0 OIDC discovery lives at the standard path under the Authority.
            options.Authority            = authority;
            options.Audience             = audience;
            options.RequireHttpsMetadata = true;
            options.MapInboundClaims     = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer   = authority,
                ValidAudience = audience,
            };
        }
        else // keycloak (default)
        {
            var authority       = builder.Configuration["Keycloak:Authority"] ?? throw new InvalidOperationException("Keycloak:Authority is required");
            var metadataAddress = builder.Configuration["Keycloak:MetadataAddress"];
            // Authority validates the `iss` claim in tokens (always the public URL).
            // MetadataAddress is where the API fetches signing keys — differs in Docker
            // (uses container hostname) vs hybrid dev (same as Authority base URL).
            options.Authority            = authority;
            options.MetadataAddress      = metadataAddress ?? authority;
            options.Audience             = audience;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            // Preserve original claim names from the JWT (e.g. "sub", "realm_access")
            // Without this, ASP.NET Core remaps "sub" → ClaimTypes.NameIdentifier
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer   = authority,
                ValidAudience = audience,
            };
        }
    });

builder.Services.AddAuthorization();

// CORS — allow frontend SPA
builder.Services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(builder.Configuration["Cors:Origins"]?.Split(',') ?? ["http://localhost:5173"])
              .AllowAnyHeader()
              .AllowAnyMethod()));

// Background jobs
builder.Services.AddBackgroundJobServices();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowFrontend");

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

// Register recurring jobs
RecurringJob.AddOrUpdate<MarketSignalJob>(
    "market-signals",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 */4 * * *");   // every 4 hours

RecurringJob.AddOrUpdate<BudgetOverrunJob>(
    "budget-overrun",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Daily);

// RecurringJob.AddOrUpdate<IamUserSyncJob>(
//     "iam-user-sync",
//     job => job.ExecuteAsync(CancellationToken.None),
//     Cron.Daily);

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
