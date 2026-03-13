namespace RPGEconomy.Application.DTOs;

public record WorldDto(
    int Id,
    string Name,
    string Description,
    int CurrentDay
);
