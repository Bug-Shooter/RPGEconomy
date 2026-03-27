using Dapper;
using Npgsql;

namespace RPGEconomy.Testing;

public sealed class TestDataSeeder
{
    private readonly NpgsqlConnection _connection;

    public TestDataSeeder(NpgsqlConnection connection)
        => _connection = connection;

    public Task<int> CreateWorldAsync(
        string name = "World",
        string description = "Description",
        int currentDay = 0) =>
        _connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO worlds (name, description, current_day)
            VALUES (@name, @description, @currentDay)
            RETURNING id;
            """,
            new { name, description, currentDay });

    public Task<int> CreateSettlementAsync(
        int worldId,
        string name = "Settlement",
        int population = 1000) =>
        _connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO settlements (world_id, name, population)
            VALUES (@worldId, @name, @population)
            RETURNING id;
            """,
            new { worldId, name, population });

    public Task<int> CreateWarehouseAsync(int settlementId) =>
        _connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO warehouses (settlement_id)
            VALUES (@settlementId)
            RETURNING id;
            """,
            new { settlementId });

    public Task<int> AddInventoryItemAsync(
        int warehouseId,
        int productTypeId,
        decimal quantity,
        string quality = "Normal") =>
        _connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO inventory_items (warehouse_id, product_type_id, quantity, quality)
            VALUES (@warehouseId, @productTypeId, @quantity, @quality)
            RETURNING id;
            """,
            new { warehouseId, productTypeId, quantity, quality });

    public Task<int> CreateMarketAsync(int settlementId) =>
        _connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO markets (settlement_id)
            VALUES (@settlementId)
            RETURNING id;
            """,
            new { settlementId });

    public Task<int> AddMarketOfferAsync(
        int marketId,
        int productTypeId,
        decimal currentPrice,
        decimal supplyVolume = 0,
        decimal demandVolume = 0) =>
        _connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO market_offers (market_id, product_type_id, current_price, supply_volume, demand_volume)
            VALUES (@marketId, @productTypeId, @currentPrice, @supplyVolume, @demandVolume)
            RETURNING id;
            """,
            new { marketId, productTypeId, currentPrice, supplyVolume, demandVolume });

    public Task<int> CreateProductTypeAsync(
        string name = "Product",
        string description = "Description",
        decimal basePrice = 10,
        double weightPerUnit = 1) =>
        _connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO product_types (name, description, base_price, weight_per_unit)
            VALUES (@name, @description, @basePrice, @weightPerUnit)
            RETURNING id;
            """,
            new { name, description, basePrice, weightPerUnit });

    public async Task<int> CreateRecipeAsync(
        string name,
        double laborDaysRequired,
        IEnumerable<(int ProductTypeId, decimal Quantity)> inputs,
        IEnumerable<(int ProductTypeId, decimal Quantity)> outputs)
    {
        var recipeId = await _connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO production_recipes (name, labor_days_required)
            VALUES (@name, @laborDaysRequired)
            RETURNING id;
            """,
            new { name, laborDaysRequired });

        foreach (var input in inputs)
        {
            await _connection.ExecuteAsync(
                """
                INSERT INTO recipe_ingredients (recipe_id, product_type_id, quantity, is_input)
                VALUES (@recipeId, @productTypeId, @quantity, TRUE)
                """,
                new { recipeId, productTypeId = input.ProductTypeId, quantity = input.Quantity });
        }

        foreach (var output in outputs)
        {
            await _connection.ExecuteAsync(
                """
                INSERT INTO recipe_ingredients (recipe_id, product_type_id, quantity, is_input)
                VALUES (@recipeId, @productTypeId, @quantity, FALSE)
                """,
                new { recipeId, productTypeId = output.ProductTypeId, quantity = output.Quantity });
        }

        return recipeId;
    }

    public Task<int> CreateBuildingAsync(
        int settlementId,
        int recipeId,
        string name = "Building",
        int workerCount = 1,
        bool isActive = true) =>
        _connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO buildings (name, settlement_id, recipe_id, worker_count, is_active)
            VALUES (@name, @settlementId, @recipeId, @workerCount, @isActive)
            RETURNING id;
            """,
            new { name, settlementId, recipeId, workerCount, isActive });

    public async Task<int> CreatePopulationGroupAsync(
        int settlementId,
        string name,
        int populationSize,
        IEnumerable<(int ProductTypeId, decimal AmountPerPersonPerTick)> consumptionProfile)
    {
        var populationGroupId = await _connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO population_groups (settlement_id, name, population_size)
            VALUES (@settlementId, @name, @populationSize)
            RETURNING id;
            """,
            new { settlementId, name, populationSize });

        foreach (var item in consumptionProfile)
        {
            await _connection.ExecuteAsync(
                """
                INSERT INTO population_group_consumption
                    (population_group_id, product_type_id, amount_per_person_per_tick)
                VALUES
                    (@populationGroupId, @productTypeId, @amountPerPersonPerTick)
                """,
                new
                {
                    populationGroupId,
                    productTypeId = item.ProductTypeId,
                    amountPerPersonPerTick = item.AmountPerPersonPerTick
                });
        }

        return populationGroupId;
    }
}
