using RPGEconomy.Testing;

namespace RPGEconomy.Infrastructure.IntegrationTests;

[CollectionDefinition(IntegrationTestCollection.Database)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
