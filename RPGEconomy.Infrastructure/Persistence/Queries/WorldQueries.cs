namespace RPGEconomy.Infrastructure.Persistence.Queries;

internal static class WorldQueries
{
    public const string GetById = """
        SELECT id, name, description, current_day AS "currentDay"
        FROM worlds
        WHERE id = @Id
        """;

    public const string GetAll = """
        SELECT id, name, description, current_day AS "currentDay"
        FROM worlds
        """;

    public const string Insert = """
    INSERT INTO worlds (name, description, current_day)
    VALUES (@Name, @Description, @CurrentDay)
    RETURNING id;
    """;

    public const string Update = """
        UPDATE worlds
        SET name = @Name,
            description = @Description,
            current_day = @CurrentDay
        WHERE id = @Id
        """;

    public const string Delete = """
        DELETE FROM worlds WHERE id = @Id
        """;
}
