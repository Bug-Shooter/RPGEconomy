namespace RPGEconomy.Application.DTOs;

public record ProductTypeDto(
    int Id,
    string Name,
    string Description,
    double BasePrice,
    double WeightPerUnit);
