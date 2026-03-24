using FluentAssertions;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Markets;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.Resources;
using RPGEconomy.Simulation.Engine;
using RPGEconomy.Simulation.Services;
using RPGEconomy.Domain.World;
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
            new BuildingRepositoryFake(),
            new RecipeRepositoryFake());

        var result = await engine.ExecuteAsync(new SimulationExecutionRequest(1, 1, 1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Advance_World_And_Persist_Context()
    {
        var world = new WorldEntity(1, "World", "Desc", 3);
        var settlement = new Settlement(10, 1, "Town", 100);
        var warehouse = new Warehouse(20, settlement.Id);
        warehouse.AddItem(1, 4, QualityGrade.Normal);
        var market = new Market(30, settlement.Id);
        market.RegisterProduct(2, 10);
        var recipe = ProductionRecipe.Create("Bread", 1, [new RecipeIngredient(1, 2)], [new RecipeIngredient(2, 1)]);
        var engine = CreateEngine(
            new WorldRepositoryFake(world),
            new SettlementRepositoryFake(settlement),
            new WarehouseRepositoryFake(warehouse),
            new MarketRepositoryFake(market),
            new BuildingRepositoryFake(new Building(40, "Bakery", settlement.Id, 100, 2, true)),
            new RecipeRepositoryFake((100, recipe)));

        var result = await engine.ExecuteAsync(new SimulationExecutionRequest(1, world.Id, 2), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Result.DaysBefore.Should().Be(3);
        result.Value.Result.DaysAfter.Should().Be(5);
        world.CurrentDay.Should().Be(5);
        market.Offers.Should().ContainSingle(x => x.ProductTypeId == 2 && x.SupplyVolume == 2);
    }

    private static SimulationEngine CreateEngine(
        IWorldRepository worlds,
        ISettlementRepository settlements,
        IWarehouseRepository warehouses,
        IMarketRepository markets,
        IBuildingRepository buildings,
        IProductionRecipeRepository recipes) =>
        new(
            worlds,
            settlements,
            warehouses,
            markets,
            buildings,
            recipes,
            new ProductionSimulationService(),
            new MarketSimulationService());

    private sealed class WorldRepositoryFake : IWorldRepository
    {
        private readonly WorldEntity? _world;

        public WorldRepositoryFake(WorldEntity? world = null)
            => _world = world;

        public Task<WorldEntity?> GetByIdAsync(int id) => Task.FromResult(_world?.Id == id ? _world : null);
        public Task<IReadOnlyList<WorldEntity>> GetAllAsync() => Task.FromResult((IReadOnlyList<WorldEntity>)(_world is null ? [] : [_world]));
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
        public int SaveCalls { get; private set; }

        public WarehouseRepositoryFake(params Warehouse[] items)
        {
            foreach (var item in items)
                _items[item.SettlementId] = item;
        }

        public Task<Warehouse?> GetByIdAsync(int id) => Task.FromResult(_items.Values.FirstOrDefault(x => x.Id == id));
        public Task<Warehouse?> GetBySettlementIdAsync(int settlementId) => Task.FromResult(_items.GetValueOrDefault(settlementId));
        public Task<int> SaveAsync(Warehouse entity)
        {
            SaveCalls++;
            return Task.FromResult(entity.Id);
        }
        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class MarketRepositoryFake : IMarketRepository
    {
        private readonly Dictionary<int, Market> _items = [];
        public int SaveCalls { get; private set; }

        public MarketRepositoryFake(params Market[] items)
        {
            foreach (var item in items)
                _items[item.SettlementId] = item;
        }

        public Task<Market?> GetByIdAsync(int id) => Task.FromResult(_items.Values.FirstOrDefault(x => x.Id == id));
        public Task<Market?> GetBySettlementIdAsync(int settlementId) => Task.FromResult(_items.GetValueOrDefault(settlementId));
        public Task<int> SaveAsync(Market entity)
        {
            SaveCalls++;
            return Task.FromResult(entity.Id);
        }
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

    private sealed class RecipeRepositoryFake : IProductionRecipeRepository
    {
        private readonly Dictionary<int, ProductionRecipe> _items = [];

        public RecipeRepositoryFake(params (int Id, ProductionRecipe Recipe)[] items)
        {
            foreach (var (Id, Recipe) in items)
                _items[Id] = Recipe;
        }

        public Task<ProductionRecipe?> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));
        public Task<IReadOnlyList<ProductionRecipe>> GetAllAsync() =>
            Task.FromResult((IReadOnlyList<ProductionRecipe>)_items.Values.ToList().AsReadOnly());
        public Task<int> SaveAsync(ProductionRecipe entity) => Task.FromResult(entity.Id);
        public Task DeleteAsync(int id) => Task.CompletedTask;
    }
}
