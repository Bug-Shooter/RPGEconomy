using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Markets;
using RPGEconomy.Domain.Population;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.World;

namespace RPGEconomy.Application.Services;

public class SettlementService : ISettlementService
{
    private readonly ISettlementRepository _settlementRepo;
    private readonly IWorldRepository _worldRepo;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly IMarketRepository _marketRepo;
    private readonly IPopulationGroupRepository _populationGroupRepo;
    private readonly IProductTypeRepository _productTypeRepo;

    public SettlementService(
        ISettlementRepository settlementRepo,
        IWorldRepository worldRepo,
        IWarehouseRepository warehouseRepo,
        IMarketRepository marketRepo,
        IPopulationGroupRepository populationGroupRepo,
        IProductTypeRepository productTypeRepo)
    {
        _settlementRepo = settlementRepo;
        _worldRepo = worldRepo;
        _warehouseRepo = warehouseRepo;
        _marketRepo = marketRepo;
        _populationGroupRepo = populationGroupRepo;
        _productTypeRepo = productTypeRepo;
    }

    public async Task<Result<SettlementDetailsDto>> CreateAsync(int worldId, string name)
    {
        var world = await _worldRepo.GetByIdAsync(worldId);
        if (world is null)
            return Result<SettlementDetailsDto>.Failure($"Мир с Id {worldId} не найден");

        var createResult = Settlement.Create(name, worldId);
        if (!createResult.IsSuccess)
            return Result<SettlementDetailsDto>.Failure(createResult.Error!);

        var settlement = createResult.Value!;
        var settlementId = 0;
        try
        {
            settlementId = await _settlementRepo.SaveAsync(settlement);
            await _warehouseRepo.SaveAsync(Warehouse.Create(settlementId));
            await _marketRepo.SaveAsync(Market.Create(settlementId));
        }
        catch
        {
            if (settlementId > 0)
                await _settlementRepo.DeleteAsync(settlementId);

            throw;
        }

        return await GetByIdAsync(settlementId);
    }

    public async Task<Result<SettlementDetailsDto>> GetByIdAsync(int id)
    {
        var settlement = await _settlementRepo.GetByIdAsync(id);
        if (settlement is null)
            return Result<SettlementDetailsDto>.Failure($"Поселение с Id {id} не найдено");

        var warehouse = await _warehouseRepo.GetBySettlementIdAsync(id);
        if (warehouse is null)
            return Result<SettlementDetailsDto>.Failure($"Для поселения с Id {id} не найден склад");

        var market = await _marketRepo.GetBySettlementIdAsync(id);
        if (market is null)
            return Result<SettlementDetailsDto>.Failure($"Для поселения с Id {id} не найден рынок");

        var population = await GetPopulationAsync(id);
        var productNames = await GetProductNamesAsync();

        return Result<SettlementDetailsDto>.Success(
            new SettlementDetailsDto(
                settlement.Id,
                settlement.Name,
                population,
                MapInventory(warehouse, productNames),
                MapPrices(market, productNames)));
    }

    public async Task<Result<IReadOnlyList<SettlementListItemDto>>> GetByWorldIdAsync(int worldId)
    {
        var world = await _worldRepo.GetByIdAsync(worldId);
        if (world is null)
            return Result<IReadOnlyList<SettlementListItemDto>>.Failure($"Мир с Id {worldId} не найден");

        var settlements = await _settlementRepo.GetByWorldIdAsync(worldId);
        var items = new List<SettlementListItemDto>(settlements.Count);
        foreach (var settlement in settlements)
        {
            items.Add(new SettlementListItemDto(
                settlement.Id,
                settlement.Name,
                await GetPopulationAsync(settlement.Id)));
        }

        return Result<IReadOnlyList<SettlementListItemDto>>.Success(items.AsReadOnly());
    }

    public async Task<Result<SettlementDetailsDto>> UpdateAsync(int id, string name)
    {
        var settlement = await _settlementRepo.GetByIdAsync(id);
        if (settlement is null)
            return Result<SettlementDetailsDto>.Failure($"Поселение с Id {id} не найдено");

        var updateResult = settlement.Update(name);
        if (!updateResult.IsSuccess)
            return Result<SettlementDetailsDto>.Failure(updateResult.Error!);

        await _settlementRepo.SaveAsync(settlement);
        return await GetByIdAsync(id);
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var settlement = await _settlementRepo.GetByIdAsync(id);
        if (settlement is null)
            return Result.Failure($"Поселение с Id {id} не найдено");

        await _settlementRepo.DeleteAsync(id);
        return Result.Success();
    }

    private async Task<int> GetPopulationAsync(int settlementId)
    {
        var groups = await _populationGroupRepo.GetBySettlementIdAsync(settlementId);
        return groups.Sum(group => group.PopulationSize);
    }

    private async Task<IReadOnlyDictionary<int, string>> GetProductNamesAsync()
    {
        var products = await _productTypeRepo.GetAllAsync();
        return products.ToDictionary(product => product.Id, product => product.Name);
    }

    private static IReadOnlyList<InventoryItemDto> MapInventory(
        Warehouse warehouse,
        IReadOnlyDictionary<int, string> productNames) =>
        warehouse.Items
            .Select(item => new InventoryItemDto(
                item.ProductTypeId,
                ResolveProductName(productNames, item.ProductTypeId),
                item.Quantity,
                item.Quality))
            .ToList()
            .AsReadOnly();

    private static IReadOnlyList<MarketPriceDto> MapPrices(
        Market market,
        IReadOnlyDictionary<int, string> productNames) =>
        market.Offers
            .Select(offer => new MarketPriceDto(
                offer.ProductTypeId,
                ResolveProductName(productNames, offer.ProductTypeId),
                offer.CurrentPrice,
                offer.SupplyVolume,
                offer.DemandVolume))
            .ToList()
            .AsReadOnly();

    private static string ResolveProductName(
        IReadOnlyDictionary<int, string> productNames,
        int productTypeId) =>
        productNames.GetValueOrDefault(productTypeId, "Неизвестный товар");
}
