using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Markets;

public class Market : AggregateRoot
{
    private readonly List<MarketOffer> _offers = new();

    public int SettlementId { get; private set; }
    public IReadOnlyList<MarketOffer> Offers => _offers.AsReadOnly();

    // Dapper
    public Market(int id, int settlementId) : base(id)
    {
        SettlementId = settlementId;
    }

    public static Market Create(int settlementId)
        => new(0, settlementId);

    public Result RegisterProduct(int productTypeId, double initialPrice)
    {
        if (_offers.Any(o => o.ProductTypeId == productTypeId))
            return Result.Failure("Товар уже зарегистрирован на рынке");

        _offers.Add(MarketOffer.Create(Id, productTypeId, initialPrice));
        return Result.Success();
    }

    public Result UpdateMarket(int productTypeId, int supply, int demand)
    {
        var offer = _offers.FirstOrDefault(o => o.ProductTypeId == productTypeId);
        if (offer is null) return Result.Failure("Товар не найден на рынке");

        offer.UpdateVolumes(supply, demand);
        offer.RecalculatePrice();
        return Result.Success();
    }

    public double? GetPrice(int productTypeId) =>
        _offers.FirstOrDefault(o => o.ProductTypeId == productTypeId)?.CurrentPrice;

    internal void LoadOffers(IEnumerable<MarketOffer> offers)
    {
        _offers.Clear();
        _offers.AddRange(offers);
    }
}

