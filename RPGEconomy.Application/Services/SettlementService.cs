using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Markets;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.World;

namespace RPGEconomy.Application.Services;

public class SettlementService : ISettlementService
{
    private readonly ISettlementRepository _settlementRepo;
    private readonly IWorldRepository _worldRepo;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly IMarketRepository _marketRepo;

    public SettlementService(
        ISettlementRepository settlementRepo,
        IWorldRepository worldRepo,
        IWarehouseRepository warehouseRepo,
        IMarketRepository marketRepo)
    {
        _settlementRepo = settlementRepo;
        _worldRepo = worldRepo;
        _warehouseRepo = warehouseRepo;
        _marketRepo = marketRepo;
    }

    public async Task<Result<SettlementSummaryDto>> CreateAsync(
        int worldId, string name, int population)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<SettlementSummaryDto>.Failure("Название поселения не может быть пустым");

        if (population <= 0)
            return Result<SettlementSummaryDto>.Failure("Население должно быть больше нуля");

        var world = await _worldRepo.GetByIdAsync(worldId);
        if (world is null)
            return Result<SettlementSummaryDto>.Failure($"Мир с Id {worldId} не найден");

        // Создаём поселение
        var settlement = Settlement.Create(name, worldId, population);
        var settlementId = await _settlementRepo.SaveAsync(settlement);

        // Автоматически создаём склад и рынок для поселения
        var warehouse = Warehouse.Create(settlementId);
        await _warehouseRepo.SaveAsync(warehouse);

        var market = Market.Create(settlementId);
        await _marketRepo.SaveAsync(market);

        return Result<SettlementSummaryDto>.Success(
            new SettlementSummaryDto(settlementId, name, population, [], []));
    }

    public async Task<Result<SettlementSummaryDto>> GetByIdAsync(int id)
    {
        var settlement = await _settlementRepo.GetByIdAsync(id);
        if (settlement is null)
            return Result<SettlementSummaryDto>.Failure($"Поселение с Id {id} не найдено");

        var warehouse = await _warehouseRepo.GetBySettlementIdAsync(id);
        var market = await _marketRepo.GetBySettlementIdAsync(id);

        var inventoryItems = warehouse?.Items
            .Select(i => new InventoryItemDto(i.ProductTypeId, string.Empty, i.Quantity, i.Quality))
            .ToList()
            .AsReadOnly() ?? (IReadOnlyList<InventoryItemDto>)[];

        var prices = market?.Offers
            .Select(o => new MarketPriceDto(o.ProductTypeId, string.Empty, o.CurrentPrice, o.SupplyVolume, o.DemandVolume))
            .ToList()
            .AsReadOnly() ?? (IReadOnlyList<MarketPriceDto>)[];

        return Result<SettlementSummaryDto>.Success(
            new SettlementSummaryDto(settlement.Id, settlement.Name, settlement.Population, inventoryItems, prices));
    }

    public async Task<Result<IReadOnlyList<SettlementSummaryDto>>> GetByWorldIdAsync(int worldId)
    {
        var world = await _worldRepo.GetByIdAsync(worldId);
        if (world is null)
            return Result<IReadOnlyList<SettlementSummaryDto>>.Failure($"Мир с Id {worldId} не найден");

        var settlements = await _settlementRepo.GetByWorldIdAsync(worldId);

        var dtos = settlements
            .Select(s => new SettlementSummaryDto(s.Id, s.Name, s.Population, [], []))
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<SettlementSummaryDto>>.Success(dtos);
    }

    public async Task<Result<SettlementSummaryDto>> UpdateAsync(int id, string name, int population)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<SettlementSummaryDto>.Failure("Название поселения не может быть пустым");

        if (population <= 0)
            return Result<SettlementSummaryDto>.Failure("Население должно быть больше нуля");

        var settlement = await _settlementRepo.GetByIdAsync(id);
        if (settlement is null)
            return Result<SettlementSummaryDto>.Failure($"Поселение с Id {id} не найдено");

        settlement.Update(name, population);
        await _settlementRepo.SaveAsync(settlement);

        return Result<SettlementSummaryDto>.Success(
            new SettlementSummaryDto(settlement.Id, settlement.Name, settlement.Population, [], []));
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var settlement = await _settlementRepo.GetByIdAsync(id);
        if (settlement is null)
            return Result.Failure($"Поселение с Id {id} не найдено");

        await _settlementRepo.DeleteAsync(id);
        return Result.Success();
    }
}
