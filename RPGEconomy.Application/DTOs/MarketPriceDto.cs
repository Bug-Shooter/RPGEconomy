namespace RPGEconomy.Application.DTOs;

public record MarketPriceDto(
    int ProductTypeId,
    string ProductName,
    decimal Price,
    decimal Supply,
    decimal Demand
);
