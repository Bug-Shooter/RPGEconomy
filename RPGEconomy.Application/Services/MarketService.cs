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

        var prices = market.Offers
            .Select(BuildPriceDtoAsync)
            .ToList();

        var resolvedPrices = await Task.WhenAll(prices);

        return Result<IReadOnlyList<MarketPriceDto>>.Success(resolvedPrices.ToList().AsReadOnly());
    }

    public async Task<Result<MarketPriceDto>> GetProductAsync(int settlementId, int productTypeId)
    {
        var market = await _marketRepo.GetBySettlementIdAsync(settlementId);
        if (market is null)
            return Result<MarketPriceDto>.Failure("Рынок не найден");

        var offer = market.GetOffer(productTypeId);
        if (offer is null)
            return Result<MarketPriceDto>.Failure($"Товар с Id {productTypeId} не найден на рынке");

        return Result<MarketPriceDto>.Success(await BuildPriceDtoAsync(offer));
    }

    public async Task<Result<MarketPriceDto>> UpdateProductStateAsync(
        int settlementId,
        int productTypeId,
        int supply,
        int demand)
    {
        var market = await _marketRepo.GetBySettlementIdAsync(settlementId);
        if (market is null)
            return Result<MarketPriceDto>.Failure("Рынок не найден");

        var result = market.UpdateProductState(productTypeId, supply, demand);
        if (!result.IsSuccess)
            return Result<MarketPriceDto>.Failure(result.Error!);

        await _marketRepo.SaveAsync(market);
        return await GetProductAsync(settlementId, productTypeId);
    }

    public async Task<Result> RegisterProductAsync(
        int settlementId, int productTypeId, decimal initialPrice)
    {
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

    private async Task<MarketPriceDto> BuildPriceDtoAsync(RPGEconomy.Domain.Markets.MarketOffer offer)
    {
        var product = await _productTypeRepo.GetByIdAsync(offer.ProductTypeId);
        return new MarketPriceDto(
            offer.ProductTypeId,
            product?.Name ?? "Unknown",
            offer.CurrentPrice,
            offer.SupplyVolume,
            offer.DemandVolume);
    }
}
