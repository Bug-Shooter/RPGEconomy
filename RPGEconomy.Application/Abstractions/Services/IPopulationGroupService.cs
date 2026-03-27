using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface IPopulationGroupService
{
    Task<Result<IReadOnlyList<PopulationGroupDto>>> GetBySettlementIdAsync(int settlementId);
    Task<Result<PopulationGroupDto>> GetByIdAsync(int id);
    Task<Result<PopulationGroupDto>> CreateAsync(
        int settlementId,
        string name,
        int populationSize,
        IReadOnlyList<ConsumptionProfileItemDto> consumptionProfile);
    Task<Result<PopulationGroupDto>> UpdateAsync(
        int id,
        string name,
        int populationSize,
        IReadOnlyList<ConsumptionProfileItemDto> consumptionProfile);
    Task<Result> DeleteAsync(int id);
}
