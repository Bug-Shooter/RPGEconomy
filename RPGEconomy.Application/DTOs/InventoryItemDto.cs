namespace RPGEconomy.Application.DTOs;

public record InventoryItemDto(
    int ProductTypeId,
    string ProductName,
    int Quantity,
    string Quality
);
