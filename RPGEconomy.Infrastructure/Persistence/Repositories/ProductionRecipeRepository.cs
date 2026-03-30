using Dapper;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Domain.Production;
using RPGEconomy.Infrastructure.Persistence.Queries;
using System.Data;
using System.Transactions;

namespace RPGEconomy.Infrastructure.Persistence.Repositories;

public class ProductionRecipeRepository : IProductionRecipeRepository
{
    private readonly IDbConnectionFactory _factory;

    public ProductionRecipeRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<ProductionRecipe?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();

        var recipe = await conn.QueryFirstOrDefaultAsync<ProductionRecipe>(ProductionRecipeQueries.GetById, new { Id = id });
        if (recipe is null)
            return null;

        await LoadIngredientsAsync(conn, recipe);
        return recipe;
    }

    public async Task<IReadOnlyList<ProductionRecipe>> GetAllAsync()
    {
        using var conn = _factory.Create();

        var recipes = (await conn.QueryAsync<ProductionRecipe>(ProductionRecipeQueries.GetAll)).ToList();
        foreach (var recipe in recipes)
            await LoadIngredientsAsync(conn, recipe);

        return recipes.AsReadOnly();
    }

    public async Task<IReadOnlyList<ProductionRecipe>> SearchByNameAsync(string search)
    {
        using var conn = _factory.Create();

        var recipes = (await conn.QueryAsync<ProductionRecipe>(
            ProductionRecipeQueries.SearchByName,
            new { Search = search })).ToList();
        foreach (var recipe in recipes)
            await LoadIngredientsAsync(conn, recipe);

        return recipes.AsReadOnly();
    }

    public async Task<int> SaveAsync(ProductionRecipe recipe)
    {
        using var conn = _factory.Create();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var useLocalTransaction = Transaction.Current is null;
        using var tx = useLocalTransaction ? conn.BeginTransaction() : null;

        int recipeId;
        if (recipe.IsNew)
        {
            recipeId = await conn.ExecuteScalarAsync<int>(
                ProductionRecipeQueries.Insert,
                new
                {
                    recipe.Name,
                    recipe.LaborDaysRequired
                },
                tx);
        }
        else
        {
            recipeId = recipe.Id;
            await conn.ExecuteAsync(
                ProductionRecipeQueries.Update,
                new
                {
                    recipe.Id,
                    recipe.Name,
                    recipe.LaborDaysRequired
                },
                tx);
        }

        await conn.ExecuteAsync(ProductionRecipeQueries.DeleteIngredients, new { RecipeId = recipeId }, tx);

        var allIngredients = recipe.Inputs
            .Select(input => new { RecipeId = recipeId, input.ProductTypeId, input.Quantity, IsInput = true })
            .Concat(recipe.Outputs.Select(output => new { RecipeId = recipeId, output.ProductTypeId, output.Quantity, IsInput = false }));

        if (allIngredients.Any())
            await conn.ExecuteAsync(ProductionRecipeQueries.InsertIngredient, allIngredients, tx);

        tx?.Commit();
        return recipeId;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync(ProductionRecipeQueries.Delete, new { Id = id });
    }

    private static async Task LoadIngredientsAsync(IDbConnection conn, ProductionRecipe recipe)
    {
        var rows = await conn.QueryAsync<(int Id, int RecipeId, int ProductTypeId, decimal Quantity, bool IsInput)>(
            ProductionRecipeQueries.GetIngredients,
            new { RecipeId = recipe.Id });

        var inputs = rows.Where(row => row.IsInput).Select(row => new RecipeIngredient(row.ProductTypeId, row.Quantity));
        var outputs = rows.Where(row => !row.IsInput).Select(row => new RecipeIngredient(row.ProductTypeId, row.Quantity));
        recipe.LoadIngredients(inputs, outputs);
    }
}
