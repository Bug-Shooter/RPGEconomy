using Dapper;
using FluentAssertions;
using RPGEconomy.Domain.Events;
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
        var firstProductTypeId = await seed.CreateProductTypeAsync("Wheat", "Raw", 2m, 1);
        var secondProductTypeId = await seed.CreateProductTypeAsync("Bread", "Food", 10m, 1);

        var repository = new WarehouseRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));
        var warehouseId = await repository.SaveAsync(Warehouse.Create(settlementId));
        var warehouse = await repository.GetByIdAsync(warehouseId);
        warehouse.Should().NotBeNull();
        warehouse!.AddItem(firstProductTypeId, 5, QualityGrade.Normal);
        await repository.SaveAsync(warehouse);

        warehouse = await repository.GetByIdAsync(warehouseId);
        warehouse!.Withdraw(firstProductTypeId, 5, QualityGrade.Normal);
        warehouse.AddItem(secondProductTypeId, 3.5m, QualityGrade.Normal);
        await repository.SaveAsync(warehouse);

        var reloaded = await repository.GetByIdAsync(warehouseId);
        reloaded!.Items.Should().ContainSingle(x => x.ProductTypeId == secondProductTypeId && x.Quantity == 3.5m);

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
        var breadId = await seed.CreateProductTypeAsync("Bread", "Food", 10m, 1);
        var aleId = await seed.CreateProductTypeAsync("Ale", "Drink", 7m, 1);

        var repository = new MarketRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));
        var marketId = await repository.SaveAsync(Market.Create(settlementId));
        var market = await repository.GetByIdAsync(marketId);
        market!.RegisterProduct(breadId, 10m);
        await repository.SaveAsync(market);

        market = await repository.GetByIdAsync(marketId);
        market!.UpdateProductState(breadId, 2, 5);
        market.RegisterProduct(aleId, 15m);
        await repository.SaveAsync(market);

        var reloaded = await repository.GetByIdAsync(marketId);
        reloaded!.Offers.Should().HaveCount(2);
        reloaded.Offers.Should().Contain(x => x.ProductTypeId == breadId && x.CurrentPrice > 10m);
        var offerCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM market_offers WHERE market_id = @marketId", new { marketId });
        offerCount.Should().Be(2);
    }

    [Fact]
    public async Task ProductionRecipeRepository_Should_Load_Inputs_And_Outputs_Correctly()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var grainId = await seed.CreateProductTypeAsync("Grain", "Raw", 2m, 1);
        var breadId = await seed.CreateProductTypeAsync("Bread", "Food", 10m, 1);
        var repository = new ProductionRecipeRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));
        var recipe = ProductionRecipe.Create(
            "Bread",
            1.5,
            [new RecipeIngredient(grainId, 2.5m)],
            [new RecipeIngredient(breadId, 1.25m)]).Value!;

        var recipeId = await repository.SaveAsync(recipe);
        var stored = await repository.GetByIdAsync(recipeId);

        stored.Should().NotBeNull();
        stored!.Inputs.Should().ContainSingle(x => x.ProductTypeId == grainId && x.Quantity == 2.5m);
        stored.Outputs.Should().ContainSingle(x => x.ProductTypeId == breadId && x.Quantity == 1.25m);
    }

    [Fact]
    public async Task PopulationGroupRepository_Should_Roundtrip_Profile_And_Decimal_Consumption()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync();
        var settlementId = await seed.CreateSettlementAsync(worldId);
        var breadId = await seed.CreateProductTypeAsync("Bread", "Food", 10m, 1);
        var fishId = await seed.CreateProductTypeAsync("Fish", "Food", 8m, 1);

        var repository = new PopulationGroupRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));
        var group = PopulationGroup.Create(
            settlementId,
            "Peasants",
            50,
            [(breadId, 0.05m), (fishId, 1.2m)]).Value!;

        var id = await repository.SaveAsync(group);
        var stored = await repository.GetByIdAsync(id);

        stored.Should().NotBeNull();
        stored!.PopulationSize.Should().Be(50);
        stored.ConsumptionProfile.Should().ContainSingle(x => x.ProductTypeId == breadId && x.AmountPerPersonPerTick == 0.05m);
        stored.ConsumptionProfile.Should().ContainSingle(x => x.ProductTypeId == fishId && x.AmountPerPersonPerTick == 1.2m);
    }

    [Fact]
    public async Task PopulationGroupRepository_Should_Roundtrip_Reserve_Settings_And_Stocks()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync();
        var settlementId = await seed.CreateSettlementAsync(worldId);
        var productTypeId = await seed.CreateProductTypeAsync("Bread", "Food", 10m, 1);

        var repository = new PopulationGroupRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));
        var group = PopulationGroup.Create(
            settlementId,
            "Peasants",
            50,
            3m,
            [(productTypeId, 0.05m)]).Value!;
        group.ReceiveReserveStock(productTypeId, 2.5m);

        var id = await repository.SaveAsync(group);
        var stored = await repository.GetByIdAsync(id);

        stored.Should().NotBeNull();
        stored!.ReserveCoverageTicks.Should().Be(3m);
        stored.StockItems.Should().ContainSingle(x => x.ProductTypeId == productTypeId && x.Quantity == 2.5m);
    }

    [Fact]
    public async Task BuildingRepository_Should_Roundtrip_Input_Reserve_Settings_And_Stocks()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync();
        var settlementId = await seed.CreateSettlementAsync(worldId);
        var inputProductTypeId = await seed.CreateProductTypeAsync("Flour", "Input", 5m, 1);
        var outputProductTypeId = await seed.CreateProductTypeAsync("Bread", "Food", 10m, 1);
        var recipeId = await seed.CreateRecipeAsync("Bread", 1, [(inputProductTypeId, 1m)], [(outputProductTypeId, 1m)]);

        var repository = new BuildingRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));
        var createResult = Building.Create("Bakery", settlementId, recipeId, 4, 2m);
        createResult.IsSuccess.Should().BeTrue();
        var building = createResult.Value!;
        building.ReceiveInputReserve(inputProductTypeId, 3m);

        var id = await repository.SaveAsync(building);
        var stored = await repository.GetByIdAsync(id);

        stored.Should().NotBeNull();
        stored!.InputReserveCoverageTicks.Should().Be(2m);
        stored.InputReserveItems.Should().ContainSingle(x => x.ProductTypeId == inputProductTypeId && x.Quantity == 3m);
    }

    [Fact]
    public async Task EconomicEventRepository_Should_Roundtrip_Effects()
    {
        await PostgresTestDatabase.ResetAsync();
        await using var connection = await PostgresTestDatabase.OpenConnectionAsync();
        var seed = new TestDataSeeder(connection);
        var worldId = await seed.CreateWorldAsync();
        var settlementId = await seed.CreateSettlementAsync(worldId);
        var productTypeId = await seed.CreateProductTypeAsync("Bread", "Food", 10m, 1);

        var repository = new EconomicEventRepository(new NpgsqlConnectionFactory(PostgresTestDatabase.ConnectionString));
        var economicEvent = EconomicEvent.Create(
            settlementId,
            "Uncertainty",
            true,
            0,
            5,
            [(EconomicEffectType.DesiredReserveCoverageMultiplier, 2m, null, productTypeId)]).Value!;

        var id = await repository.SaveAsync(economicEvent);
        var stored = await repository.GetByIdAsync(id);

        stored.Should().NotBeNull();
        stored!.Effects.Should().ContainSingle(x =>
            x.EffectType == EconomicEffectType.DesiredReserveCoverageMultiplier &&
            x.Value == 2m &&
            x.ProductTypeId == productTypeId);
    }
}
