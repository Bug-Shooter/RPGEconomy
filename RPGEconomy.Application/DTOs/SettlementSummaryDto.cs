namespace RPGEconomy.Application.DTOs;

public record SettlementSummaryDto(
    int SettlementId,
    string Name,
    int Population,
    IReadOnlyList<InventoryItemDto> Warehouse,
    IReadOnlyList<MarketPriceDto> Prices
);

