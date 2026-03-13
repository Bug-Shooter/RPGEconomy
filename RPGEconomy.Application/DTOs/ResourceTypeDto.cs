namespace RPGEconomy.Application.DTOs;

public record ResourceTypeDto(
    int Id,
    string Name,
    string Description,
    bool IsRenewable,
    double RegenerationRatePerDay);