namespace RPGEconomy.Application.DTOs;

public record PopulationGroupDto(
    int Id,
    int SettlementId,
    string Name,
    int PopulationSize,
    IReadOnlyList<ConsumptionProfileItemDto> ConsumptionProfile);
