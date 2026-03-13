namespace RPGEconomy.Application.DTOs;

public record MarketPriceDto(
    int ProductTypeId,
    string ProductName,
    double Price,
    int Supply,
    int Demand
);
