using Respawn;

namespace Tests.Common;

public class DatabaseFixture : IAsyncLifetime
{
    public CustomWebApplicationFactory Factory { get; } = new();
    private Respawner _respawner = null!;

    public async Task InitializeAsync()
    {
        await Factory.InitializeAsync();

        _respawner = await Respawner.CreateAsync(
            Factory.ConnectionString,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,
            });
    }

    /// <summary>
    /// Resets all tables to a clean state. Call in each test's InitializeAsync.
    /// </summary>
    public async Task ResetAsync()
        => await _respawner.ResetAsync(Factory.ConnectionString);

    public async Task DisposeAsync()
        => await Factory.DisposeAsync();
}
