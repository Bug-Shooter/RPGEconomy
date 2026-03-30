namespace RPGEconomy.Application.DTOs;

public record CurrencyDto(
    int Id,
    string Name,
    string Code,
    decimal ExchangeRateToBase);
