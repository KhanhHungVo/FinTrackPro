using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Repositories;
using FinTrackPro.Infrastructure.Auth;
using FinTrackPro.Infrastructure.ExternalServices;
using FinTrackPro.Infrastructure.Persistence;
using FinTrackPro.Infrastructure.Persistence.Repositories;
using FinTrackPro.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace FinTrackPro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(p =>
            p.GetRequiredService<ApplicationDbContext>());

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<ITradeRepository, TradeRepository>();
        services.AddScoped<IWatchedSymbolRepository, WatchedSymbolRepository>();
        services.AddScoped<ISignalRepository, SignalRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();

        // Auth — provider-conditional registration
        services.Configure<IdentityProviderOptions>(
            configuration.GetSection(IdentityProviderOptions.SectionName));

        var iamProvider = configuration["IdentityProvider:Provider"] ?? "keycloak";
        if (iamProvider == "auth0")
        {
            services.AddScoped<IClaimsTransformation, Auth0ClaimsTransformer>();
            services.AddHttpClient<IIamProviderService, Auth0ManagementService>();
        }
        else
        {
            services.AddScoped<IClaimsTransformation, KeycloakClaimsTransformer>();
            services.AddHttpClient<IIamProviderService, KeycloakAdminService>();
        }

        // Infrastructure services
        services.AddHttpContextAccessor();
        services.AddTransient<ICurrentUserService, CurrentUserService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationChannel, TelegramNotificationChannel>();

        // Telegram Bot
        services.AddSingleton<ITelegramBotClient>(sp =>
            new TelegramBotClient(configuration["Telegram:BotToken"]!));

        // Memory cache
        services.AddMemoryCache();

        // HTTP clients for external APIs
        services.AddHttpClient<IBinanceService, BinanceService>(client =>
            client.BaseAddress = new Uri("https://api.binance.com"));

        services.AddHttpClient<IFearGreedService, FearGreedService>(client =>
            client.BaseAddress = new Uri("https://api.alternative.me"));

        services.AddHttpClient<ICoinGeckoService, CoinGeckoService>(client =>
        {
            client.BaseAddress = new Uri("https://api.coingecko.com");
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var apiKey = configuration["CoinGecko:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
                client.DefaultRequestHeaders.Add("x-cg-demo-api-key", apiKey);
        });

        return services;
    }
}
