using RPGEconomy.Testing;

namespace RPGEconomy.API.IntegrationTests;

[CollectionDefinition(IntegrationTestCollection.Database)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
