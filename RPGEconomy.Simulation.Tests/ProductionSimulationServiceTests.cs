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
        warehouse.AddItem(1, 4m, QualityGrade.Normal);
        var ctx = CreateContext(
            warehouse,
            [new Building(1, "Bakery", 10, 100, 2, true)],
            new Dictionary<int, ProductionRecipe>
            {
                [100] = ProductionRecipe.Create(
                    "Bread",
                    1,
                    [new RecipeIngredient(1, 2m)],
                    [new RecipeIngredient(2, 1m)]).Value!
            });

        new ProductionSimulationService().RunTick(ctx);

        warehouse.Items.Should().Contain(x => x.ProductTypeId == 2 && x.Quantity == 2m);
        warehouse.Items.Should().NotContain(x => x.ProductTypeId == 1);
        ctx.GetProductionDemand(10).Should().BeEmpty();
    }

    [Fact]
    public void RunTick_Should_Produce_Partially_And_Record_Missing_Input_Demand()
    {
        var warehouse = new Warehouse(1, 10);
        warehouse.AddItem(1, 3m, QualityGrade.Normal);
        var ctx = CreateContext(
            warehouse,
            [new Building(1, "Bakery", 10, 100, 2, true)],
            new Dictionary<int, ProductionRecipe>
            {
                [100] = ProductionRecipe.Create(
                    "Bread",
                    1,
                    [new RecipeIngredient(1, 2m)],
                    [new RecipeIngredient(2, 1m)]).Value!
            });

        new ProductionSimulationService().RunTick(ctx);

        warehouse.Items.Should().ContainSingle(x => x.ProductTypeId == 2 && x.Quantity == 1.5m);
        ctx.GetProductionDemand(10).Should().ContainSingle(x => x.Key == 1 && x.Value == 1m);
    }

    [Fact]
    public void RunTick_Should_Not_Produce_When_Only_High_Quality_Input_Exists()
    {
        var warehouse = new Warehouse(1, 10);
        warehouse.AddItem(1, 4m, QualityGrade.High);
        var ctx = CreateContext(
            warehouse,
            [new Building(1, "Bakery", 10, 100, 2, true)],
            new Dictionary<int, ProductionRecipe>
            {
                [100] = ProductionRecipe.Create(
                    "Bread",
                    1,
                    [new RecipeIngredient(1, 2m)],
                    [new RecipeIngredient(2, 1m)]).Value!
            });

        new ProductionSimulationService().RunTick(ctx);

        warehouse.Items.Should().NotContain(x => x.ProductTypeId == 2);
        warehouse.Items.Should().ContainSingle(x => x.ProductTypeId == 1 && x.Quantity == 4m && x.Quality == QualityGrade.High.Name);
        ctx.GetProductionDemand(10).Should().ContainSingle(x => x.Key == 1 && x.Value == 4m);
    }

    [Fact]
    public void RunTick_Should_Use_Outputs_From_Earlier_Buildings_In_Deterministic_Order()
    {
        var warehouse = new Warehouse(1, 10);
        warehouse.AddItem(1, 1m, QualityGrade.Normal);
        var ctx = CreateContext(
            warehouse,
            [
                new Building(1, "Mill", 10, 100, 1, true),
                new Building(2, "Bakery", 10, 101, 1, true)
            ],
            new Dictionary<int, ProductionRecipe>
            {
                [100] = ProductionRecipe.Create(
                    "Flour",
                    1,
                    [new RecipeIngredient(1, 1m)],
                    [new RecipeIngredient(2, 1m)]).Value!,
                [101] = ProductionRecipe.Create(
                    "Bread",
                    1,
                    [new RecipeIngredient(2, 1m)],
                    [new RecipeIngredient(3, 1m)]).Value!
            });

        new ProductionSimulationService().RunTick(ctx);

        warehouse.Items.Should().ContainSingle(x => x.ProductTypeId == 3 && x.Quantity == 1m);
        ctx.GetProductionDemand(10).Should().BeEmpty();
    }

    private static SimulationContext CreateContext(
        Warehouse warehouse,
        IReadOnlyList<Building> buildings,
        IReadOnlyDictionary<int, ProductionRecipe> recipes)
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
            recipes);
    }
}
