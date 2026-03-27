using RPGEconomy.Domain.Population;
using RPGEconomy.Simulation.Engine;

namespace RPGEconomy.Simulation.Services;

public class SettlementEconomySimulationService
{
    public void RunTick(SimulationContext ctx)
    {
        foreach (var settlement in ctx.Settlements)
        {
            if (!ctx.Markets.TryGetValue(settlement.Id, out var market))
                continue;

            var supplyByProduct = ctx.Warehouses.TryGetValue(settlement.Id, out var warehouse)
                ? warehouse.Items
                    .GroupBy(item => item.ProductTypeId)
                    .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity))
                : new Dictionary<int, decimal>();

            var demandByProduct = ctx.PopulationGroups.TryGetValue(settlement.Id, out var groups)
                ? AggregateDemand(groups)
                : new Dictionary<int, decimal>();

            foreach (var offer in market.Offers)
            {
                var supply = supplyByProduct.GetValueOrDefault(offer.ProductTypeId, 0m);
                var demand = demandByProduct.GetValueOrDefault(offer.ProductTypeId, 0m);
                market.UpdateProductState(offer.ProductTypeId, supply, demand);
            }
        }
    }

    private static Dictionary<int, decimal> AggregateDemand(IReadOnlyList<PopulationGroup> groups)
    {
        var demandByProduct = new Dictionary<int, decimal>();
        foreach (var group in groups)
        {
            foreach (var demand in group.CalculateDemand())
                demandByProduct[demand.Key] = demandByProduct.GetValueOrDefault(demand.Key, 0m) + demand.Value;
        }

        return demandByProduct;
    }
}
