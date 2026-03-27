using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Population;

public class ConsumptionProfileItem : Entity
{
    public int PopulationGroupId { get; private set; }
    public int ProductTypeId { get; private set; }
    public decimal AmountPerPersonPerTick { get; private set; }

    public ConsumptionProfileItem(
        int id,
        int populationGroupId,
        int productTypeId,
        decimal amountPerPersonPerTick) : base(id)
    {
        PopulationGroupId = populationGroupId;
        ProductTypeId = productTypeId;
        AmountPerPersonPerTick = amountPerPersonPerTick;
    }

    public static Result<ConsumptionProfileItem> Create(
        int populationGroupId,
        int productTypeId,
        decimal amountPerPersonPerTick)
    {
        if (productTypeId <= 0)
            return Result<ConsumptionProfileItem>.Failure("Product type id must be greater than zero");

        if (amountPerPersonPerTick < 0)
            return Result<ConsumptionProfileItem>.Failure("Consumption amount cannot be negative");

        return Result<ConsumptionProfileItem>.Success(
            new ConsumptionProfileItem(0, populationGroupId, productTypeId, amountPerPersonPerTick));
    }
}
