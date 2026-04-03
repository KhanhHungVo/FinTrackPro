using System.Text;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

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
        // UseSetting injects config before AddInfrastructureServices runs, so Hangfire,
        // HealthChecks, and EF Core all receive the test DB connection string.
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("DatabaseProvider:Provider", "postgresql");

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
        await ResetDatabaseAsync();
    }

    /// <summary>
    /// Drops and recreates the test database via the postgres maintenance DB so the
    /// connection never targets a non-existent database (which Npgsql rejects with 3D000).
    /// </summary>
    private async Task ResetDatabaseAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(ConnectionString);
        var dbName = builder.Database
            ?? throw new InvalidOperationException("Connection string must include a database name.");
        builder.Database = "postgres";

        await using var conn = new NpgsqlConnection(builder.ConnectionString);
        await conn.OpenAsync();

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"""
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{dbName}' AND pid <> pg_backend_pid();
                """;
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"DROP DATABASE IF EXISTS \"{dbName}\";";
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"CREATE DATABASE \"{dbName}\";";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public new Task DisposeAsync() => Task.CompletedTask;
}
