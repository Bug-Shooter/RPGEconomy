using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Markets;

public class MarketOffer : Entity
{
    public int MarketId { get; private set; }
    public int ProductTypeId { get; private set; }
    public decimal CurrentPrice { get; private set; }
    public decimal SupplyVolume { get; private set; }
    public decimal DemandVolume { get; private set; }

    // Dapper
    public MarketOffer(int id, int marketId, int productTypeId,
        decimal currentPrice, decimal supplyVolume, decimal demandVolume) : base(id)
    {
        MarketId = marketId;
        ProductTypeId = productTypeId;
        CurrentPrice = currentPrice;
        SupplyVolume = supplyVolume;
        DemandVolume = demandVolume;
    }

    public static Result<MarketOffer> Create(int marketId, int productTypeId, decimal initialPrice)
    {
        if (initialPrice <= 0)
            return Result<MarketOffer>.Failure("Начальная цена должна быть больше нуля");

        return Result<MarketOffer>.Success(
            new MarketOffer(0, marketId, productTypeId, initialPrice, 0, 0));
    }

    internal Result UpdateState(decimal supply, decimal demand)
    {
        if (supply < 0)
            return Result.Failure("Предложение не может быть отрицательным");

        if (demand < 0)
            return Result.Failure("Спрос не может быть отрицательным");

        SupplyVolume = supply;
        DemandVolume = demand;
        CurrentPrice = MarketPricePolicy.Recalculate(CurrentPrice, SupplyVolume, DemandVolume);
        return Result.Success();
    }
}
