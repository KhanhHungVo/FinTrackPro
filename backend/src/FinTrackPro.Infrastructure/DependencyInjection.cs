using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Repositories;
using FinTrackPro.Infrastructure.Auth;
using FinTrackPro.Infrastructure.Persistence.Seeders;
using FinTrackPro.Infrastructure.ExternalServices;
using FinTrackPro.Infrastructure.ExternalServices.ExchangeRate;
using FinTrackPro.Infrastructure.Http;
using FinTrackPro.Infrastructure.Identity;
using FinTrackPro.Infrastructure.Persistence;
using FinTrackPro.Infrastructure.Persistence.Repositories;
using FinTrackPro.Infrastructure.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Npgsql;
using Polly;
using Telegram.Bot;

namespace FinTrackPro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var db = ResolveDatabaseConfiguration(configuration);

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (db.IsPostgres)
                options.UseNpgsql(
                    db.ConnectionString,
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            else
                options.UseSqlServer(
                    db.ConnectionString,
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
        });

        services.AddScoped<IApplicationDbContext>(p =>
            p.GetRequiredService<ApplicationDbContext>());

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserIdentityRepository, UserIdentityRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<ITradeRepository, TradeRepository>();
        services.AddScoped<IWatchedSymbolRepository, WatchedSymbolRepository>();
        services.AddScoped<ISignalRepository, SignalRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<ITransactionCategoryRepository, TransactionCategoryRepository>();
        services.AddScoped<IDataSeeder, TransactionCategoryDataSeeder>();

        // HTTP logging + resilience options
        services.Configure<HttpLoggingOptions>(configuration.GetSection(HttpLoggingOptions.SectionName));
        services.Configure<HttpResilienceOptions>(configuration.GetSection(HttpResilienceOptions.SectionName));
        services.AddTransient<LoggingDelegatingHandler>();

        var ro = configuration.GetSection(HttpResilienceOptions.SectionName)
                               .Get<HttpResilienceOptions>() ?? new HttpResilienceOptions();
        var binanceBaseUrl = configuration["ExternalApis:BinanceBaseUrl"] ?? "https://api.binance.com";
        var fearGreedBaseUrl = configuration["ExternalApis:FearGreedBaseUrl"] ?? "https://api.alternative.me";
        var coinGeckoBaseUrl = configuration["ExternalApis:CoinGeckoBaseUrl"] ?? "https://api.coingecko.com";

        // Auth — provider-conditional registration
        services.Configure<IdentityProviderOptions>(
            configuration.GetSection(IdentityProviderOptions.SectionName));

        var iamProvider = configuration["IdentityProvider:Provider"] ?? "keycloak";
        if (iamProvider.Equals("auth0", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IClaimsTransformation, Auth0ClaimsTransformer>();
            services.AddHttpClient<IIamProviderService, Auth0ManagementService>()
                    .AddHttpMessageHandler<LoggingDelegatingHandler>()
                    .AddStandardResilienceHandler(o => ConfigureResilience(o, ro));
        }
        else
        {
            services.AddScoped<IClaimsTransformation, KeycloakClaimsTransformer>();
            services.AddHttpClient<IIamProviderService, KeycloakAdminService>()
                    .AddHttpMessageHandler<LoggingDelegatingHandler>()
                    .AddStandardResilienceHandler(o => ConfigureResilience(o, ro));
        }

        // Identity services
        services.AddHttpContextAccessor();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ICurrentUser, CurrentUserAccessor>();

        // Infrastructure services
        services.AddScoped<INotificationService, NotificationService>();

        // Telegram Bot
        var botToken = configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
        {
            services.AddScoped<INotificationChannel, NullNotificationChannel>();
        }
        else
        {
            services.AddScoped<INotificationChannel, TelegramNotificationChannel>();
            services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(botToken));
        }

        // Memory cache
        services.AddMemoryCache();

        // HTTP clients for external APIs
        services.AddHttpClient<IBinanceService, BinanceService>(client =>
            client.BaseAddress = new Uri(binanceBaseUrl))
            .AddHttpMessageHandler<LoggingDelegatingHandler>()
            .AddStandardResilienceHandler(o => ConfigureResilience(o, ro));

        services.AddHttpClient<IFearGreedService, FearGreedService>(client =>
            client.BaseAddress = new Uri(fearGreedBaseUrl))
            .AddHttpMessageHandler<LoggingDelegatingHandler>()
            .AddStandardResilienceHandler(o => ConfigureResilience(o, ro));

        services.AddHttpClient<ICoinGeckoService, CoinGeckoService>(client =>
        {
            client.BaseAddress = new Uri(coinGeckoBaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var apiKey = configuration["CoinGecko:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
                client.DefaultRequestHeaders.Add("x-cg-demo-api-key", apiKey);
        })
        .AddHttpMessageHandler<LoggingDelegatingHandler>()
        .AddStandardResilienceHandler(o => ConfigureResilience(o, ro));

        services.Configure<ExchangeRateOptions>(
            configuration.GetSection(ExchangeRateOptions.SectionName));

        services.AddHttpClient<IExchangeRateClient, ExchangeRateClient>(client =>
        {
            var baseUrl = configuration["ExchangeRate:BaseUrl"];
            client.BaseAddress = new Uri(baseUrl!);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            // API key is embedded in the URL path by ExchangeRateClient, not sent as a header
        })
        .AddHttpMessageHandler<LoggingDelegatingHandler>()
        .AddStandardResilienceHandler(o => ConfigureResilience(o, ro));

        services.AddScoped<IExchangeRateService, ExchangeRateService>();

        // Hangfire — storage selection mirrors the EF Core db-provider check above
        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            if (db.IsPostgres)
                config.UsePostgreSqlStorage(c =>
                    c.UseNpgsqlConnection(db.ConnectionString));
            else
                config.UseSqlServerStorage(
                    db.ConnectionString,
                    new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true
                    });
        });
        services.AddHangfireServer();

        var hc = services.AddHealthChecks();
        if (db.IsPostgres)
            hc.AddNpgSql(db.ConnectionString, name: "postgresql", failureStatus: HealthStatus.Unhealthy);
        else
            hc.AddSqlServer(db.ConnectionString, name: "sqlserver", failureStatus: HealthStatus.Unhealthy);

        return services;
    }

    private const int DefaultPostgresPort = 5432;

    private static DatabaseConfiguration ResolveDatabaseConfiguration(IConfiguration configuration)
    {
        var provider = configuration["DatabaseProvider:Provider"] ?? throw new InvalidOperationException("DatabaseProvider:Provider is required.");
        var rawConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
        var isPostgres = provider.Equals("postgresql", StringComparison.OrdinalIgnoreCase);
        var connectionString = isPostgres
            ? NormalizePostgresConnectionString(rawConnectionString)
            : rawConnectionString;

        return new DatabaseConfiguration(isPostgres, connectionString);
    }

    /// <summary>
    /// Accepts either a Npgsql key-value connection string or a postgres:// / postgresql:// URL
    /// (as issued by Render, Supabase, etc.) and always returns a Npgsql key-value string.
    /// </summary>
    private static string NormalizePostgresConnectionString(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        var uri = new Uri(connectionString);

        // Split on the first ':' only so passwords that contain ':' are preserved correctly.
        var sep = uri.UserInfo.IndexOf(':');
        var username = Uri.UnescapeDataString(sep < 0 ? uri.UserInfo : uri.UserInfo[..sep]);
        var password = sep < 0 ? string.Empty : Uri.UnescapeDataString(uri.UserInfo[(sep + 1)..]);

        // Honour ?sslmode= if present in the URL; default to Require for cloud providers.
        var query = QueryHelpers.ParseQuery(uri.Query);
        var sslMode = query.TryGetValue("sslmode", out var rawSslMode) &&
                      Enum.TryParse<SslMode>(rawSslMode.ToString(), ignoreCase: true, out var parsed)
            ? parsed
            : SslMode.Require;

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port == -1 ? DefaultPostgresPort : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = username,
            Password = password,
            SslMode = sslMode,
        }.ToString();
    }

    private static void ConfigureResilience(HttpStandardResilienceOptions o, HttpResilienceOptions ro)
    {
        o.Retry.MaxRetryAttempts = ro.RetryCount;
        o.Retry.Delay = TimeSpan.FromMilliseconds(ro.RetryBaseDelayMs);
        o.Retry.BackoffType = DelayBackoffType.Exponential;
        o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(ro.TimeoutSeconds);
        o.CircuitBreaker.FailureRatio = ro.CircuitBreakerFailurePercent / 100.0;
        o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(ro.CircuitBreakerBreakDurationSeconds);
        o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(ro.CircuitBreakerSamplingDurationSeconds);
        o.CircuitBreaker.MinimumThroughput = ro.CircuitBreakerMinimumThroughput;
    }

    private sealed record DatabaseConfiguration(bool IsPostgres, string ConnectionString);
}
