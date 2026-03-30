namespace RPGEconomy.Application.DTOs;

public record SimulationSettlementDto(
    int SettlementId,
    string Name,
    int Population,
    IReadOnlyList<InventoryItemDto> Warehouse,
    IReadOnlyList<MarketPriceDto> Prices
);
