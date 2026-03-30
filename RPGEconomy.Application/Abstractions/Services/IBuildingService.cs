using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface IBuildingService
{
    Task<Result<BuildingDto>> GetByIdAsync(int id);
    Task<Result<IReadOnlyList<BuildingDto>>> GetBySettlementIdAsync(int settlementId);
    Task<Result<BuildingDto>> CreateAsync(int settlementId, string name, int recipeId, int workerCount, decimal inputReserveCoverageTicks);
    Task<Result<BuildingDto>> UpdateAsync(int id, string name, int workerCount, decimal inputReserveCoverageTicks);
    Task<Result> ActivateAsync(int id);
    Task<Result> DeactivateAsync(int id);
    Task<Result> DeleteAsync(int id);
}
