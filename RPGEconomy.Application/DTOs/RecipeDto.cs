namespace RPGEconomy.Application.DTOs;

public record RecipeDto(
    int Id,
    string Name,
    double LaborDaysRequired,
    IReadOnlyList<RecipeIngredientDto> Inputs,
    IReadOnlyList<RecipeIngredientDto> Outputs);
