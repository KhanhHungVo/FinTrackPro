using System.Text;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using FinTrackPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;

namespace Tests.Common;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace real DbContext with Testcontainers connection
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(_dbContainer.GetConnectionString()));

            // Replace ICurrentUserService with controllable fake
            var currentUserDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ICurrentUserService));
            if (currentUserDescriptor is not null)
                services.Remove(currentUserDescriptor);

            services.AddScoped<ICurrentUserService, FakeCurrentUserService>();

            // Replace IBinanceService with a stub that accepts all symbols
            var binanceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IBinanceService));
            if (binanceDescriptor is not null)
                services.Remove(binanceDescriptor);

            services.AddScoped<IBinanceService, FakeBinanceService>();

            // Replace Keycloak JWT auth with local symmetric-key JWT for tests
            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                options.Authority = null;
                options.RequireHttpsMetadata = false;
                options.Audience = AuthTokenFactory.TestAudience;
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
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
