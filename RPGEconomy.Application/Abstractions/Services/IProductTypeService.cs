using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface IProductTypeService
{
    Task<Result<ProductTypeDto>> GetByIdAsync(int id);
    Task<Result<IReadOnlyList<ProductTypeDto>>> GetAllAsync();
    Task<Result<ProductTypeDto>> CreateAsync(string name, string description, decimal basePrice, double weightPerUnit);
    Task<Result<ProductTypeDto>> UpdateAsync(int id, string name, string description, decimal basePrice, double weightPerUnit);
    Task<Result> DeleteAsync(int id);
}
