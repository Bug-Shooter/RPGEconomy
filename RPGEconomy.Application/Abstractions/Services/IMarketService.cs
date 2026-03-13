using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface IMarketService
{
    Task<Result<IReadOnlyList<MarketPriceDto>>> GetPricesAsync(int settlementId);
    Task<Result> RegisterProductAsync(int settlementId, int productTypeId, double initialPrice);
}
