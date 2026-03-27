using Dapper;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Domain.Production;
using RPGEconomy.Infrastructure.Persistence.Queries;
using System.Data;

namespace RPGEconomy.Infrastructure.Persistence.Repositories;

public class ProductionRecipeRepository : IProductionRecipeRepository
{
    private readonly IDbConnectionFactory _factory;

    public ProductionRecipeRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<ProductionRecipe?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();

        var recipe = await conn.QueryFirstOrDefaultAsync<ProductionRecipe>(
            ProductionRecipeQueries.GetById, new { Id = id });

        if (recipe is null) return null;
        await LoadIngredientsAsync(conn, recipe);
        return recipe;
    }

    public async Task<IReadOnlyList<ProductionRecipe>> GetAllAsync()
    {
        using var conn = _factory.Create();

        var recipes = (await conn.QueryAsync<ProductionRecipe>(
            ProductionRecipeQueries.GetAll)).ToList();

        foreach (var recipe in recipes)
            await LoadIngredientsAsync(conn, recipe);

        return recipes.AsReadOnly();
    }

    public async Task<int> SaveAsync(ProductionRecipe recipe)
    {
        using var conn = _factory.Create();

        int recipeId;

        if (recipe.IsNew)
        {
            recipeId = await conn.ExecuteScalarAsync<int>(
                ProductionRecipeQueries.Insert, new
                {
                    recipe.Name,
                    recipe.LaborDaysRequired
                });
        }
        else
        {
            recipeId = recipe.Id;
            await conn.ExecuteAsync(ProductionRecipeQueries.Update, new
            {
                recipe.Id,
                recipe.Name,
                recipe.LaborDaysRequired
            });
        }

        // Пересохраняем ингредиенты (delete + insert)
        await conn.ExecuteAsync(
            ProductionRecipeQueries.DeleteIngredients, new { RecipeId = recipeId });

        var allIngredients = recipe.Inputs
            .Select(i => new { RecipeId = recipeId, i.ProductTypeId, i.Quantity, IsInput = true })
            .Concat(recipe.Outputs
            .Select(o => new { RecipeId = recipeId, o.ProductTypeId, o.Quantity, IsInput = false }));

        if (allIngredients.Any())
            await conn.ExecuteAsync(
                ProductionRecipeQueries.InsertIngredient, allIngredients);

        return recipeId;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync(
            ProductionRecipeQueries.DeleteIngredients, new { RecipeId = id });
        await conn.ExecuteAsync(
            ProductionRecipeQueries.Delete, new { Id = id });
    }

    private async Task LoadIngredientsAsync(IDbConnection conn, ProductionRecipe recipe)
    {
        // Читаем сырые данные из БД
        var rows = await conn.QueryAsync<(int Id, int RecipeId, int ProductTypeId, decimal Quantity, bool IsInput)>(
            ProductionRecipeQueries.GetIngredients, new { RecipeId = recipe.Id });

        var inputs = rows.Where(r => r.IsInput)
                         .Select(r => new RecipeIngredient(r.ProductTypeId, r.Quantity));

        var outputs = rows.Where(r => !r.IsInput)
                         .Select(r => new RecipeIngredient(r.ProductTypeId, r.Quantity));

        recipe.LoadIngredients(inputs, outputs);
    }
}
