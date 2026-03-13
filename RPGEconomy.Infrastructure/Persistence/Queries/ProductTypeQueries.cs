namespace RPGEconomy.Infrastructure.Persistence.Queries;

internal static class ProductTypeQueries
{
    public const string GetById = """
        SELECT id, name, description, base_price AS "basePrice", weight_per_unit AS "weightPerUnit"
        FROM product_types
        WHERE id = @Id
        """;

    public const string GetAll = """
        SELECT id, name, description, base_price AS "basePrice", weight_per_unit AS "weightPerUnit"
        FROM product_types
        """;

    public const string GetByName = """
        SELECT id, name, description, base_price AS "basePrice", weight_per_unit AS "weightPerUnit"
        FROM product_types
        WHERE name = @Name
        """;

    public const string Insert = """
        INSERT INTO product_types (name, description, base_price, weight_per_unit)
        VALUES (@Name, @Description, @BasePrice, @WeightPerUnit)
        RETURNING id;
        """;

    public const string Update = """
        UPDATE product_types
        SET name            = @Name,
            description     = @Description,
            base_price      = @BasePrice,
            weight_per_unit = @WeightPerUnit
        WHERE id = @Id
        """;

    public const string Delete = """
        DELETE FROM product_types WHERE id = @Id
        """;
}

