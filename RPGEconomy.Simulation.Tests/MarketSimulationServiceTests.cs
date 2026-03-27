using FluentAssertions;
using RPGEconomy.Domain.Markets;
using RPGEconomy.Domain.Population;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.Resources;
using RPGEconomy.Domain.World;
using RPGEconomy.Simulation.Engine;
using RPGEconomy.Simulation.Services;

namespace RPGEconomy.Simulation.Tests;

public class MarketSimulationServiceTests
{
    [Fact]
    public void RunTick_Should_Use_Warehouse_Supply_And_Population_Group_Demand()
    {
        var settlement = new Settlement(10, 1, "Town", 250);
        var warehouse = new Warehouse(1, settlement.Id);
        warehouse.AddItem(99, 12m, QualityGrade.Normal);
        var market = new Market(2, settlement.Id);
        market.RegisterProduct(99, 10m);
        var populationGroup = PopulationGroup.Create(
            settlement.Id,
            "Peasants",
            50,
            [(99, 0.05m)]).Value!;

        var ctx = new SimulationContext(
            1,
            0,
            [settlement],
            new Dictionary<int, Warehouse> { [settlement.Id] = warehouse },
            new Dictionary<int, Market> { [settlement.Id] = market },
            new Dictionary<int, IReadOnlyList<PopulationGroup>> { [settlement.Id] = [populationGroup] },
            new Dictionary<int, IReadOnlyList<Building>>(),
            new Dictionary<int, ProductionRecipe>());

        new SettlementEconomySimulationService().RunTick(ctx);

        market.Offers.Should().ContainSingle();
        market.Offers[0].SupplyVolume.Should().Be(12m);
        market.Offers[0].DemandVolume.Should().Be(2.5m);
    }
}
