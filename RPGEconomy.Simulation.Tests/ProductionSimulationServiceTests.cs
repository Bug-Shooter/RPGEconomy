using FluentAssertions;
using RPGEconomy.Domain.Markets;
using RPGEconomy.Domain.Population;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.Resources;
using RPGEconomy.Domain.World;
using RPGEconomy.Simulation.Engine;
using RPGEconomy.Simulation.Services;

namespace RPGEconomy.Simulation.Tests;

public class ProductionSimulationServiceTests
{
    [Fact]
    public void RunTick_Should_Produce_Output_When_Building_Is_Active_And_Inputs_Are_Available()
    {
        var warehouse = new Warehouse(1, 10);
        warehouse.AddItem(1, 4, QualityGrade.Normal);
        var ctx = CreateContext(
            warehouse,
            [new Building(1, "Bakery", 10, 100, 2, true)],
            ProductionRecipe.Create(
                "Bread",
                1,
                [new RecipeIngredient(1, 2)],
                [new RecipeIngredient(2, 1)]));

        new ProductionSimulationService().RunTick(ctx);

        warehouse.Items.Should().Contain(x => x.ProductTypeId == 2 && x.Quantity == 2);
        warehouse.Items.Should().NotContain(x => x.ProductTypeId == 1);
    }

    [Fact]
    public void RunTick_Should_Not_Produce_When_Only_High_Quality_Input_Exists()
    {
        var warehouse = new Warehouse(1, 10);
        warehouse.AddItem(1, 4, QualityGrade.High);
        var ctx = CreateContext(
            warehouse,
            [new Building(1, "Bakery", 10, 100, 2, true)],
            ProductionRecipe.Create(
                "Bread",
                1,
                [new RecipeIngredient(1, 2)],
                [new RecipeIngredient(2, 1)]));

        new ProductionSimulationService().RunTick(ctx);

        warehouse.Items.Should().NotContain(x => x.ProductTypeId == 2);
        warehouse.Items.Should().ContainSingle(x => x.ProductTypeId == 1 && x.Quantity == 4 && x.Quality == QualityGrade.High.Name);
    }

    private static SimulationContext CreateContext(
        Warehouse warehouse,
        IReadOnlyList<Building> buildings,
        ProductionRecipe recipe)
    {
        var settlement = new Settlement(10, 1, "Town", 100);
        return new SimulationContext(
            1,
            0,
            [settlement],
            new Dictionary<int, Warehouse> { [settlement.Id] = warehouse },
            new Dictionary<int, Market> { [settlement.Id] = new Market(2, settlement.Id) },
            new Dictionary<int, IReadOnlyList<PopulationGroup>>(),
            new Dictionary<int, IReadOnlyList<Building>> { [settlement.Id] = buildings },
            new Dictionary<int, ProductionRecipe> { [100] = recipe });
    }
}
