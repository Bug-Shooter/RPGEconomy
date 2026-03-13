using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Markets;

public class MarketOffer : Entity
{
    public int MarketId { get; private set; }
    public int ProductTypeId { get; private set; }
    public double CurrentPrice { get; private set; }
    public int SupplyVolume { get; private set; }
    public int DemandVolume { get; private set; }

    // Dapper
    public MarketOffer(int id, int marketId, int productTypeId,
        double currentPrice, int supplyVolume, int demandVolume) : base(id)
    {
        MarketId = marketId;
        ProductTypeId = productTypeId;
        CurrentPrice = currentPrice;
        SupplyVolume = supplyVolume;
        DemandVolume = demandVolume;
    }

    public static MarketOffer Create(int marketId, int productTypeId, double initialPrice)
        => new(0, marketId, productTypeId, initialPrice, 0, 0);

    internal void UpdateVolumes(int supply, int demand)
    {
        SupplyVolume = supply;
        DemandVolume = demand;
    }

    internal void RecalculatePrice(double sensitivity = 0.1f)
    {
        if (SupplyVolume <= 0 && DemandVolume > 0)
        {
            CurrentPrice *= 1 + sensitivity;
            return;
        }

        double ratio = (double)DemandVolume / Math.Max(SupplyVolume, 1);
        CurrentPrice *= 1 + sensitivity * (ratio - 1);
        CurrentPrice = Math.Max(CurrentPrice, 0.01f);
    }
}
