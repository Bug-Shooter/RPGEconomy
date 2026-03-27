using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Events;

public class EconomicEffect : Entity
{
    public int EconomicEventId { get; private set; }
    public EconomicEffectType EffectType { get; private set; }
    public decimal Value { get; private set; }
    public int? PopulationGroupId { get; private set; }
    public int? ProductTypeId { get; private set; }

    public EconomicEffect(
        int id,
        int economicEventId,
        EconomicEffectType effectType,
        decimal value,
        int? populationGroupId,
        int? productTypeId) : base(id)
    {
        EconomicEventId = economicEventId;
        EffectType = effectType;
        Value = value;
        PopulationGroupId = populationGroupId;
        ProductTypeId = productTypeId;
    }

    public static Result<EconomicEffect> Create(
        int economicEventId,
        EconomicEffectType effectType,
        decimal value,
        int? populationGroupId,
        int? productTypeId)
    {
        if (value <= 0m)
            return Result<EconomicEffect>.Failure("Economic effect value must be greater than zero");

        if (populationGroupId <= 0)
            populationGroupId = null;

        if (productTypeId <= 0)
            productTypeId = null;

        if (effectType == EconomicEffectType.ProducerReserveCoverageMultiplier && populationGroupId.HasValue)
        {
            return Result<EconomicEffect>.Failure(
                "Producer reserve coverage multiplier cannot target a population group");
        }

        return Result<EconomicEffect>.Success(
            new EconomicEffect(0, economicEventId, effectType, value, populationGroupId, productTypeId));
    }
}
