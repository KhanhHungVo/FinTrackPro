using Tests.Common;

namespace FinTrackPro.Api.IntegrationTests;

[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<DatabaseFixture>
{
    // Marker class. Wires DatabaseFixture as a shared fixture for all integration test classes.
    // One PostgreSQL DatabaseFixture is shared across the entire test run.
}
