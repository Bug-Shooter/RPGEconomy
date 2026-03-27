using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface ICurrencyService
{
    Task<Result<CurrencyDto>> GetByIdAsync(int id);
    Task<Result<IReadOnlyList<CurrencyDto>>> GetAllAsync();
    Task<Result<CurrencyDto>> CreateAsync(string name, string code, decimal exchangeRateToBase);
    Task<Result<CurrencyDto>> UpdateAsync(int id, string name, string code, decimal exchangeRateToBase);
    Task<Result> DeleteAsync(int id);
}
