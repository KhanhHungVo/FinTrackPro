using Tests.Common;

namespace FinTrackPro.Api.IntegrationTests;

[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<DatabaseFixture>
{
    // Marker class. Wires DatabaseFixture as a shared fixture for all integration test classes.
    // One Testcontainers SQL Server instance is created for the entire test run.
}
