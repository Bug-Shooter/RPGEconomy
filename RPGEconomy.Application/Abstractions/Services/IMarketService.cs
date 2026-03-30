using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface IMarketService
{
    Task<Result<IReadOnlyList<MarketPriceDto>>> GetPricesAsync(int settlementId);
    Task<Result<MarketPriceDto>> GetProductAsync(int settlementId, int productTypeId);
    Task<Result<MarketPriceDto>> UpdateProductStateAsync(int settlementId, int productTypeId, decimal supply, decimal demand);
    Task<Result> RegisterProductAsync(int settlementId, int productTypeId, decimal initialPrice);
}
