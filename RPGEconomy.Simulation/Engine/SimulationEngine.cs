using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Markets;
using RPGEconomy.Domain.Production;
using RPGEconomy.Simulation.Services;

namespace RPGEconomy.Simulation.Engine;

public class SimulationEngine : ISimulationEngine
{
    private readonly IWorldRepository _worldRepo;
    private readonly ISettlementRepository _settlementRepo;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly IMarketRepository _marketRepo;
    private readonly IBuildingRepository _buildingRepo;
    private readonly IProductionRecipeRepository _recipeRepo;

    private readonly ProductionSimulationService _productionService;
    private readonly MarketSimulationService _marketService;

    public SimulationEngine(
        IWorldRepository worldRepo,
        ISettlementRepository settlementRepo,
        IWarehouseRepository warehouseRepo,
        IMarketRepository marketRepo,
        IBuildingRepository buildingRepo,
        IProductionRecipeRepository recipeRepo,
        ProductionSimulationService productionService,
        MarketSimulationService marketService)
    {
        _worldRepo = worldRepo;
        _settlementRepo = settlementRepo;
        _warehouseRepo = warehouseRepo;
        _marketRepo = marketRepo;
        _buildingRepo = buildingRepo;
        _recipeRepo = recipeRepo;
        _productionService = productionService;
        _marketService = marketService;
    }

    public async Task<Result<SimulationResultDto>> AdvanceAsync(int worldId, int days)
    {
        var world = await _worldRepo.GetByIdAsync(worldId);
        if (world is null)
            return Result<SimulationResultDto>.Failure($"Мир с Id {worldId} не найден");

        var daysBefore = world.CurrentDay;

        // Загружаем контекст один раз
        var ctx = await LoadContextAsync(worldId, world.CurrentDay);

        // Прогоняем N тиков
        for (int i = 0; i < days; i++)
        {
            RunTick(ctx);
        }

        // Сохраняем изменённые агрегаты
        await PersistContextAsync(ctx);

        // Обновляем текущий день мира
        world.AdvanceDays(days);
        await _worldRepo.SaveAsync(world);

        // Формируем результат
        var result = BuildResult(worldId, daysBefore, world.CurrentDay, ctx);
        return Result<SimulationResultDto>.Success(result);
    }

    private void RunTick(SimulationContext ctx)
    {
        // Порядок важен: сначала производство, потом рынок
        _productionService.RunTick(ctx);
        _marketService.RunTick(ctx);
    }

    private async Task<SimulationContext> LoadContextAsync(int worldId, int currentDay)
    {
        var settlements = await _settlementRepo.GetByWorldIdAsync(worldId);

        var warehouses = new Dictionary<int, Warehouse>();
        var markets = new Dictionary<int, Market>();
        var buildings = new Dictionary<int, IReadOnlyList<Building>>();

        foreach (var settlement in settlements)
        {
            var warehouse = await _warehouseRepo.GetBySettlementIdAsync(settlement.Id);
            if (warehouse is not null) warehouses[settlement.Id] = warehouse;

            var market = await _marketRepo.GetBySettlementIdAsync(settlement.Id);
            if (market is not null) markets[settlement.Id] = market;

            var buildingList = await _buildingRepo.GetBySettlementIdAsync(settlement.Id);
            buildings[settlement.Id] = buildingList;
        }

        var allRecipes = await _recipeRepo.GetAllAsync();
        var recipes = allRecipes.ToDictionary(r => r.Id);

        return new SimulationContext(
            worldId, currentDay,
            settlements,
            warehouses,
            markets,
            buildings,
            recipes);
    }

    private async Task PersistContextAsync(SimulationContext ctx)
    {
        foreach (var warehouse in ctx.Warehouses.Values)
            await _warehouseRepo.SaveAsync(warehouse);

        foreach (var market in ctx.Markets.Values)
            await _marketRepo.SaveAsync(market);
    }

    private SimulationResultDto BuildResult(
        int worldId, int daysBefore, int daysAfter, SimulationContext ctx)
    {
        var settlements = ctx.Settlements.Select(s =>
        {
            var inventory = ctx.Warehouses.TryGetValue(s.Id, out var wh)
                ? wh.Items.Select(i =>
                    new InventoryItemDto(i.ProductTypeId, string.Empty, i.Quantity, i.Quality))
                    .ToList().AsReadOnly()
                : (IReadOnlyList<InventoryItemDto>)[];

            var prices = ctx.Markets.TryGetValue(s.Id, out var m)
                ? m.Offers.Select(o =>
                    new MarketPriceDto(o.ProductTypeId, string.Empty, o.CurrentPrice, o.SupplyVolume, o.DemandVolume))
                    .ToList().AsReadOnly()
                : (IReadOnlyList<MarketPriceDto>)[];

            return new SettlementSummaryDto(s.Id, s.Name, inventory, prices);
        }).ToList().AsReadOnly();

        return new SimulationResultDto(worldId, daysBefore, daysAfter, settlements);
    }
}
