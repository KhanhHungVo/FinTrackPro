using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Options;
using FinTrackPro.Domain.Repositories;
using FinTrackPro.Infrastructure.Auth;
using FinTrackPro.Infrastructure.Persistence.Seeders;
using FinTrackPro.Infrastructure.ExternalServices;
using FinTrackPro.Infrastructure.ExternalServices.ExchangeRate;
using FinTrackPro.Infrastructure.Http;
using FinTrackPro.Infrastructure.Identity;
using FinTrackPro.Infrastructure.Persistence;
using FinTrackPro.Infrastructure.Persistence.Interceptors;
using FinTrackPro.Infrastructure.Persistence.Repositories;
using FinTrackPro.Infrastructure.Services;
using FinTrackPro.Infrastructure.Stripe;
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
        var db        = ResolveDatabaseConfiguration(configuration);
        var ro        = configuration.GetSection(HttpResilienceOptions.SectionName).Get<HttpResilienceOptions>() ?? new();
        var binance   = configuration.GetSection(BinanceOptions.SectionName).Get<BinanceOptions>() ?? new();
        var fearGreed = configuration.GetSection(FearGreedOptions.SectionName).Get<FearGreedOptions>() ?? new();
        var cg        = configuration.GetSection(CoinGeckoOptions.SectionName).Get<CoinGeckoOptions>() ?? new();
        var iam       = configuration.GetSection(IdentityProviderOptions.SectionName).Get<IdentityProviderOptions>() ?? new();
        var er        = configuration.GetSection(ExchangeRateOptions.SectionName).Get<ExchangeRateOptions>() ?? new();
        var pg        = configuration.GetSection(PaymentGatewayOptions.SectionName).Get<PaymentGatewayOptions>() ?? new();
        var botToken  = configuration["Telegram:BotToken"];

        AddDatabase(services, db);
        AddHttpInfrastructure(services, configuration);
        AddIamServices(services, configuration, iam, ro);
        services.AddMemoryCache();
        AddExternalHttpClients(services, configuration, binance, fearGreed, cg, er, ro);
        AddTelegramBot(services, botToken);
        AddPaymentGateway(services, configuration, pg);
        services.AddScoped<INotificationService, NotificationService>();
        AddHangfireStorage(services, db);
        AddHealthMonitoring(services, db);

        return services;
    }

    private static void AddDatabase(IServiceCollection services, DatabaseConfiguration db)
    {
        services.AddSingleton<IClock, Services.SystemClock>();
        services.AddSingleton<AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());

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
    }

    private static void AddHttpInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HttpLoggingOptions>(configuration.GetSection(HttpLoggingOptions.SectionName));
        services.Configure<HttpResilienceOptions>(configuration.GetSection(HttpResilienceOptions.SectionName));
        services.AddTransient<LoggingDelegatingHandler>();
    }

    private static void AddIamServices(
        IServiceCollection services,
        IConfiguration configuration,
        IdentityProviderOptions iam,
        HttpResilienceOptions ro)
    {
        services.Configure<IdentityProviderOptions>(
            configuration.GetSection(IdentityProviderOptions.SectionName));

        if (iam.Provider.Equals(IdentityProviderOptions.Providers.Auth0, StringComparison.OrdinalIgnoreCase))
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

        services.AddHttpContextAccessor();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ICurrentUser, CurrentUserAccessor>();
    }

    private static void AddExternalHttpClients(
        IServiceCollection services,
        IConfiguration configuration,
        BinanceOptions binance,
        FearGreedOptions fearGreed,
        CoinGeckoOptions cg,
        ExchangeRateOptions er,
        HttpResilienceOptions ro)
    {
        services.Configure<BinanceOptions>(configuration.GetSection(BinanceOptions.SectionName));
        services.Configure<FearGreedOptions>(configuration.GetSection(FearGreedOptions.SectionName));
        services.Configure<CoinGeckoOptions>(configuration.GetSection(CoinGeckoOptions.SectionName));
        services.Configure<ExchangeRateOptions>(configuration.GetSection(ExchangeRateOptions.SectionName));

        services.AddHttpClient<IBinanceService, BinanceService>(client =>
            client.BaseAddress = new Uri(binance.BaseUrl))
            .AddHttpMessageHandler<LoggingDelegatingHandler>()
            .AddStandardResilienceHandler(o => ConfigureResilience(o, ro));

        services.AddHttpClient<IFearGreedService, FearGreedService>(client =>
            client.BaseAddress = new Uri(fearGreed.BaseUrl))
            .AddHttpMessageHandler<LoggingDelegatingHandler>()
            .AddStandardResilienceHandler(o => ConfigureResilience(o, ro));

        services.AddHttpClient<ICoinGeckoService, CoinGeckoService>(client =>
        {
            client.BaseAddress = new Uri(cg.BaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            if (!string.IsNullOrWhiteSpace(cg.ApiKey))
                client.DefaultRequestHeaders.Add("x-cg-demo-api-key", cg.ApiKey);
        })
        .AddHttpMessageHandler<LoggingDelegatingHandler>()
        .AddStandardResilienceHandler(o => ConfigureResilience(o, ro));

        services.AddHttpClient<IExchangeRateClient, ExchangeRateClient>(client =>
        {
            client.BaseAddress = new Uri(er.BaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            // API key is embedded in the URL path by ExchangeRateClient, not sent as a header
        })
        .AddHttpMessageHandler<LoggingDelegatingHandler>()
        .AddStandardResilienceHandler(o => ConfigureResilience(o, ro));

        services.AddScoped<IExchangeRateService, ExchangeRateService>();
    }

    private static void AddTelegramBot(IServiceCollection services, string? botToken)
    {
        if (string.IsNullOrWhiteSpace(botToken))
        {
            services.AddScoped<INotificationChannel, NullNotificationChannel>();
        }
        else
        {
            services.AddScoped<INotificationChannel, TelegramNotificationChannel>();
            services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(botToken));
        }
    }

    private static void AddHangfireStorage(IServiceCollection services, DatabaseConfiguration db)
    {
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
    }

    private static void AddHealthMonitoring(IServiceCollection services, DatabaseConfiguration db)
    {
        var hc = services.AddHealthChecks();
        if (db.IsPostgres)
            hc.AddNpgSql(db.ConnectionString, name: db.HealthCheckName, failureStatus: HealthStatus.Unhealthy);
        else
            hc.AddSqlServer(db.ConnectionString, name: db.HealthCheckName, failureStatus: HealthStatus.Unhealthy);
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

    private static void AddPaymentGateway(
        IServiceCollection services,
        IConfiguration configuration,
        PaymentGatewayOptions pg)
    {
        services.Configure<PaymentGatewayOptions>(configuration.GetSection(PaymentGatewayOptions.SectionName));
        services.Configure<SubscriptionPlanOptions>(configuration.GetSection(SubscriptionPlanOptions.SectionName));
        services.AddScoped<ISubscriptionLimitService, SubscriptionLimitService>();

        if (pg.Provider.Equals("stripe", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
            services.AddScoped<IPaymentGatewayService, StripePaymentGatewayService>();
            services.AddScoped<IPaymentWebhookHandler, StripeWebhookHandler>();
        }
        // Future: else if (pg.Provider.Equals("paddle", StringComparison.OrdinalIgnoreCase)) { ... }
    }

    private sealed record DatabaseConfiguration(bool IsPostgres, string ConnectionString)
    {
        public string HealthCheckName => IsPostgres ? "postgresql" : "sqlserver";
    }
}
