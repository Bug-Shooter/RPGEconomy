using FluentAssertions;
using RPGEconomy.Domain.Production;

namespace RPGEconomy.Domain.Tests;

public class ProductionRecipeTests
{
    [Fact]
    public void Create_Should_Reject_Recipe_Without_Outputs()
    {
        var result = ProductionRecipe.Create(
            "Invalid",
            1,
            [new RecipeIngredient(1, 1m)],
            []);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("хотя бы один выходной товар");
    }

    [Fact]
    public void Create_Should_Reject_Duplicate_Input_Product()
    {
        var result = ProductionRecipe.Create(
            "Invalid",
            1,
            [new RecipeIngredient(1, 1m), new RecipeIngredient(1, 2m)],
            [new RecipeIngredient(2, 1m)]);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("дублирующиеся входные ресурсы");
    }

    [Fact]
    public void Create_Should_Reject_NonPositive_Ingredient_Quantity()
    {
        var result = ProductionRecipe.Create(
            "Invalid",
            1,
            [new RecipeIngredient(1, 0m)],
            [new RecipeIngredient(2, 1m)]);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("входного ресурса");
    }

    [Fact]
    public void Update_Should_Reject_Duplicate_Output_Product()
    {
        var recipe = ProductionRecipe.Create(
            "Bread",
            1,
            [new RecipeIngredient(1, 1m)],
            [new RecipeIngredient(2, 1m)]).Value!;

        var result = recipe.Update(
            "Bread",
            1,
            [new RecipeIngredient(1, 1m)],
            [new RecipeIngredient(2, 1m), new RecipeIngredient(2, 0.5m)]);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("дублирующиеся выходные товары");
    }
}
