namespace Tests.Common;

[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<DatabaseFixture>
{
    // Marker class. Ensures DatabaseFixture is created once and shared
    // across all test classes decorated with [Collection(nameof(IntegrationTestCollection))].
    // One PostgreSQL DatabaseFixture is shared across all integration test classes.
}
