namespace RPGEconomy.Application.DTOs;

public record EconomicEventDto(
    int Id,
    int SettlementId,
    string Name,
    bool IsEnabled,
    int StartDay,
    int? EndDay,
    IReadOnlyList<EconomicEffectDto> Effects);
