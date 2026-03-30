using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Population;

public class PopulationGroup : AggregateRoot
{
    private readonly List<ConsumptionProfileItem> _consumptionProfile = [];
    private readonly List<PopulationStockItem> _stockItems = [];

    public int SettlementId { get; private set; }
    public string Name { get; private set; }
    public int PopulationSize { get; private set; }
    public decimal ReserveCoverageTicks { get; private set; }
    public IReadOnlyList<ConsumptionProfileItem> ConsumptionProfile => _consumptionProfile.AsReadOnly();
    public IReadOnlyList<PopulationStockItem> StockItems => _stockItems.AsReadOnly();

    public PopulationGroup(int id, int settlementId, string name, int populationSize, decimal reserveCoverageTicks = 0m) : base(id)
    {
        SettlementId = settlementId;
        Name = name;
        PopulationSize = populationSize;
        ReserveCoverageTicks = reserveCoverageTicks;
    }

    public static Result<PopulationGroup> Create(
        int settlementId,
        string name,
        int populationSize,
        decimal reserveCoverageTicks,
        IEnumerable<(int ProductTypeId, decimal AmountPerPersonPerTick)> consumptionProfile)
    {
        var validation = Validate(settlementId, name, populationSize, reserveCoverageTicks, consumptionProfile);
        if (!validation.IsSuccess)
            return Result<PopulationGroup>.Failure(validation.Error!);

        var group = new PopulationGroup(0, settlementId, name, populationSize, reserveCoverageTicks);
        var profileResult = group.ReplaceConsumptionProfile(consumptionProfile);
        if (!profileResult.IsSuccess)
            return Result<PopulationGroup>.Failure(profileResult.Error!);

        return Result<PopulationGroup>.Success(group);
    }

    public static Result<PopulationGroup> Create(
        int settlementId,
        string name,
        int populationSize,
        IEnumerable<(int ProductTypeId, decimal AmountPerPersonPerTick)> consumptionProfile) =>
        Create(settlementId, name, populationSize, 0m, consumptionProfile);

    public Result Update(
        string name,
        int populationSize,
        decimal reserveCoverageTicks,
        IEnumerable<(int ProductTypeId, decimal AmountPerPersonPerTick)> consumptionProfile)
    {
        var validation = Validate(SettlementId, name, populationSize, reserveCoverageTicks, consumptionProfile);
        if (!validation.IsSuccess)
            return validation;

        Name = name;
        PopulationSize = populationSize;
        ReserveCoverageTicks = reserveCoverageTicks;
        return ReplaceConsumptionProfile(consumptionProfile);
    }

    public Result Update(
        string name,
        int populationSize,
        IEnumerable<(int ProductTypeId, decimal AmountPerPersonPerTick)> consumptionProfile) =>
        Update(name, populationSize, ReserveCoverageTicks, consumptionProfile);

