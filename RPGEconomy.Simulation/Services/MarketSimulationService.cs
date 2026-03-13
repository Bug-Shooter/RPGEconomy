using RPGEconomy.Simulation.Engine;

namespace RPGEconomy.Simulation.Services;

public class MarketSimulationService
{
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
                // В V2 заменим на реальный спрос из ConsumptionProfile
                var demand = CalculateBaseDemand(settlement.Population, offer.ProductTypeId);

                market.UpdateMarket(offer.ProductTypeId, supply, demand);
            }
        }
    }

    // Заглушка для MVP — фиксированный спрос на единицу населения
    // Позже будет заменено на ConsumptionProfile
    private int CalculateBaseDemand(int population, int productTypeId)
        => (int)Math.Ceiling(population * 0.01f);
}
