namespace RPGEconomy.Application.DTOs;

public record BuildingDto(
    int Id,
    string Name,
    int SettlementId,
    int RecipeId,
    int WorkerCount,
    bool IsActive,
    decimal InputReserveCoverageTicks,
    IReadOnlyList<ReserveStockItemDto> InputReserveStock);
