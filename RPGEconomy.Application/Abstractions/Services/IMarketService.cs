using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface IMarketService
{
    Task<Result<IReadOnlyList<MarketPriceDto>>> GetPricesAsync(int settlementId);
    Task<Result<MarketPriceDto>> GetProductAsync(int settlementId, int productTypeId);
    Task<Result<MarketPriceDto>> UpdateProductStateAsync(int settlementId, int productTypeId, int supply, int demand);
    Task<Result> RegisterProductAsync(int settlementId, int productTypeId, decimal initialPrice);
}
