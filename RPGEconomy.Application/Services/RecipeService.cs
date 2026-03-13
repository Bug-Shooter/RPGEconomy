using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Production;

namespace RPGEconomy.Application.Services;

public class RecipeService : IRecipeService
{
    private readonly IProductionRecipeRepository _recipeRepo;

    public RecipeService(IProductionRecipeRepository recipeRepo)
        => _recipeRepo = recipeRepo;

    public async Task<Result<RecipeDto>> GetByIdAsync(int id)
    {
        var recipe = await _recipeRepo.GetByIdAsync(id);
        if (recipe is null)
            return Result<RecipeDto>.Failure($"Рецепт с Id {id} не найден");

        return Result<RecipeDto>.Success(ToDto(recipe));
    }

    public async Task<Result<IReadOnlyList<RecipeDto>>> GetAllAsync()
    {
        var recipes = await _recipeRepo.GetAllAsync();
        var dtos = recipes.Select(ToDto).ToList().AsReadOnly();
        return Result<IReadOnlyList<RecipeDto>>.Success(dtos);
    }

    public async Task<Result<RecipeDto>> CreateAsync(
        string name, double laborDaysRequired,
        IEnumerable<RecipeIngredientDto> inputs,
        IEnumerable<RecipeIngredientDto> outputs)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<RecipeDto>.Failure("Название рецепта не может быть пустым");

        if (laborDaysRequired <= 0)
            return Result<RecipeDto>.Failure("Трудозатраты должны быть больше нуля");

        var recipe = ProductionRecipe.Create(
            name, laborDaysRequired,
            inputs.Select(i => new RecipeIngredient(i.ProductTypeId, i.Quantity)),
            outputs.Select(o => new RecipeIngredient(o.ProductTypeId, o.Quantity)));

        var id = await _recipeRepo.SaveAsync(recipe);
        return Result<RecipeDto>.Success(ToDto(recipe) with { Id = id });
    }

    public async Task<Result<RecipeDto>> UpdateAsync(
        int id, string name, double laborDaysRequired,
        IEnumerable<RecipeIngredientDto> inputs,
        IEnumerable<RecipeIngredientDto> outputs)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<RecipeDto>.Failure("Название рецепта не может быть пустым");

        if (laborDaysRequired <= 0)
            return Result<RecipeDto>.Failure("Трудозатраты должны быть больше нуля");

        var recipe = await _recipeRepo.GetByIdAsync(id);
        if (recipe is null)
            return Result<RecipeDto>.Failure($"Рецепт с Id {id} не найден");

        recipe.Update(
            name, laborDaysRequired,
            inputs.Select(i => new RecipeIngredient(i.ProductTypeId, i.Quantity)),
            outputs.Select(o => new RecipeIngredient(o.ProductTypeId, o.Quantity)));

        await _recipeRepo.SaveAsync(recipe);
        return Result<RecipeDto>.Success(ToDto(recipe));
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var recipe = await _recipeRepo.GetByIdAsync(id);
        if (recipe is null) return Result.Failure($"Рецепт с Id {id} не найден");

        await _recipeRepo.DeleteAsync(id);
        return Result.Success();
    }

    private static RecipeDto ToDto(ProductionRecipe r) => new(
        r.Id, r.Name, r.LaborDaysRequired,
        r.Inputs.Select(i => new RecipeIngredientDto(i.ProductTypeId, i.Quantity)).ToList().AsReadOnly(),
        r.Outputs.Select(o => new RecipeIngredientDto(o.ProductTypeId, o.Quantity)).ToList().AsReadOnly());
}
