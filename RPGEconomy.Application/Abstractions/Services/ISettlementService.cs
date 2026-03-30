using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface ISettlementService
{
    Task<Result<SettlementDetailsDto>> CreateAsync(int worldId, string name);
    Task<Result<SettlementDetailsDto>> GetByIdAsync(int id);
    Task<Result<IReadOnlyList<SettlementListItemDto>>> GetByWorldIdAsync(int worldId);
    Task<Result<SettlementDetailsDto>> UpdateAsync(int id, string name);
    Task<Result> DeleteAsync(int id);
}
