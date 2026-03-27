namespace RPGEconomy.Application.DTOs;

public record MarketPriceDto(
    int ProductTypeId,
    string ProductName,
    decimal Price,
    int Supply,
    int Demand
);
