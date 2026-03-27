using RPGEconomy.Domain.World;

namespace RPGEconomy.Simulation.Services;

public class PopulationMarketDemandProvider
{
    public int GetDemand(Settlement settlement, int productTypeId)
        => (int)Math.Ceiling(settlement.Population * 0.01d);
}
