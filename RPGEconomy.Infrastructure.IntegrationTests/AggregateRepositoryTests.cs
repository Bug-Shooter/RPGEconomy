using Dapper;
using FluentAssertions;
using RPGEconomy.Domain.Markets;
using RPGEconomy.Domain.Population;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.Resources;
using RPGEconomy.Domain.World;
using RPGEconomy.Infrastructure.Persistence;
using RPGEconomy.Infrastructure.Persistence.Repositories;
using RPGEconomy.Testing;

namespace RPGEconomy.Infrastructure.IntegrationTests;

[Collection(IntegrationTestCollection.Database)]
public class AggregateRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public AggregateRepositoryTests(DatabaseFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task WarehouseRepository_Should_Replace_Child_Items_On_Save()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync();
        var settlementId = await seed.CreateSettlementAsync(worldId);

        var repository = new WarehouseRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));
        var warehouseId = await repository.SaveAsync(Warehouse.Create(settlementId));
        var warehouse = await repository.GetByIdAsync(warehouseId);
        warehouse.Should().NotBeNull();
        warehouse!.AddItem(1, 5, QualityGrade.Normal);
        await repository.SaveAsync(warehouse);

        warehouse = await repository.GetByIdAsync(warehouseId);
        warehouse!.Withdraw(1, 5, QualityGrade.Normal);
        warehouse.AddItem(2, 3.5m, QualityGrade.Normal);
        await repository.SaveAsync(warehouse);

        var reloaded = await repository.GetByIdAsync(warehouseId);
        reloaded!.Items.Should().ContainSingle(x => x.ProductTypeId == 2 && x.Quantity == 3.5m);

        var itemCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM inventory_items WHERE warehouse_id = @warehouseId", new { warehouseId });
        itemCount.Should().Be(1);
    }

    [Fact]
    public async Task MarketRepository_Should_Replace_Offers_On_Save()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync();
        var settlementId = await seed.CreateSettlementAsync(worldId);

        var repository = new MarketRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));
        var marketId = await repository.SaveAsync(Market.Create(settlementId));
        var market = await repository.GetByIdAsync(marketId);
        market!.RegisterProduct(1, 10m);
        await repository.SaveAsync(market);

        market = await repository.GetByIdAsync(marketId);
        market!.UpdateProductState(1, 2, 5);
        market.RegisterProduct(2, 15m);
        await repository.SaveAsync(market);

        var reloaded = await repository.GetByIdAsync(marketId);
        reloaded!.Offers.Should().HaveCount(2);
        reloaded.Offers.Should().Contain(x => x.ProductTypeId == 1 && x.CurrentPrice > 10m);
        var offerCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM market_offers WHERE market_id = @marketId", new { marketId });
        offerCount.Should().Be(2);
    }

    [Fact]
    public async Task ProductionRecipeRepository_Should_Load_Inputs_And_Outputs_Correctly()
    {
        await PostgresTestDatabase.ResetAsync();
        var repository = new ProductionRecipeRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));
        var recipe = ProductionRecipe.Create(
            "Bread",
            1.5,
            [new RecipeIngredient(1, 2.5m)],
            [new RecipeIngredient(2, 1.25m)]);

        var recipeId = await repository.SaveAsync(recipe);
        var stored = await repository.GetByIdAsync(recipeId);

        stored.Should().NotBeNull();
        stored!.Inputs.Should().ContainSingle(x => x.ProductTypeId == 1 && x.Quantity == 2.5m);
        stored.Outputs.Should().ContainSingle(x => x.ProductTypeId == 2 && x.Quantity == 1.25m);
    }

    [Fact]
    public async Task PopulationGroupRepository_Should_Roundtrip_Profile_And_Decimal_Consumption()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync();
        var settlementId = await seed.CreateSettlementAsync(worldId);

        var repository = new PopulationGroupRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));
        var group = PopulationGroup.Create(
            settlementId,
            "Peasants",
            50,
            [(1, 0.05m), (2, 1.2m)]).Value!;

        var id = await repository.SaveAsync(group);
        var stored = await repository.GetByIdAsync(id);

        stored.Should().NotBeNull();
        stored!.PopulationSize.Should().Be(50);
        stored.ConsumptionProfile.Should().ContainSingle(x => x.ProductTypeId == 1 && x.AmountPerPersonPerTick == 0.05m);
        stored.ConsumptionProfile.Should().ContainSingle(x => x.ProductTypeId == 2 && x.AmountPerPersonPerTick == 1.2m);
    }
}
