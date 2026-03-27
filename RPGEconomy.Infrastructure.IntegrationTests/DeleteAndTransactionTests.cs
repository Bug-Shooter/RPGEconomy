using Dapper;
using FluentAssertions;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Infrastructure.Decorators;
using RPGEconomy.Infrastructure.Persistence;
using RPGEconomy.Infrastructure.Persistence.Repositories;
using RPGEconomy.Testing;

namespace RPGEconomy.Infrastructure.IntegrationTests;

[Collection(IntegrationTestCollection.Database)]
public class DeleteAndTransactionTests
{
    private readonly DatabaseFixture _fixture;

    public DeleteAndTransactionTests(DatabaseFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task SettlementRepository_Delete_Should_Remove_Dependent_Rows()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync();
        var settlementId = await seed.CreateSettlementAsync(worldId);
        var warehouseId = await seed.CreateWarehouseAsync(settlementId);
        await seed.AddInventoryItemAsync(warehouseId, 1, 3);
        var marketId = await seed.CreateMarketAsync(settlementId);
        await seed.AddMarketOfferAsync(marketId, 1, 10);
        var recipeId = await seed.CreateRecipeAsync("Bread", 1, [], [(1, 1m)]);
        await seed.CreateBuildingAsync(settlementId, recipeId);
        await seed.CreatePopulationGroupAsync(settlementId, "Peasants", 10, [(1, 0.2m)]);

        var repository = new SettlementRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));
        await repository.DeleteAsync(settlementId);

        (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM settlements WHERE id = @settlementId", new { settlementId })).Should().Be(0);
        (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM warehouses WHERE settlement_id = @settlementId", new { settlementId })).Should().Be(0);
        (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM markets WHERE settlement_id = @settlementId", new { settlementId })).Should().Be(0);
        (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM buildings WHERE settlement_id = @settlementId", new { settlementId })).Should().Be(0);
        (await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM population_groups WHERE settlement_id = @settlementId", new { settlementId })).Should().Be(0);
    }

    [Fact]
    public async Task WorldRepository_Delete_Should_Fail_When_Child_Settlements_Exist()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync();
        await seed.CreateSettlementAsync(worldId);

        var repository = new WorldRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));
        var action = async () => await repository.DeleteAsync(worldId);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task TransactionSimulationDecorator_Should_Roll_Back_Changes_When_Inner_Throws()
    {
        await PostgresTestDatabase.ResetAsync();
        var decorator = new TransactionSimulationDecorator(new FailingDatabaseWriter(PostgresTestDatabase.ConnectionString));

        var action = async () => await decorator.ExecuteAsync(new SimulationExecutionRequest(1, 1, 1));

        await action.Should().ThrowAsync<InvalidOperationException>();

        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var worldCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM worlds");
        worldCount.Should().Be(0);
    }

    private sealed class FailingDatabaseWriter : ISimulationExecutor
    {
        private readonly string _connectionString;

        public FailingDatabaseWriter(string connectionString)
            => _connectionString = connectionString;

        public async Task<Result<SimulationExecutionResult>> ExecuteAsync(SimulationExecutionRequest request, CancellationToken cancellationToken = default)
        {
            await using var connection = new Npgsql.NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync("INSERT INTO worlds (name, description, current_day) VALUES ('Tx world', 'Desc', 0)");
            throw new InvalidOperationException("boom");
        }
    }
}
