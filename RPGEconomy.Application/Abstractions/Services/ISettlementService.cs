using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface ISettlementService
{
    Task<Result<SettlementSummaryDto>> CreateAsync(int worldId, string name, int population);
    Task<Result<SettlementSummaryDto>> GetByIdAsync(int id);
    Task<Result<IReadOnlyList<SettlementSummaryDto>>> GetByWorldIdAsync(int worldId);
    Task<Result<SettlementSummaryDto>> UpdateAsync(int id, string name, int population);
    Task<Result> DeleteAsync(int id);
}
