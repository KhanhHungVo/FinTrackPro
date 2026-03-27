using System.Text;
using FinTrackPro.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using FinTrackPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Tests.Common;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string ConnectionString { get; } = ResolveConnectionString();

    private static string ResolveConnectionString()
    {
        if (Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING") is { } cs)
            return cs;

        return new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "testsettings.json"))
            .Build()
            .GetConnectionString("DefaultConnection")!;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override connection string so Hangfire and the app read the test DB, not appsettings.json
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace real DbContext with the test PostgreSQL connection
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(ConnectionString));

            // Replace IBinanceService with a stub that accepts all symbols
            var binanceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IBinanceService));
            if (binanceDescriptor is not null)
                services.Remove(binanceDescriptor);

            services.AddScoped<IBinanceService, FakeBinanceService>();

            // Replace Keycloak JWT auth with local symmetric-key JWT for tests.
            // MapInboundClaims = false preserves the raw "iss" claim so UserContextMiddleware
            // can read it from ClaimsPrincipal.
            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                options.Authority = null;
                options.RequireHttpsMetadata = false;
                options.Audience = AuthTokenFactory.TestAudience;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = AuthTokenFactory.TestIssuer,
                    ValidateAudience = true,
                    ValidAudience = AuthTokenFactory.TestAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(AuthTokenFactory.TestSigningKey)),
                    ValidateLifetime = true,
                };
            });
        });
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString,
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public new Task DisposeAsync() => Task.CompletedTask;
}
