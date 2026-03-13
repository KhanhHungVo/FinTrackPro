using FinTrackPro.API.Infrastructure;
using FinTrackPro.API.Middleware;
using FinTrackPro.Application;
using FinTrackPro.BackgroundJobs.Jobs;
using FinTrackPro.Domain.Constants;
using FinTrackPro.Infrastructure;
using Hangfire;
using Hangfire.SqlServer;
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

// Controllers
builder.Services.AddControllers();

// OpenAPI (built-in .NET 10)
builder.Services.AddOpenApi();

// Authentication — Keycloak JWT Bearer
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience  = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        // Preserve original claim names from the JWT (e.g. "sub", "realm_access")
        // Without this, ASP.NET Core remaps "sub" → ClaimTypes.NameIdentifier
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorization();

// CORS — allow frontend SPA
builder.Services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(builder.Configuration["Cors:Origins"]?.Split(',') ?? ["http://localhost:5173"])
              .AllowAnyHeader()
              .AllowAnyMethod()));

// Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
builder.Services.AddHangfireServer();

// Background job classes
builder.Services.AddScoped<MarketSignalJob>();
builder.Services.AddScoped<BudgetOverrunJob>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();            // /openapi/v1.json
    app.MapScalarApiReference(); // Scalar UI at /scalar
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Hangfire Dashboard — restricted to Admin role
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireRoleFilter(UserRole.Admin)]
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

app.Run();
