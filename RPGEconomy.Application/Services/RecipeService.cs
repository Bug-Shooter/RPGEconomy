using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Production;

namespace RPGEconomy.Application.Services;

public class RecipeService : IRecipeService
{
    private readonly IProductionRecipeRepository _recipeRepo;
    private readonly IProductTypeRepository _productTypeRepo;

    public RecipeService(
        IProductionRecipeRepository recipeRepo,
        IProductTypeRepository productTypeRepo)
    {
        _recipeRepo = recipeRepo;
        _productTypeRepo = productTypeRepo;
    }

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
        string name,
        double laborDaysRequired,
        IEnumerable<RecipeIngredientDto> inputs,
        IEnumerable<RecipeIngredientDto> outputs)
    {
        var inputList = inputs.ToList();
        var outputList = outputs.ToList();

        var productValidation = await ValidateProductTypesAsync(inputList, outputList);
        if (!productValidation.IsSuccess)
            return Result<RecipeDto>.Failure(productValidation.Error!);

        var createResult = ProductionRecipe.Create(
            name,
            laborDaysRequired,
            inputList.Select(i => new RecipeIngredient(i.ProductTypeId, i.Quantity)),
            outputList.Select(o => new RecipeIngredient(o.ProductTypeId, o.Quantity)));

        if (!createResult.IsSuccess)
            return Result<RecipeDto>.Failure(createResult.Error!);

        var recipe = createResult.Value!;
        var id = await _recipeRepo.SaveAsync(recipe);
        return Result<RecipeDto>.Success(ToDto(recipe) with { Id = id });
    }

    public async Task<Result<RecipeDto>> UpdateAsync(
        int id,
        string name,
        double laborDaysRequired,
        IEnumerable<RecipeIngredientDto> inputs,
        IEnumerable<RecipeIngredientDto> outputs)
    {
        var recipe = await _recipeRepo.GetByIdAsync(id);
        if (recipe is null)
            return Result<RecipeDto>.Failure($"Рецепт с Id {id} не найден");

        var inputList = inputs.ToList();
        var outputList = outputs.ToList();

        var productValidation = await ValidateProductTypesAsync(inputList, outputList);
        if (!productValidation.IsSuccess)
            return Result<RecipeDto>.Failure(productValidation.Error!);

        var updateResult = recipe.Update(
            name,
            laborDaysRequired,
            inputList.Select(i => new RecipeIngredient(i.ProductTypeId, i.Quantity)),
            outputList.Select(o => new RecipeIngredient(o.ProductTypeId, o.Quantity)));

        if (!updateResult.IsSuccess)
            return Result<RecipeDto>.Failure(updateResult.Error!);

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

    private async Task<Result> ValidateProductTypesAsync(
        IEnumerable<RecipeIngredientDto> inputs,
        IEnumerable<RecipeIngredientDto> outputs)
    {
        var productTypeIds = inputs
            .Concat(outputs)
            .Select(item => item.ProductTypeId)
            .Distinct()
            .ToList();

        foreach (var productTypeId in productTypeIds)
        {
            var productType = await _productTypeRepo.GetByIdAsync(productTypeId);
            if (productType is null)
                return Result.Failure($"Тип товара с Id {productTypeId} не найден");
        }

        return Result.Success();
    }

    private static RecipeDto ToDto(ProductionRecipe recipe) => new(
        recipe.Id,
        recipe.Name,
        recipe.LaborDaysRequired,
        recipe.Inputs.Select(i => new RecipeIngredientDto(i.ProductTypeId, i.Quantity)).ToList().AsReadOnly(),
        recipe.Outputs.Select(o => new RecipeIngredientDto(o.ProductTypeId, o.Quantity)).ToList().AsReadOnly());
}
