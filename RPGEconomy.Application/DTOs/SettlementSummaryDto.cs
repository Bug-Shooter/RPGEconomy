namespace RPGEconomy.Application.DTOs;

public record SettlementSummaryDto(
    int SettlementId,
    string Name,
    IReadOnlyList<InventoryItemDto> Warehouse,
    IReadOnlyList<MarketPriceDto> Prices
);

