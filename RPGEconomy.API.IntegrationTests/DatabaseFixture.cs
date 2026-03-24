using RPGEconomy.Testing;

namespace RPGEconomy.API.IntegrationTests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    public PostgresTestDatabase Database { get; } = new();

    public ValueTask InitializeAsync() =>
        new(PostgresTestDatabase.InitializeAsync());

    public ValueTask DisposeAsync() =>
        new(Task.CompletedTask);
}
