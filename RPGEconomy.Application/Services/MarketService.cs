using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Services;

public class MarketService : IMarketService
{
    private readonly IMarketRepository _marketRepo;
    private readonly IProductTypeRepository _productTypeRepo;

    public MarketService(IMarketRepository marketRepo, IProductTypeRepository productTypeRepo)
    {
        _marketRepo = marketRepo;
        _productTypeRepo = productTypeRepo;
    }

    public async Task<Result<IReadOnlyList<MarketPriceDto>>> GetPricesAsync(int settlementId)
    {
        var market = await _marketRepo.GetBySettlementIdAsync(settlementId);
        if (market is null)
            return Result<IReadOnlyList<MarketPriceDto>>.Failure("Рынок не найден");

        // Подтягиваем имена товаров
        var productIds = market.Offers.Select(o => o.ProductTypeId).ToList();
        var productNames = new Dictionary<int, string>();

        foreach (var pid in productIds)
        {
            var product = await _productTypeRepo.GetByIdAsync(pid);
            if (product is not null)
                productNames[pid] = product.Name;
        }

        var prices = market.Offers
            .Select(o => new MarketPriceDto(
                o.ProductTypeId,
                productNames.GetValueOrDefault(o.ProductTypeId, "Unknown"),
                o.CurrentPrice,
                o.SupplyVolume,
                o.DemandVolume))
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<MarketPriceDto>>.Success(prices);
    }

    public async Task<Result> RegisterProductAsync(
        int settlementId, int productTypeId, double initialPrice)
    {
        if (initialPrice <= 0)
            return Result.Failure("Начальная цена должна быть больше нуля");

        var product = await _productTypeRepo.GetByIdAsync(productTypeId);
        if (product is null)
            return Result.Failure($"Тип товара с Id {productTypeId} не найден");

        var market = await _marketRepo.GetBySettlementIdAsync(settlementId);
        if (market is null)
            return Result.Failure("Рынок не найден");

        var result = market.RegisterProduct(productTypeId, initialPrice);
        if (!result.IsSuccess) return result;

        await _marketRepo.SaveAsync(market);
        return Result.Success();
    }
}