    public IReadOnlyDictionary<int, decimal> CalculateConsumptionDemand(
        IReadOnlyDictionary<int, decimal>? multipliersByProduct = null)
    {
        return _consumptionProfile
            .GroupBy(item => item.ProductTypeId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var multiplier = multipliersByProduct?.GetValueOrDefault(group.Key, 1m) ?? 1m;
                    return group.Sum(item => item.AmountPerPersonPerTick * PopulationSize) * multiplier;
                });
    }

    public IReadOnlyDictionary<int, decimal> ConsumeFromStock(IReadOnlyDictionary<int, decimal> requestedConsumption)
    {
        var unmetDemand = new Dictionary<int, decimal>();

        foreach (var request in requestedConsumption)
        {
            if (request.Value <= 0m)
                continue;

            var available = GetStockQuantity(request.Key);
            var consumed = decimal.Min(available, request.Value);
            if (consumed > 0m)
            {
                var item = _stockItems.FirstOrDefault(x => x.ProductTypeId == request.Key);
                if (item is not null)
                {
                    item.DecreaseQuantity(consumed);
                    if (item.Quantity == 0m)
                        _stockItems.Remove(item);
                }
            }

            var unmet = request.Value - consumed;
            if (unmet > 0m)
                unmetDemand[request.Key] = unmet;
        }

        return unmetDemand;
    }

    public IReadOnlyDictionary<int, decimal> CalculateDesiredReserve(
        IReadOnlyDictionary<int, decimal> plannedConsumptionDemand,
        IReadOnlyDictionary<int, decimal>? coverageMultipliersByProduct = null)
    {
        return plannedConsumptionDemand
            .Where(x => x.Value > 0m)
            .ToDictionary(
                x => x.Key,
                x =>
                {
                    var multiplier = coverageMultipliersByProduct?.GetValueOrDefault(x.Key, 1m) ?? 1m;
                    return x.Value * ReserveCoverageTicks * multiplier;
                });
    }

    public IReadOnlyDictionary<int, decimal> CalculateReserveDemand(
        IReadOnlyDictionary<int, decimal> desiredReserve)
    {
        return desiredReserve
            .Select(x => new KeyValuePair<int, decimal>(x.Key, decimal.Max(x.Value - GetStockQuantity(x.Key), 0m)))
            .Where(x => x.Value > 0m)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public decimal GetStockQuantity(int productTypeId) =>
        _stockItems
            .Where(item => item.ProductTypeId == productTypeId)
            .Sum(item => item.Quantity);

    public Result ReceiveReserveStock(int productTypeId, decimal quantity)
    {
        if (quantity <= 0m)
            return Result.Failure("Reserve stock quantity must be greater than zero");

        var existing = _stockItems.FirstOrDefault(item => item.ProductTypeId == productTypeId);
        if (existing is not null)
        {
            existing.IncreaseQuantity(quantity);
            return Result.Success();
        }

        var createResult = PopulationStockItem.Create(Id, productTypeId, quantity);
        if (!createResult.IsSuccess)
            return Result.Failure(createResult.Error!);

        _stockItems.Add(createResult.Value!);
        return Result.Success();
    }

    internal void LoadConsumptionProfile(IEnumerable<ConsumptionProfileItem> consumptionProfile)
    {
        _consumptionProfile.Clear();
        _consumptionProfile.AddRange(consumptionProfile);
    }

    internal void LoadStockItems(IEnumerable<PopulationStockItem> stockItems)
    {
        _stockItems.Clear();
        _stockItems.AddRange(stockItems);
    }

    private Result ReplaceConsumptionProfile(
        IEnumerable<(int ProductTypeId, decimal AmountPerPersonPerTick)> consumptionProfile)
    {
        var items = consumptionProfile.ToList();
        if (items.GroupBy(item => item.ProductTypeId).Any(group => group.Count() > 1))
            return Result.Failure("Consumption profile cannot contain duplicate products");

        var mappedItems = new List<ConsumptionProfileItem>(items.Count);
        foreach (var item in items)
        {
            var createResult = ConsumptionProfileItem.Create(Id, item.ProductTypeId, item.AmountPerPersonPerTick);
            if (!createResult.IsSuccess)
                return Result.Failure(createResult.Error!);

            mappedItems.Add(createResult.Value!);
        }

        LoadConsumptionProfile(mappedItems);
        return Result.Success();
    }

    private static Result Validate(
        int settlementId,
        string name,
        int populationSize,
        decimal reserveCoverageTicks,
        IEnumerable<(int ProductTypeId, decimal AmountPerPersonPerTick)> consumptionProfile)
    {
        if (settlementId <= 0)
            return Result.Failure("Settlement id must be greater than zero");

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Population group name cannot be empty");

        if (populationSize < 0)
            return Result.Failure("Population size cannot be negative");

        if (reserveCoverageTicks < 0m)
            return Result.Failure("Reserve coverage ticks cannot be negative");

        if (consumptionProfile.Any(item => item.AmountPerPersonPerTick < 0))
            return Result.Failure("Consumption amount cannot be negative");

        return Result.Success();
    }
}
