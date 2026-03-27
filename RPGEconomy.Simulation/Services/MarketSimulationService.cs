using RPGEconomy.Simulation.Engine;

namespace RPGEconomy.Simulation.Services;

public class MarketSimulationService
{
    private readonly PopulationMarketDemandProvider _demandProvider;

    public MarketSimulationService(PopulationMarketDemandProvider demandProvider)
        => _demandProvider = demandProvider;

    // Обновляет цены на рынке на основе остатков склада
    public void RunTick(SimulationContext ctx)
    {
        foreach (var settlement in ctx.Settlements)
        {
            if (!ctx.Markets.TryGetValue(settlement.Id, out var market)) continue;
            if (!ctx.Warehouses.TryGetValue(settlement.Id, out var warehouse)) continue;

            foreach (var offer in market.Offers)
            {
                // Предложение = то что есть на складе
                var supply = warehouse.Items
                    .Where(i => i.ProductTypeId == offer.ProductTypeId)
                    .Sum(i => i.Quantity);

                // Спрос = упрощённо: базовый спрос пропорционален населению
                // В V2 изменить на реальный спрос из ConsumptionProfile
                var demand = _demandProvider.GetDemand(settlement, offer.ProductTypeId);

                market.UpdateProductState(offer.ProductTypeId, supply, demand);
            }
        }
    }
}
