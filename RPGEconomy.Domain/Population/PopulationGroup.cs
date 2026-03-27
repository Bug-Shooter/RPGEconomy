using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Population;

public class PopulationGroup : AggregateRoot
{
    private readonly List<ConsumptionProfileItem> _consumptionProfile = [];

    public int SettlementId { get; private set; }
    public string Name { get; private set; }
    public int PopulationSize { get; private set; }
    public IReadOnlyList<ConsumptionProfileItem> ConsumptionProfile => _consumptionProfile.AsReadOnly();

    public PopulationGroup(int id, int settlementId, string name, int populationSize) : base(id)
    {
        SettlementId = settlementId;
        Name = name;
        PopulationSize = populationSize;
    }

    public static Result<PopulationGroup> Create(
        int settlementId,
        string name,
        int populationSize,
        IEnumerable<(int ProductTypeId, decimal AmountPerPersonPerTick)> consumptionProfile)
    {
        var validation = Validate(settlementId, name, populationSize, consumptionProfile);
        if (!validation.IsSuccess)
            return Result<PopulationGroup>.Failure(validation.Error!);

        var group = new PopulationGroup(0, settlementId, name, populationSize);
        var profileResult = group.ReplaceConsumptionProfile(consumptionProfile);
        if (!profileResult.IsSuccess)
            return Result<PopulationGroup>.Failure(profileResult.Error!);

        return Result<PopulationGroup>.Success(group);
    }

    public Result Update(
        string name,
        int populationSize,
        IEnumerable<(int ProductTypeId, decimal AmountPerPersonPerTick)> consumptionProfile)
    {
        var validation = Validate(SettlementId, name, populationSize, consumptionProfile);
        if (!validation.IsSuccess)
            return validation;

        Name = name;
        PopulationSize = populationSize;
        return ReplaceConsumptionProfile(consumptionProfile);
    }

    public IReadOnlyDictionary<int, decimal> CalculateDemand()
    {
        return _consumptionProfile
            .GroupBy(item => item.ProductTypeId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(item => item.AmountPerPersonPerTick * PopulationSize));
    }

    internal void LoadConsumptionProfile(IEnumerable<ConsumptionProfileItem> consumptionProfile)
    {
        _consumptionProfile.Clear();
        _consumptionProfile.AddRange(consumptionProfile);
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
        IEnumerable<(int ProductTypeId, decimal AmountPerPersonPerTick)> consumptionProfile)
    {
        if (settlementId <= 0)
            return Result.Failure("Settlement id must be greater than zero");

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Population group name cannot be empty");

        if (populationSize < 0)
            return Result.Failure("Population size cannot be negative");

        if (consumptionProfile.Any(item => item.AmountPerPersonPerTick < 0))
            return Result.Failure("Consumption amount cannot be negative");

        return Result.Success();
    }
}
