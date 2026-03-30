namespace RPGEconomy.Infrastructure.Persistence.Queries;

internal static class BuildingQueries
{
    public const string GetById = """
        SELECT id,
               name,
               settlement_id AS "settlementId",
               recipe_id AS "recipeId",
               worker_count AS "workerCount",
               is_active AS "isActive",
               input_reserve_coverage_ticks AS "inputReserveCoverageTicks"
        FROM buildings
        WHERE id = @Id
        """;

    public const string GetBySettlementId = """
        SELECT id,
               name,
               settlement_id AS "settlementId",
               recipe_id AS "recipeId",
               worker_count AS "workerCount",
               is_active AS "isActive",
               input_reserve_coverage_ticks AS "inputReserveCoverageTicks"
        FROM buildings
        WHERE settlement_id = @SettlementId
        ORDER BY id
        """;

    public const string Insert = """
        INSERT INTO buildings (name, settlement_id, recipe_id, worker_count, is_active, input_reserve_coverage_ticks)
        VALUES (@Name, @SettlementId, @RecipeId, @WorkerCount, @IsActive, @InputReserveCoverageTicks)
        RETURNING id;
        """;

    public const string Update = """
        UPDATE buildings
        SET name         = @Name,
            worker_count = @WorkerCount,
            is_active    = @IsActive,
            input_reserve_coverage_ticks = @InputReserveCoverageTicks
        WHERE id = @Id
        """;

    public const string GetInputReserves = """
        SELECT id,
               building_id AS "buildingId",
               product_type_id AS "productTypeId",
               quantity
        FROM building_input_reserves
        WHERE building_id = @BuildingId
        ORDER BY id
        """;

    public const string DeleteInputReserves = """
        DELETE FROM building_input_reserves
        WHERE building_id = @BuildingId
        """;

    public const string InsertInputReserve = """
        INSERT INTO building_input_reserves (building_id, product_type_id, quantity)
        VALUES (@BuildingId, @ProductTypeId, @Quantity)
        """;

    public const string Delete = """
        DELETE FROM buildings WHERE id = @Id
        """;
}

