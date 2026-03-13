namespace RPGEconomy.Infrastructure.Persistence.Queries;

internal static class ProductionRecipeQueries
{
    public const string GetById = """
        SELECT id, name, labor_days_required AS "laborDaysRequired"
        FROM production_recipes
        WHERE id = @Id
        """;

    public const string GetAll = """
        SELECT id, name, labor_days_required AS "laborDaysRequired"
        FROM production_recipes
        """;

    public const string Insert = """
        INSERT INTO production_recipes (name, labor_days_required)
        VALUES (@Name, @LaborDaysRequired)
        RETURNING id;
        """;

    public const string Update = """
        UPDATE production_recipes
        SET name                = @Name,
            labor_days_required = @LaborDaysRequired
        WHERE id = @Id
        """;

    public const string Delete = """
        DELETE FROM production_recipes WHERE id = @Id
        """;

    public const string GetIngredients = """
        SELECT id, recipe_id AS "recipeId", product_type_id AS "productTypeId", quantity, is_input AS "isInput"
        FROM recipe_ingredients
        WHERE recipe_id = @RecipeId
        """;

    public const string DeleteIngredients = """
        DELETE FROM recipe_ingredients WHERE recipe_id = @RecipeId
        """;

    public const string InsertIngredient = """
        INSERT INTO recipe_ingredients (recipe_id, product_type_id, quantity, is_input)
        VALUES (@RecipeId, @ProductTypeId, @Quantity, @IsInput)
        """;
}

