using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface IResourceTypeService
{
    Task<Result<ResourceTypeDto>> GetByIdAsync(int id);
    Task<Result<IReadOnlyList<ResourceTypeDto>>> GetAllAsync();
    Task<Result<ResourceTypeDto>> CreateAsync(string name, string description, bool isRenewable, double regenerationRatePerDay);
    Task<Result<ResourceTypeDto>> UpdateAsync(int id, string name, string description, bool isRenewable, double regenerationRatePerDay);
    Task<Result> DeleteAsync(int id);
}
