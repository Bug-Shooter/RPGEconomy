namespace RPGEconomy.Infrastructure.Persistence.Queries;

internal static class SettlementQueries
{
    public const string GetById = """
        SELECT id, world_id AS "worldId", name
        FROM settlements
        WHERE id = @Id
        """;

    public const string GetByWorldId = """
        SELECT id, world_id AS "worldId", name
        FROM settlements
        WHERE world_id = @WorldId
        ORDER BY id
        """;

    public const string Insert = """
        INSERT INTO settlements (world_id, name)
        VALUES (@WorldId, @Name)
        RETURNING id;
        """;

    public const string Update = """
        UPDATE settlements
        SET name = @Name
        WHERE id = @Id
        """;

    public const string Delete = """
        DELETE FROM settlements
        WHERE id = @Id;
        """;
}
