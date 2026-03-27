using RPGEconomy.Domain.Events;

namespace RPGEconomy.Application.DTOs;

public record EconomicEffectDto(
    EconomicEffectType EffectType,
    decimal Value,
    int? PopulationGroupId,
    int? ProductTypeId);
