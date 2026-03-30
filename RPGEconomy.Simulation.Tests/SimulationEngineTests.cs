using FluentAssertions;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Events;
using RPGEconomy.Domain.Markets;
using RPGEconomy.Domain.Population;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.Resources;
using RPGEconomy.Domain.World;
using RPGEconomy.Simulation.Engine;
using RPGEconomy.Simulation.Services;
using WorldEntity = RPGEconomy.Domain.World.World;

namespace RPGEconomy.Simulation.Tests;

public class SimulationEngineTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_World_Is_Missing()
    {
        var engine = CreateEngine(
            new WorldRepositoryFake(),
            new SettlementRepositoryFake(),
            new WarehouseRepositoryFake(),
            new MarketRepositoryFake(),
            new EconomicEventRepositoryFake(),
            new PopulationGroupRepositoryFake(),
            new BuildingRepositoryFake(),
            new RecipeRepositoryFake(),
            new ProductTypeRepositoryFake());

        var result = await engine.ExecuteAsync(new SimulationExecutionRequest(1, 1, 1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Advance_World_And_Keep_Legacy_ZeroInput_Recipes_Free_Of_ProductionDemand()
    {
        var world = new WorldEntity(1, "World", "Desc", 3);
        var settlement = new Settlement(10, 1, "Town");
        var market = new Market(30, settlement.Id);
        market.RegisterProduct(2, 10m);
        var recipe = ProductionRecipe.Create("Bread", 1, [], [new RecipeIngredient(2, 1m)]).Value!;
        var engine = CreateEngine(
            new WorldRepositoryFake(world),
            new SettlementRepositoryFake(settlement),
            new WarehouseRepositoryFake(new Warehouse(20, settlement.Id)),
            new MarketRepositoryFake(market),
            new EconomicEventRepositoryFake(),
            new PopulationGroupRepositoryFake(
                PopulationGroup.Create(settlement.Id, "Peasants", 50, [(2, 0.1m)]).Value!),
            new BuildingRepositoryFake(new Building(40, "Bakery", settlement.Id, 100, 2, true)),
            new RecipeRepositoryFake((100, recipe)),
            new ProductTypeRepositoryFake(new ProductType(2, "Bread", "Desc", 10m, 1d)));

        var result = await engine.ExecuteAsync(new SimulationExecutionRequest(1, world.Id, 2), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Result.DaysBefore.Should().Be(3);
        result.Value.Result.DaysAfter.Should().Be(5);
        world.CurrentDay.Should().Be(5);
        result.Value.Result.Settlements.Should().ContainSingle();
        result.Value.Result.Settlements[0].Prices.Should().ContainSingle();

        var price = result.Value.Result.Settlements[0].Prices[0];
        price.ProductTypeId.Should().Be(2);
        price.ProductName.Should().Be("Bread");
        price.Supply.Should().Be(2m);
        price.Demand.Should().Be(5m);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Add_ProductionDemand_For_Missing_Inputs()
    {
        var world = new WorldEntity(1, "World", "Desc", 0);
        var settlement = new Settlement(10, 1, "Town");
        var warehouse = new Warehouse(20, settlement.Id);

        var market = new Market(30, settlement.Id);
        market.RegisterProduct(1, 5m);
        market.RegisterProduct(2, 10m);

        var recipe = ProductionRecipe.Create(
            "Bread",
            1,
            [new RecipeIngredient(1, 2m)],
            [new RecipeIngredient(2, 1m)]).Value!;

        var building = new Building(40, "Bakery", settlement.Id, 100, 2, true);
        building.ReceiveInputReserve(1, 3m);

        var engine = CreateEngine(
            new WorldRepositoryFake(world),
            new SettlementRepositoryFake(settlement),
            new WarehouseRepositoryFake(warehouse),
            new MarketRepositoryFake(market),
            new EconomicEventRepositoryFake(),
            new PopulationGroupRepositoryFake(
                PopulationGroup.Create(settlement.Id, "Peasants", 50, [(2, 0.1m)]).Value!),
            new BuildingRepositoryFake(building),
            new RecipeRepositoryFake((100, recipe)),
            new ProductTypeRepositoryFake(
                new ProductType(1, "Grain", "Desc", 5m, 1d),
                new ProductType(2, "Bread", "Desc", 10m, 1d)));

        var result = await engine.ExecuteAsync(new SimulationExecutionRequest(1, world.Id, 1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var settlementResult = result.Value!.Result.Settlements.Single();

        settlementResult.Prices.Should().Contain(x => x.ProductTypeId == 1 && x.Supply == 0m && x.Demand == 1m);
        settlementResult.Prices.Should().Contain(x => x.ProductTypeId == 2 && x.Supply == 1.5m && x.Demand == 5m);
        settlementResult.Warehouse.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_Settlement_Has_No_Warehouse()
    {
        var world = new WorldEntity(1, "World", "Desc", 0);
        var settlement = new Settlement(10, 1, "Town");
        var market = new Market(30, settlement.Id);

        var engine = CreateEngine(
            new WorldRepositoryFake(world),
            new SettlementRepositoryFake(settlement),
            new WarehouseRepositoryFake(),
            new MarketRepositoryFake(market),
            new EconomicEventRepositoryFake(),
            new PopulationGroupRepositoryFake(),
            new BuildingRepositoryFake(),
            new RecipeRepositoryFake(),
            new ProductTypeRepositoryFake());

        var result = await engine.ExecuteAsync(new SimulationExecutionRequest(1, world.Id, 1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("склад");
        world.CurrentDay.Should().Be(0);
    }

    private static SimulationEngine CreateEngine(
        IWorldRepository worlds,
        ISettlementRepository settlements,
        IWarehouseRepository warehouses,
        IMarketRepository markets,
        IEconomicEventRepository economicEvents,
        IPopulationGroupRepository populationGroups,
        IBuildingRepository buildings,
        IProductionRecipeRepository recipes,
        IProductTypeRepository productTypes) =>
        new(
            worlds,
            settlements,
            warehouses,
            markets,
            economicEvents,
            populationGroups,
            buildings,
            recipes,
            productTypes,
            new ProductionSimulationService(),
            new SettlementEconomySimulationService());

    private sealed class WorldRepositoryFake : IWorldRepository
    {
        private readonly WorldEntity? _world;

        public WorldRepositoryFake(WorldEntity? world = null)
            => _world = world;

        public Task<WorldEntity?> GetByIdAsync(int id) => Task.FromResult(_world?.Id == id ? _world : null);

        public Task<IReadOnlyList<WorldEntity>> GetAllAsync() =>
            Task.FromResult((IReadOnlyList<WorldEntity>)(_world is null ? [] : [_world]));

        public Task<int> SaveAsync(WorldEntity entity) => Task.FromResult(entity.Id);

        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class SettlementRepositoryFake : ISettlementRepository
    {
        private readonly IReadOnlyList<Settlement> _settlements;

        public SettlementRepositoryFake(params Settlement[] settlements)
            => _settlements = settlements;

        public Task<Settlement?> GetByIdAsync(int id) => Task.FromResult(_settlements.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Settlement>> GetByWorldIdAsync(int worldId) =>
            Task.FromResult((IReadOnlyList<Settlement>)_settlements.Where(x => x.WorldId == worldId).ToList().AsReadOnly());

        public Task<int> SaveAsync(Settlement entity) => Task.FromResult(entity.Id);

        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class WarehouseRepositoryFake : IWarehouseRepository
    {
        private readonly Dictionary<int, Warehouse> _items = [];

        public WarehouseRepositoryFake(params Warehouse[] items)
        {
            foreach (var item in items)
                _items[item.SettlementId] = item;
        }

        public Task<Warehouse?> GetByIdAsync(int id) => Task.FromResult(_items.Values.FirstOrDefault(x => x.Id == id));

        public Task<Warehouse?> GetBySettlementIdAsync(int settlementId) => Task.FromResult(_items.GetValueOrDefault(settlementId));

        public Task<int> SaveAsync(Warehouse entity) => Task.FromResult(entity.Id);

        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class MarketRepositoryFake : IMarketRepository
    {
        private readonly Dictionary<int, Market> _items = [];

        public MarketRepositoryFake(params Market[] items)
        {
            foreach (var item in items)
                _items[item.SettlementId] = item;
        }

        public Task<Market?> GetByIdAsync(int id) => Task.FromResult(_items.Values.FirstOrDefault(x => x.Id == id));

        public Task<Market?> GetBySettlementIdAsync(int settlementId) => Task.FromResult(_items.GetValueOrDefault(settlementId));

        public Task<int> SaveAsync(Market entity) => Task.FromResult(entity.Id);

        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class BuildingRepositoryFake : IBuildingRepository
    {
        private readonly IReadOnlyList<Building> _items;

        public BuildingRepositoryFake(params Building[] items)
            => _items = items;

        public Task<Building?> GetByIdAsync(int id) => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Building>> GetBySettlementIdAsync(int settlementId) =>
            Task.FromResult((IReadOnlyList<Building>)_items.Where(x => x.SettlementId == settlementId).ToList().AsReadOnly());

        public Task<int> SaveAsync(Building entity) => Task.FromResult(entity.Id);

        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class EconomicEventRepositoryFake : IEconomicEventRepository
    {
        private readonly IReadOnlyList<EconomicEvent> _items;

        public EconomicEventRepositoryFake(params EconomicEvent[] items)
            => _items = items;

        public Task<EconomicEvent?> GetByIdAsync(int id) => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<EconomicEvent>> GetBySettlementIdAsync(int settlementId) =>
            Task.FromResult((IReadOnlyList<EconomicEvent>)_items.Where(x => x.SettlementId == settlementId).ToList().AsReadOnly());

        public Task<int> SaveAsync(EconomicEvent entity) => Task.FromResult(entity.Id);

        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class PopulationGroupRepositoryFake : IPopulationGroupRepository
    {
        private readonly IReadOnlyList<PopulationGroup> _items;

        public PopulationGroupRepositoryFake(params PopulationGroup[] items)
            => _items = items;

        public Task<PopulationGroup?> GetByIdAsync(int id) => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<PopulationGroup>> GetBySettlementIdAsync(int settlementId) =>
            Task.FromResult((IReadOnlyList<PopulationGroup>)_items.Where(x => x.SettlementId == settlementId).ToList().AsReadOnly());

        public Task<int> SaveAsync(PopulationGroup entity) => Task.FromResult(entity.Id);

        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class RecipeRepositoryFake : IProductionRecipeRepository
    {
        private readonly Dictionary<int, ProductionRecipe> _items = [];

        public RecipeRepositoryFake(params (int Id, ProductionRecipe Recipe)[] items)
        {
            foreach (var (id, recipe) in items)
            {
                var stored = new ProductionRecipe(id, recipe.Name, recipe.LaborDaysRequired);
                stored.Update(
                    recipe.Name,
                    recipe.LaborDaysRequired,
                    recipe.Inputs.Select(i => new RecipeIngredient(i.ProductTypeId, i.Quantity)),
                    recipe.Outputs.Select(o => new RecipeIngredient(o.ProductTypeId, o.Quantity)));
                _items[id] = stored;
            }
        }

        public Task<ProductionRecipe?> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<ProductionRecipe>> GetAllAsync() =>
            Task.FromResult((IReadOnlyList<ProductionRecipe>)_items.Values.ToList().AsReadOnly());

        public Task<int> SaveAsync(ProductionRecipe entity) => Task.FromResult(entity.Id);

        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class ProductTypeRepositoryFake : IProductTypeRepository
    {
        private readonly Dictionary<int, ProductType> _items = [];

        public ProductTypeRepositoryFake(params ProductType[] items)
        {
            foreach (var item in items)
                _items[item.Id] = item;
        }

        public Task<ProductType?> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<ProductType>> GetAllAsync() =>
            Task.FromResult((IReadOnlyList<ProductType>)_items.Values.ToList().AsReadOnly());

        public Task<ProductType?> GetByNameAsync(string name) =>
            Task.FromResult(_items.Values.FirstOrDefault(x => x.Name == name));

        public Task<int> SaveAsync(ProductType entity) => Task.FromResult(entity.Id);

        public Task DeleteAsync(int id) => Task.CompletedTask;

        public Task<bool> IsInUseAsync(int id) => Task.FromResult(false);
    }
}
