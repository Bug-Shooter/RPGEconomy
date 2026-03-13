namespace RPGEconomy.Application.DTOs;

public record SimulationResultDto(
    int WorldId,
    int DaysBefore,
    int DaysAfter,
    IReadOnlyList<SettlementSummaryDto> Settlements
);
