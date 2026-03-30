namespace RPGEconomy.Application.DTOs;

public record SettlementDetailsDto(
    int SettlementId,
    string Name,
    int Population,
    IReadOnlyList<InventoryItemDto> Warehouse,
    IReadOnlyList<MarketPriceDto> Prices
);
