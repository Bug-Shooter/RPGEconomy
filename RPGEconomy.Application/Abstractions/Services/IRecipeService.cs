using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface IRecipeService
{
    Task<Result<RecipeDto>> GetByIdAsync(int id);
    Task<Result<IReadOnlyList<RecipeDto>>> GetAllAsync();
    Task<Result<RecipeDto>> CreateAsync(
        string name,
        double laborDaysRequired,
        IEnumerable<RecipeIngredientDto> inputs,
        IEnumerable<RecipeIngredientDto> outputs);
    Task<Result<RecipeDto>> UpdateAsync(
        int id,
        string name,
        double laborDaysRequired,
        IEnumerable<RecipeIngredientDto> inputs,
        IEnumerable<RecipeIngredientDto> outputs);
    Task<Result> DeleteAsync(int id);
}
