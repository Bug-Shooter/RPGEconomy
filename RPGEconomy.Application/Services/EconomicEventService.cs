using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Events;

namespace RPGEconomy.Application.Services;

public class EconomicEventService : IEconomicEventService
{
    private readonly IEconomicEventRepository _economicEventRepository;
    private readonly ISettlementRepository _settlementRepository;
    private readonly IPopulationGroupRepository _populationGroupRepository;
    private readonly IProductTypeRepository _productTypeRepository;

    public EconomicEventService(
        IEconomicEventRepository economicEventRepository,
        ISettlementRepository settlementRepository,
        IPopulationGroupRepository populationGroupRepository,
        IProductTypeRepository productTypeRepository)
    {
        _economicEventRepository = economicEventRepository;
        _settlementRepository = settlementRepository;
        _populationGroupRepository = populationGroupRepository;
        _productTypeRepository = productTypeRepository;
    }

    public async Task<Result<IReadOnlyList<EconomicEventDto>>> GetBySettlementIdAsync(int settlementId)
    {
        var settlement = await _settlementRepository.GetByIdAsync(settlementId);
        if (settlement is null)
            return Result<IReadOnlyList<EconomicEventDto>>.Failure($"Settlement with Id {settlementId} was not found");

        var items = await _economicEventRepository.GetBySettlementIdAsync(settlementId);
        return Result<IReadOnlyList<EconomicEventDto>>.Success(items.Select(ToDto).ToList().AsReadOnly());
    }

    public async Task<Result<EconomicEventDto>> GetByIdAsync(int id)
    {
        var economicEvent = await _economicEventRepository.GetByIdAsync(id);
        if (economicEvent is null)
            return Result<EconomicEventDto>.Failure($"Economic event with Id {id} was not found");

        return Result<EconomicEventDto>.Success(ToDto(economicEvent));
    }

    public async Task<Result<EconomicEventDto>> CreateAsync(
        int settlementId,
        string name,
        bool isEnabled,
        int startDay,
        int? endDay,
        IReadOnlyList<EconomicEffectDto> effects)
    {
        var settlement = await _settlementRepository.GetByIdAsync(settlementId);
        if (settlement is null)
            return Result<EconomicEventDto>.Failure($"Settlement with Id {settlementId} was not found");

        var validation = await ValidateEffectsAsync(settlementId, effects);
        if (!validation.IsSuccess)
            return Result<EconomicEventDto>.Failure(validation.Error!);

        var createResult = EconomicEvent.Create(
            settlementId,
            name,
            isEnabled,
            startDay,
            endDay,
            effects.Select(ToDomainTuple));

        if (!createResult.IsSuccess)
            return Result<EconomicEventDto>.Failure(createResult.Error!);

        var id = await _economicEventRepository.SaveAsync(createResult.Value!);
        var saved = await _economicEventRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Created economic event was not found");

        return Result<EconomicEventDto>.Success(ToDto(saved));
    }

    public async Task<Result<EconomicEventDto>> UpdateAsync(
        int id,
        string name,
        bool isEnabled,
        int startDay,
        int? endDay,
        IReadOnlyList<EconomicEffectDto> effects)
    {
        var economicEvent = await _economicEventRepository.GetByIdAsync(id);
        if (economicEvent is null)
            return Result<EconomicEventDto>.Failure($"Economic event with Id {id} was not found");

        var validation = await ValidateEffectsAsync(economicEvent.SettlementId, effects);
        if (!validation.IsSuccess)
            return Result<EconomicEventDto>.Failure(validation.Error!);

        var updateResult = economicEvent.Update(name, isEnabled, startDay, endDay, effects.Select(ToDomainTuple));
        if (!updateResult.IsSuccess)
            return Result<EconomicEventDto>.Failure(updateResult.Error!);

        await _economicEventRepository.SaveAsync(economicEvent);
        return Result<EconomicEventDto>.Success(ToDto(economicEvent));
    }

    public async Task<Result> ActivateAsync(int id)
    {
        var economicEvent = await _economicEventRepository.GetByIdAsync(id);
        if (economicEvent is null)
            return Result.Failure($"Economic event with Id {id} was not found");

        var result = economicEvent.Activate();
        if (!result.IsSuccess)
            return result;

        await _economicEventRepository.SaveAsync(economicEvent);
        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(int id)
    {
        var economicEvent = await _economicEventRepository.GetByIdAsync(id);
        if (economicEvent is null)
            return Result.Failure($"Economic event with Id {id} was not found");

        var result = economicEvent.Deactivate();
        if (!result.IsSuccess)
            return result;

        await _economicEventRepository.SaveAsync(economicEvent);
        return Result.Success();
    }

    private async Task<Result> ValidateEffectsAsync(int settlementId, IEnumerable<EconomicEffectDto> effects)
    {
        foreach (var effect in effects)
        {
            if (effect.PopulationGroupId.HasValue)
            {
                var group = await _populationGroupRepository.GetByIdAsync(effect.PopulationGroupId.Value);
                if (group is null)
                    return Result.Failure($"Population group with Id {effect.PopulationGroupId.Value} was not found");

                if (group.SettlementId != settlementId)
                    return Result.Failure("Economic effect population group must belong to the same settlement");
            }

            if (effect.ProductTypeId.HasValue)
            {
                var productType = await _productTypeRepository.GetByIdAsync(effect.ProductTypeId.Value);
                if (productType is null)
                    return Result.Failure($"Product type with Id {effect.ProductTypeId.Value} was not found");
            }
        }

        return Result.Success();
    }

    private static (EconomicEffectType EffectType, decimal Value, int? PopulationGroupId, int? ProductTypeId) ToDomainTuple(
        EconomicEffectDto effect) =>
        (effect.EffectType, effect.Value, effect.PopulationGroupId, effect.ProductTypeId);

    private static EconomicEventDto ToDto(EconomicEvent economicEvent) =>
        new(
            economicEvent.Id,
            economicEvent.SettlementId,
            economicEvent.Name,
            economicEvent.IsEnabled,
            economicEvent.StartDay,
            economicEvent.EndDay,
            economicEvent.Effects
                .Select(effect => new EconomicEffectDto(
                    effect.EffectType,
                    effect.Value,
                    effect.PopulationGroupId,
                    effect.ProductTypeId))
                .ToList()
                .AsReadOnly());
}
