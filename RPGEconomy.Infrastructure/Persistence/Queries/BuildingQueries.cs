namespace RPGEconomy.Infrastructure.Persistence.Queries;

internal static class BuildingQueries
{
    public const string GetById = """
        SELECT id, name, settlement_id AS "settlementId", recipe_id AS "recipeId", worker_count AS "workerCount", is_active AS "isActive"
        FROM buildings
        WHERE id = @Id
        """;

    public const string GetBySettlementId = """
        SELECT id, name, settlement_id AS "settlementId", recipe_id AS "recipeId", worker_count AS "workerCount", is_active AS "isActive"
        FROM buildings
        WHERE settlement_id = @SettlementId
        ORDER BY id
        """;

    public const string Insert = """
        INSERT INTO buildings (name, settlement_id, recipe_id, worker_count, is_active)
        VALUES (@Name, @SettlementId, @RecipeId, @WorkerCount, @IsActive)
        RETURNING id;
        """;

    public const string Update = """
        UPDATE buildings
        SET name         = @Name,
            worker_count = @WorkerCount,
            is_active    = @IsActive
        WHERE id = @Id
        """;

    public const string Delete = """
        DELETE FROM buildings WHERE id = @Id
        """;
}

