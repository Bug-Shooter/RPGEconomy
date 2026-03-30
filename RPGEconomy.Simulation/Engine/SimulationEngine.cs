using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Events;
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
    private readonly IEconomicEventRepository _economicEventRepo;
    private readonly IPopulationGroupRepository _populationGroupRepo;
    private readonly IBuildingRepository _buildingRepo;
    private readonly IProductionRecipeRepository _recipeRepo;
    private readonly IProductTypeRepository _productTypeRepo;

    private readonly ProductionSimulationService _productionService;
    private readonly SettlementEconomySimulationService _settlementEconomyService;

    public SimulationEngine(
        IWorldRepository worldRepo,
        ISettlementRepository settlementRepo,
        IWarehouseRepository warehouseRepo,
        IMarketRepository marketRepo,
        IEconomicEventRepository economicEventRepo,
        IPopulationGroupRepository populationGroupRepo,
        IBuildingRepository buildingRepo,
        IProductionRecipeRepository recipeRepo,
        IProductTypeRepository productTypeRepo,
        ProductionSimulationService productionService,
        SettlementEconomySimulationService settlementEconomyService)
    {
        _worldRepo = worldRepo;
        _settlementRepo = settlementRepo;
        _warehouseRepo = warehouseRepo;
        _marketRepo = marketRepo;
        _economicEventRepo = economicEventRepo;
        _populationGroupRepo = populationGroupRepo;
        _buildingRepo = buildingRepo;
        _recipeRepo = recipeRepo;
        _productTypeRepo = productTypeRepo;
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
            return Result<SimulationExecutionResult>.Failure($"Мир с Id {worldId} не найден");

        var daysBefore = world.CurrentDay;
        var contextResult = await LoadContextAsync(worldId, world.CurrentDay);
        if (!contextResult.IsSuccess)
            return Result<SimulationExecutionResult>.Failure(contextResult.Error!);

        var ctx = contextResult.Value!;

        for (var i = 0; i < days; i++)
            RunTick(ctx);

        var advanceResult = world.AdvanceDays(days);
        if (!advanceResult.IsSuccess)
            return Result<SimulationExecutionResult>.Failure(advanceResult.Error!);

        await PersistContextAsync(ctx);
        await _worldRepo.SaveAsync(world);

        var productNames = await GetProductNamesAsync();
        var result = BuildResult(worldId, daysBefore, world.CurrentDay, ctx, productNames);
        return Result<SimulationExecutionResult>.Success(new SimulationExecutionResult(result));
    }

    private void RunTick(SimulationContext ctx)
    {
        _settlementEconomyService.ConsumeHouseholdStocks(ctx);
        _productionService.RunTick(ctx);
        _settlementEconomyService.ReplenishReservesAndUpdateMarket(ctx);
        ctx.AdvanceDay();
    }

    private async Task<Result<SimulationContext>> LoadContextAsync(int worldId, int currentDay)
    {
        var settlements = await _settlementRepo.GetByWorldIdAsync(worldId);

        var warehouses = new Dictionary<int, Warehouse>(settlements.Count);
        var markets = new Dictionary<int, Market>(settlements.Count);
        var populationGroups = new Dictionary<int, IReadOnlyList<PopulationGroup>>(settlements.Count);
        var buildings = new Dictionary<int, IReadOnlyList<Building>>(settlements.Count);
        var economicEvents = new Dictionary<int, IReadOnlyList<EconomicEvent>>(settlements.Count);

        foreach (var settlement in settlements)
        {
            var warehouse = await _warehouseRepo.GetBySettlementIdAsync(settlement.Id);
            if (warehouse is null)
            {
                return Result<SimulationContext>.Failure(
                    $"Для поселения с Id {settlement.Id} не найден склад");
            }

            var market = await _marketRepo.GetBySettlementIdAsync(settlement.Id);
            if (market is null)
            {
                return Result<SimulationContext>.Failure(
                    $"Для поселения с Id {settlement.Id} не найден рынок");
            }

            warehouses[settlement.Id] = warehouse;
            markets[settlement.Id] = market;
            populationGroups[settlement.Id] = await _populationGroupRepo.GetBySettlementIdAsync(settlement.Id);
            buildings[settlement.Id] = await _buildingRepo.GetBySettlementIdAsync(settlement.Id);
            economicEvents[settlement.Id] = await _economicEventRepo.GetBySettlementIdAsync(settlement.Id);
        }

        var allRecipes = await _recipeRepo.GetAllAsync();
        var recipes = allRecipes.ToDictionary(recipe => recipe.Id);

        return Result<SimulationContext>.Success(
            new SimulationContext(
                worldId,
                currentDay,
                settlements,
                warehouses,
                markets,
                populationGroups,
                buildings,
                recipes,
                economicEvents));
    }

    private async Task PersistContextAsync(SimulationContext ctx)
    {
        foreach (var warehouse in ctx.Warehouses.Values)
            await _warehouseRepo.SaveAsync(warehouse);

        foreach (var populationGroup in ctx.PopulationGroups.Values.SelectMany(groups => groups))
            await _populationGroupRepo.SaveAsync(populationGroup);

        foreach (var building in ctx.Buildings.Values.SelectMany(buildings => buildings))
            await _buildingRepo.SaveAsync(building);

        foreach (var market in ctx.Markets.Values)
            await _marketRepo.SaveAsync(market);
    }

    private SimulationResultDto BuildResult(
        int worldId,
        int daysBefore,
        int daysAfter,
        SimulationContext ctx,
        IReadOnlyDictionary<int, string> productNames)
    {
        var settlements = ctx.Settlements
            .Select(settlement =>
            {
                var inventory = ctx.Warehouses[settlement.Id].Items
                    .Select(item => new InventoryItemDto(
                        item.ProductTypeId,
                        ResolveProductName(productNames, item.ProductTypeId),
                        item.Quantity,
                        item.Quality))
                    .ToList()
                    .AsReadOnly();

                var prices = ctx.Markets[settlement.Id].Offers
                    .Select(offer => new MarketPriceDto(
                        offer.ProductTypeId,
                        ResolveProductName(productNames, offer.ProductTypeId),
                        offer.CurrentPrice,
                        offer.SupplyVolume,
                        offer.DemandVolume))
                    .ToList()
                    .AsReadOnly();

                var population = ctx.PopulationGroups
                    .GetValueOrDefault(settlement.Id, Array.Empty<PopulationGroup>())
                    .Sum(group => group.PopulationSize);

                return new SimulationSettlementDto(
                    settlement.Id,
                    settlement.Name,
                    population,
                    inventory,
                    prices);
            })
            .ToList()
            .AsReadOnly();

        return new SimulationResultDto(worldId, daysBefore, daysAfter, settlements);
    }

    private async Task<IReadOnlyDictionary<int, string>> GetProductNamesAsync()
    {
        var products = await _productTypeRepo.GetAllAsync();
        return products.ToDictionary(product => product.Id, product => product.Name);
    }

    private static string ResolveProductName(
        IReadOnlyDictionary<int, string> productNames,
        int productTypeId) =>
        productNames.GetValueOrDefault(productTypeId, "Неизвестный товар");
}
