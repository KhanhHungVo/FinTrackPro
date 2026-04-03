using Npgsql;
using Respawn;

namespace Tests.Common;

public class DatabaseFixture : IAsyncLifetime
{
    public CustomWebApplicationFactory Factory { get; } = new();
    private Respawner _respawner = null!;

    public async Task InitializeAsync()
    {
        await Factory.InitializeAsync();
        Factory.CreateClient(); // triggers host build → Program.cs runs MigrateAsync + SeedAsync

        await using var conn = new NpgsqlConnection(Factory.ConnectionString);
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
        });
    }

    /// <summary>
    /// Resets all tables to a clean state. Call in each test's InitializeAsync.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var conn = new NpgsqlConnection(Factory.ConnectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    public async Task DisposeAsync()
        => await Factory.DisposeAsync();
}
