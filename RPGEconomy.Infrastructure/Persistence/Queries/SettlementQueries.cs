namespace RPGEconomy.Infrastructure.Persistence.Queries;

internal static class SettlementQueries
{
    public const string GetById = """
        SELECT id, world_id AS "worldId", name, population
        FROM settlements
        WHERE id = @Id
        """;

    public const string GetByWorldId = """
        SELECT id, world_id AS "worldId", name, population
        FROM settlements
        WHERE world_id = @WorldId
        """;

    public const string Insert = """
        INSERT INTO settlements (world_id, name, population)
        VALUES (@WorldId, @Name, @Population)
        RETURNING id;
        """;

    public const string Update = """
        UPDATE settlements
        SET name = @Name,
            population = @Population
        WHERE id = @Id
        """;

    public const string Delete = """
        DELETE FROM inventory_items
        WHERE warehouse_id IN (
            SELECT id
            FROM warehouses
            WHERE settlement_id = @Id
        );

        DELETE FROM market_offers
        WHERE market_id IN (
            SELECT id
            FROM markets
            WHERE settlement_id = @Id
        );

        DELETE FROM buildings
        WHERE settlement_id = @Id;

        DELETE FROM warehouses
        WHERE settlement_id = @Id;

        DELETE FROM markets
        WHERE settlement_id = @Id;

        DELETE FROM settlements
        WHERE id = @Id;
        """;
}

