using FluentAssertions;
using RPGEconomy.Domain.Production;

namespace RPGEconomy.Domain.Tests;

public class BuildingTests
{
    [Fact]
    public void Activate_Deactivate_Should_Enforce_Valid_State_Transitions()
    {
        // Тест двойной активации и двойной деактивации

        var building = new Building(1, "Mill", 1, 1, 3, true);

        var deactivate = building.Deactivate();
        deactivate.IsSuccess.Should().BeTrue();
        building.IsActive.Should().BeFalse();

        var deactivateAgain = building.Deactivate();
        deactivateAgain.IsSuccess.Should().BeFalse();
        building.IsActive.Should().BeFalse();

        var activate = building.Activate();
        activate.IsSuccess.Should().BeTrue();
        building.IsActive.Should().BeTrue();

        var activateAgain = building.Activate();
        activateAgain.IsSuccess.Should().BeFalse();
        building.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(5, 2, 2)]
    [InlineData(3, 0, 0)]
    public void BatchesPerDay_Should_Return_Expected_Value(
        int workerCount,
        double laborDaysPerBatch,
        int expected)
    {
        //Тест количества произведенных товаров, при 'n работников' и 'n трудо-дней на партию'

        var building = new Building(1, "Mill", 1, 1, workerCount, true);

        var result = building.BatchesPerDay(laborDaysPerBatch);

        result.Should().Be(expected);
    }

    [Fact]
    public void CalculatePlannedInputDemand_Should_Aggregate_Duplicate_Input_Product_Rows()
    {
        var building = new Building(1, "Mill", 1, 1, 2, true);
        var recipe = CreatePersistedRecipe(
            10,
            "Flour",
            1,
            [new RecipeIngredient(1, 2m), new RecipeIngredient(1, 3m), new RecipeIngredient(2, 1m)],
            [new RecipeIngredient(3, 1m)]);

        var demand = building.CalculatePlannedInputDemand(recipe);

        demand.Should().BeEquivalentTo(new Dictionary<int, decimal>
        {
            [1] = 10m,
            [2] = 2m
        });
    }

    private static ProductionRecipe CreatePersistedRecipe(
        int id,
        string name,
        double laborDaysRequired,
        IReadOnlyList<RecipeIngredient> inputs,
        IReadOnlyList<RecipeIngredient> outputs)
    {
        var recipe = new ProductionRecipe(id, name, laborDaysRequired);
        var loadIngredients = typeof(ProductionRecipe).GetMethod("LoadIngredients", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        loadIngredients.Should().NotBeNull();

        loadIngredients!.Invoke(recipe, [inputs, outputs]);
        return recipe;
    }
}
