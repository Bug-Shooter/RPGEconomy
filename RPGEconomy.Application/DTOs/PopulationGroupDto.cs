namespace RPGEconomy.Application.DTOs;

public record PopulationGroupDto(
    int Id,
    int SettlementId,
    string Name,
    int PopulationSize,
    decimal ReserveCoverageTicks,
    IReadOnlyList<ConsumptionProfileItemDto> ConsumptionProfile,
    IReadOnlyList<ReserveStockItemDto> StockItems);
