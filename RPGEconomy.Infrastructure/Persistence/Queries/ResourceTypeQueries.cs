namespace RPGEconomy.Infrastructure.Persistence.Queries;

internal static class ResourceTypeQueries
{
    public const string GetById = """
        SELECT id, name, description, is_renewable AS "isRenewable", regeneration_rate_per_day AS "regenerationRatePerDay"
        FROM resource_types
        WHERE id = @Id
        """;

    public const string GetAll = """
        SELECT id, name, description, is_renewable AS "isRenewable", regeneration_rate_per_day AS "regenerationRatePerDay"
        FROM resource_types
        """;

    public const string SearchByName = """
        SELECT id, name, description, is_renewable AS "isRenewable", regeneration_rate_per_day AS "regenerationRatePerDay"
        FROM resource_types
        WHERE name ILIKE '%' || @Search || '%'
        ORDER BY similarity(name, @Search) DESC, name ASC, id ASC
        """;

    public const string GetByName = """
        SELECT id, name, description, is_renewable AS "isRenewable", regeneration_rate_per_day AS "regenerationRatePerDay"
        FROM resource_types
        WHERE name = @Name
        """;

    public const string Insert = """
        INSERT INTO resource_types (name, description, is_renewable, regeneration_rate_per_day)
        VALUES (@Name, @Description, @IsRenewable, @RegenerationRatePerDay)
        RETURNING id;
        """;

    public const string Update = """
        UPDATE resource_types
        SET name                     = @Name,
            description              = @Description,
            is_renewable             = @IsRenewable,
            regeneration_rate_per_day = @RegenerationRatePerDay
        WHERE id = @Id
        """;

    public const string Delete = """
        DELETE FROM resource_types WHERE id = @Id
        """;
}

