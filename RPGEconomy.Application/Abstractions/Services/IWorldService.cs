using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface IWorldService
{
    Task<Result<WorldDto>> CreateAsync(string name, string description);
    Task<Result<WorldDto>> GetByIdAsync(int id);
    Task<Result<IReadOnlyList<WorldDto>>> GetAllAsync();
    Task<Result<WorldDto>> UpdateAsync(int id, string name, string description);
    Task<Result> DeleteAsync(int id);
}
