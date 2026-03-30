using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Events;

public class EconomicEvent : AggregateRoot
{
    private readonly List<EconomicEffect> _effects = [];

    public int SettlementId { get; private set; }
    public string Name { get; private set; }
    public bool IsEnabled { get; private set; }
    public int StartDay { get; private set; }
    public int? EndDay { get; private set; }
    public IReadOnlyList<EconomicEffect> Effects => _effects.AsReadOnly();

    public EconomicEvent(
        int id,
        int settlementId,
        string name,
        bool isEnabled,
        int startDay,
        int? endDay) : base(id)
    {
        SettlementId = settlementId;
        Name = name;
        IsEnabled = isEnabled;
        StartDay = startDay;
        EndDay = endDay;
    }

    public static Result<EconomicEvent> Create(
        int settlementId,
        string name,
        bool isEnabled,
        int startDay,
        int? endDay,
        IEnumerable<(EconomicEffectType EffectType, decimal Value, int? PopulationGroupId, int? ProductTypeId)> effects)
    {
        var effectList = effects.ToList();
        var validation = Validate(settlementId, name, startDay, endDay, effectList);
        if (!validation.IsSuccess)
            return Result<EconomicEvent>.Failure(validation.Error!);

        var economicEvent = new EconomicEvent(0, settlementId, name, isEnabled, startDay, endDay);
        var replaceResult = economicEvent.ReplaceEffects(effectList);
        if (!replaceResult.IsSuccess)
            return Result<EconomicEvent>.Failure(replaceResult.Error!);

        return Result<EconomicEvent>.Success(economicEvent);
    }

    public Result Update(
        string name,
        bool isEnabled,
        int startDay,
        int? endDay,
        IEnumerable<(EconomicEffectType EffectType, decimal Value, int? PopulationGroupId, int? ProductTypeId)> effects)
    {
        var effectList = effects.ToList();
        var validation = Validate(SettlementId, name, startDay, endDay, effectList);
        if (!validation.IsSuccess)
            return validation;

        Name = name;
        IsEnabled = isEnabled;
        StartDay = startDay;
        EndDay = endDay;
        return ReplaceEffects(effectList);
    }

    public Result Activate()
    {
        if (IsEnabled)
            return Result.Failure("Economic event is already active");

        IsEnabled = true;
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsEnabled)
            return Result.Failure("Economic event is already inactive");

        IsEnabled = false;
        return Result.Success();
    }

    public IReadOnlyList<EconomicEffect> GetActiveEffects(int currentDay)
    {
        if (!IsEnabled)
            return [];

        if (currentDay < StartDay)
            return [];

        if (EndDay.HasValue && currentDay > EndDay.Value)
            return [];

        return Effects;
    }

    internal void LoadEffects(IEnumerable<EconomicEffect> effects)
    {
        _effects.Clear();
        _effects.AddRange(effects);
    }

    private Result ReplaceEffects(
        IEnumerable<(EconomicEffectType EffectType, decimal Value, int? PopulationGroupId, int? ProductTypeId)> effects)
    {
        var effectList = effects.ToList();
        if (effectList
            .GroupBy(effect => new { effect.EffectType, effect.PopulationGroupId, effect.ProductTypeId })
            .Any(group => group.Count() > 1))
        {
            return Result.Failure("Событие не может содержать дублирующиеся экономические эффекты");
        }

        var effectEntities = new List<EconomicEffect>();
        foreach (var effect in effectList)
        {
            var createResult = EconomicEffect.Create(
                Id,
                effect.EffectType,
                effect.Value,
                effect.PopulationGroupId,
                effect.ProductTypeId);

            if (!createResult.IsSuccess)
                return Result.Failure(createResult.Error!);

            effectEntities.Add(createResult.Value!);
        }

        LoadEffects(effectEntities);
        return Result.Success();
    }

    private static Result Validate(
        int settlementId,
        string name,
        int startDay,
        int? endDay,
        IReadOnlyList<(EconomicEffectType EffectType, decimal Value, int? PopulationGroupId, int? ProductTypeId)> effects)
    {
        if (settlementId <= 0)
            return Result.Failure("Settlement id must be greater than zero");

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Economic event name cannot be empty");

        if (startDay < 0)
            return Result.Failure("Economic event start day cannot be negative");

        if (endDay.HasValue && endDay.Value < startDay)
            return Result.Failure("Economic event end day cannot be earlier than start day");

        if (effects.Count == 0)
            return Result.Failure("Economic event must contain at least one effect");

        return Result.Success();
    }
}
