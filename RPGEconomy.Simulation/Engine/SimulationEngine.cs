using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Markets;
using RPGEconomy.Domain.Population;
using RPGEconomy.Domain.Production;
using RPGEconomy.Simulation.Services;

namespace RPGEconomy.Simulation.Engine;

public class SimulationEngine : ISimulationExecutor
{
    private readonly IWorldRepository _worldRepo;
    private readonly ISettlementRepository _settlementRepo;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly IMarketRepository _marketRepo;
    private readonly IPopulationGroupRepository _populationGroupRepo;
    private readonly IBuildingRepository _buildingRepo;
    private readonly IProductionRecipeRepository _recipeRepo;

    private readonly ProductionSimulationService _productionService;
    private readonly SettlementEconomySimulationService _settlementEconomyService;

    public SimulationEngine(
        IWorldRepository worldRepo,
        ISettlementRepository settlementRepo,
        IWarehouseRepository warehouseRepo,
        IMarketRepository marketRepo,
        IPopulationGroupRepository populationGroupRepo,
        IBuildingRepository buildingRepo,
        IProductionRecipeRepository recipeRepo,
        ProductionSimulationService productionService,
        SettlementEconomySimulationService settlementEconomyService)
    {
        _worldRepo = worldRepo;
        _settlementRepo = settlementRepo;
        _warehouseRepo = warehouseRepo;
        _marketRepo = marketRepo;
        _populationGroupRepo = populationGroupRepo;
        _buildingRepo = buildingRepo;
        _recipeRepo = recipeRepo;
        _productionService = productionService;
        _settlementEconomyService = settlementEconomyService;
    }

    public async Task<Result<SimulationExecutionResult>> ExecuteAsync(
        SimulationExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var worldId = request.WorldId;
        var days = request.Days;

        var world = await _worldRepo.GetByIdAsync(worldId);
        if (world is null)
            return Result<SimulationExecutionResult>.Failure($"World with Id {worldId} was not found");

        var daysBefore = world.CurrentDay;

        var ctx = await LoadContextAsync(worldId, world.CurrentDay);

        for (int i = 0; i < days; i++)
            RunTick(ctx);

        await PersistContextAsync(ctx);

        world.AdvanceDays(days);
        await _worldRepo.SaveAsync(world);

        var result = BuildResult(worldId, daysBefore, world.CurrentDay, ctx);
        return Result<SimulationExecutionResult>.Success(new SimulationExecutionResult(result));
    }

    private void RunTick(SimulationContext ctx)
    {
        _productionService.RunTick(ctx);
        _settlementEconomyService.RunTick(ctx);
    }

    private async Task<SimulationContext> LoadContextAsync(int worldId, int currentDay)
    {
        var settlements = await _settlementRepo.GetByWorldIdAsync(worldId);

        var warehouses = new Dictionary<int, Warehouse>();
        var markets = new Dictionary<int, Market>();
        var populationGroups = new Dictionary<int, IReadOnlyList<PopulationGroup>>();
        var buildings = new Dictionary<int, IReadOnlyList<Building>>();

        foreach (var settlement in settlements)
        {
            var warehouse = await _warehouseRepo.GetBySettlementIdAsync(settlement.Id);
            if (warehouse is not null)
                warehouses[settlement.Id] = warehouse;

            var market = await _marketRepo.GetBySettlementIdAsync(settlement.Id);
            if (market is not null)
                markets[settlement.Id] = market;

            populationGroups[settlement.Id] = await _populationGroupRepo.GetBySettlementIdAsync(settlement.Id);
            buildings[settlement.Id] = await _buildingRepo.GetBySettlementIdAsync(settlement.Id);
        }

        var allRecipes = await _recipeRepo.GetAllAsync();
        var recipes = allRecipes.ToDictionary(r => r.Id);

        return new SimulationContext(
            worldId,
            currentDay,
            settlements,
            warehouses,
            markets,
            populationGroups,
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
        int worldId,
        int daysBefore,
        int daysAfter,
        SimulationContext ctx)
    {
        var settlements = ctx.Settlements.Select(settlement =>
        {
            var inventory = ctx.Warehouses.TryGetValue(settlement.Id, out var warehouse)
                ? warehouse.Items
                    .Select(item => new InventoryItemDto(item.ProductTypeId, string.Empty, item.Quantity, item.Quality))
                    .ToList()
                    .AsReadOnly()
                : (IReadOnlyList<InventoryItemDto>)[];

            var prices = ctx.Markets.TryGetValue(settlement.Id, out var market)
                ? market.Offers
                    .Select(offer => new MarketPriceDto(
                        offer.ProductTypeId,
                        string.Empty,
                        offer.CurrentPrice,
                        offer.SupplyVolume,
                        offer.DemandVolume))
                    .ToList()
                    .AsReadOnly()
                : (IReadOnlyList<MarketPriceDto>)[];

            return new SettlementSummaryDto(settlement.Id, settlement.Name, settlement.Population, inventory, prices);
        }).ToList().AsReadOnly();

        return new SimulationResultDto(worldId, daysBefore, daysAfter, settlements);
    }
}
