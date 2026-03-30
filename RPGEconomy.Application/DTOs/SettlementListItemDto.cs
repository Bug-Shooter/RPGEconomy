namespace RPGEconomy.Application.DTOs;

public record SettlementListItemDto(
    int SettlementId,
    string Name,
    int Population
);
