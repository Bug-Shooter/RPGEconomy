using Xunit;

namespace RPGEconomy.Testing;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private GlobalTestDatabaseLock? _databaseLock;

    public async ValueTask InitializeAsync()
    {
        _databaseLock = new GlobalTestDatabaseLock();
        await _databaseLock.AcquireAsync();
        await PostgresTestDatabase.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_databaseLock is not null)
        {
            await _databaseLock.DisposeAsync();
        }
    }
}
