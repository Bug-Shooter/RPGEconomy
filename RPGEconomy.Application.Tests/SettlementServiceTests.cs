using FluentAssertions;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Services;
using RPGEconomy.Domain.Markets;
using RPGEconomy.Domain.Population;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.Resources;
using RPGEconomy.Domain.World;
using WorldEntity = RPGEconomy.Domain.World.World;

namespace RPGEconomy.Application.Tests;

public class SettlementServiceTests
{
    [Fact]
    public async Task CreateAsync_Should_Create_Settlement_Warehouse_And_Market()
    {
        var settlementRepo = new SettlementRepositoryFake();
        var worldRepo = new WorldRepositoryFake(new WorldEntity(1, "World", "Desc", 0));
        var warehouseRepo = new WarehouseRepositoryFake();
        var marketRepo = new MarketRepositoryFake();
        var service = new SettlementService(
            settlementRepo,
            worldRepo,
            warehouseRepo,
            marketRepo,
            new PopulationGroupRepositoryFake(),
            new ProductTypeRepositoryFake());

        var result = await service.CreateAsync(1, "Town");

        result.IsSuccess.Should().BeTrue();
        settlementRepo.Entities.Should().ContainKey(result.Value!.SettlementId);
        warehouseRepo.BySettlementId.Should().ContainKey(result.Value.SettlementId);
        marketRepo.BySettlementId.Should().ContainKey(result.Value.SettlementId);
        result.Value.Population.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_Should_Fail_When_World_Does_Not_Exist()
    {
        var service = new SettlementService(
            new SettlementRepositoryFake(),
            new WorldRepositoryFake(),
            new WarehouseRepositoryFake(),
            new MarketRepositoryFake(),
            new PopulationGroupRepositoryFake(),
            new ProductTypeRepositoryFake());

        var result = await service.CreateAsync(999, "Town");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_Should_Compose_Dto_From_Settlement_Warehouse_Market_And_Groups()
    {
        var settlement = new Settlement(5, 1, "Town");
        var settlementRepo = new SettlementRepositoryFake(settlement);
        var worldRepo = new WorldRepositoryFake(new WorldEntity(1, "World", "Desc", 0));
        var warehouse = new Warehouse(3, settlement.Id);
        warehouse.AddItem(10, 7, QualityGrade.Normal);
        var warehouseRepo = new WarehouseRepositoryFake(warehouse);
        var market = new Market(4, settlement.Id);
        market.RegisterProduct(10, 12.5m);
        market.UpdateProductState(10, 7, 10);
        var marketRepo = new MarketRepositoryFake(market);
        var groupRepo = new PopulationGroupRepositoryFake(
            PopulationGroup.Create(settlement.Id, "Peasants", 200, []).Value!);
        var productRepo = new ProductTypeRepositoryFake(new ProductType(10, "Bread", "Desc", 1m, 1d));
        var service = new SettlementService(settlementRepo, worldRepo, warehouseRepo, marketRepo, groupRepo, productRepo);

        var result = await service.GetByIdAsync(settlement.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Population.Should().Be(200);
        result.Value.Warehouse.Should().ContainSingle(x => x.ProductTypeId == 10 && x.Quantity == 7 && x.ProductName == "Bread");
        result.Value.Prices.Should().ContainSingle(x => x.ProductTypeId == 10 && x.ProductName == "Bread");
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Name_And_Keep_Computed_Population()
    {
        var settlement = new Settlement(5, 1, "Town");
        var service = new SettlementService(
            new SettlementRepositoryFake(settlement),
            new WorldRepositoryFake(new WorldEntity(1, "World", "Desc", 0)),
            new WarehouseRepositoryFake(new Warehouse(1, settlement.Id)),
            new MarketRepositoryFake(new Market(2, settlement.Id)),
            new PopulationGroupRepositoryFake(
                PopulationGroup.Create(settlement.Id, "Peasants", 200, []).Value!),
            new ProductTypeRepositoryFake());

        var result = await service.UpdateAsync(settlement.Id, "New Town");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Town");
        result.Value.Population.Should().Be(200);
    }

    private sealed class WorldRepositoryFake : IWorldRepository
    {
        private readonly Dictionary<int, WorldEntity> _worlds = [];

        public WorldRepositoryFake(params WorldEntity[] worlds)
        {
            foreach (var world in worlds)
                _worlds[world.Id] = new WorldEntity(world.Id, world.Name, world.Description, world.CurrentDay);
        }

        public Task<IReadOnlyList<WorldEntity>> GetAllAsync() =>
            Task.FromResult((IReadOnlyList<WorldEntity>)_worlds.Values.ToList().AsReadOnly());

        public Task<WorldEntity?> GetByIdAsync(int id) => Task.FromResult(_worlds.GetValueOrDefault(id));

        public Task<int> SaveAsync(WorldEntity entity)
        {
            _worlds[entity.Id] = new WorldEntity(entity.Id, entity.Name, entity.Description, entity.CurrentDay);
            return Task.FromResult(entity.Id);
        }

        public Task DeleteAsync(int id)
        {
            _worlds.Remove(id);
            return Task.CompletedTask;
        }
    }

    private sealed class SettlementRepositoryFake : ISettlementRepository
    {
        public Dictionary<int, Settlement> Entities { get; } = [];
        private int _nextId = 1;

        public SettlementRepositoryFake(params Settlement[] settlements)
        {
            foreach (var settlement in settlements)
            {
                Entities[settlement.Id] = new Settlement(settlement.Id, settlement.WorldId, settlement.Name);
                _nextId = Math.Max(_nextId, settlement.Id + 1);
            }
        }

        public Task<Settlement?> GetByIdAsync(int id) => Task.FromResult(Entities.GetValueOrDefault(id));

        public Task<IReadOnlyList<Settlement>> GetByWorldIdAsync(int worldId) =>
            Task.FromResult((IReadOnlyList<Settlement>)Entities.Values.Where(x => x.WorldId == worldId).ToList().AsReadOnly());

        public Task<int> SaveAsync(Settlement entity)
        {
            var id = entity.Id == 0 ? _nextId++ : entity.Id;
            Entities[id] = new Settlement(id, entity.WorldId, entity.Name);
            return Task.FromResult(id);
        }

        public Task DeleteAsync(int id)
        {
            Entities.Remove(id);
            return Task.CompletedTask;
        }
    }

    private sealed class WarehouseRepositoryFake : IWarehouseRepository
    {
        public Dictionary<int, Warehouse> BySettlementId { get; } = [];
        private int _nextId = 1;

        public WarehouseRepositoryFake(params Warehouse[] warehouses)
        {
            foreach (var warehouse in warehouses)
            {
                BySettlementId[warehouse.SettlementId] = CloneWarehouse(warehouse);
                _nextId = Math.Max(_nextId, warehouse.Id + 1);
            }
        }

        public Task<Warehouse?> GetByIdAsync(int id) =>
            Task.FromResult(BySettlementId.Values.FirstOrDefault(x => x.Id == id));

        public Task<Warehouse?> GetBySettlementIdAsync(int settlementId) =>
            Task.FromResult(BySettlementId.GetValueOrDefault(settlementId));

        public Task<int> SaveAsync(Warehouse entity)
        {
            var id = entity.Id == 0 ? _nextId++ : entity.Id;
            var stored = CloneWarehouse(entity, id);
            BySettlementId[entity.SettlementId] = stored;
            return Task.FromResult(id);
        }

        public Task DeleteAsync(int id)
        {
            var warehouse = BySettlementId.Values.First(x => x.Id == id);
            BySettlementId.Remove(warehouse.SettlementId);
            return Task.CompletedTask;
        }

        private static Warehouse CloneWarehouse(Warehouse warehouse, int? id = null)
        {
            var clone = new Warehouse(id ?? warehouse.Id, warehouse.SettlementId);
            foreach (var item in warehouse.Items)
                clone.AddItem(item.ProductTypeId, item.Quantity, QualityGrade.FromName(item.Quality));
            return clone;
        }
    }

    private sealed class MarketRepositoryFake : IMarketRepository
    {
        public Dictionary<int, Market> BySettlementId { get; } = [];
        private int _nextId = 1;

        public MarketRepositoryFake(params Market[] markets)
        {
            foreach (var market in markets)
            {
                BySettlementId[market.SettlementId] = CloneMarket(market);
                _nextId = Math.Max(_nextId, market.Id + 1);
            }
        }

        public Task<Market?> GetByIdAsync(int id) =>
            Task.FromResult(BySettlementId.Values.FirstOrDefault(x => x.Id == id));

        public Task<Market?> GetBySettlementIdAsync(int settlementId) =>
            Task.FromResult(BySettlementId.GetValueOrDefault(settlementId));

        public Task<int> SaveAsync(Market entity)
        {
            var id = entity.Id == 0 ? _nextId++ : entity.Id;
            BySettlementId[entity.SettlementId] = CloneMarket(entity, id);
            return Task.FromResult(id);
        }

        public Task DeleteAsync(int id)
        {
            var market = BySettlementId.Values.First(x => x.Id == id);
            BySettlementId.Remove(market.SettlementId);
            return Task.CompletedTask;
        }

        private static Market CloneMarket(Market market, int? id = null)
        {
            var clone = new Market(id ?? market.Id, market.SettlementId);
            foreach (var offer in market.Offers)
            {
                clone.RegisterProduct(offer.ProductTypeId, offer.CurrentPrice);
                clone.UpdateProductState(offer.ProductTypeId, offer.SupplyVolume, offer.DemandVolume);
            }

            return clone;
        }
    }

    private sealed class PopulationGroupRepositoryFake : IPopulationGroupRepository
    {
        private readonly Dictionary<int, PopulationGroup> _items = [];
        private int _nextId = 1;

        public PopulationGroupRepositoryFake(params PopulationGroup[] groups)
        {
            foreach (var group in groups)
            {
                var id = group.Id == 0 ? _nextId++ : group.Id;
                _items[id] = group;
                _nextId = Math.Max(_nextId, id + 1);
            }
        }

        public Task<PopulationGroup?> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<PopulationGroup>> GetBySettlementIdAsync(int settlementId) =>
            Task.FromResult((IReadOnlyList<PopulationGroup>)_items.Values.Where(x => x.SettlementId == settlementId).ToList().AsReadOnly());

        public Task<int> SaveAsync(PopulationGroup entity)
        {
            var id = entity.Id == 0 ? _nextId++ : entity.Id;
            _items[id] = entity;
            return Task.FromResult(id);
        }

        public Task DeleteAsync(int id)
        {
            _items.Remove(id);
            return Task.CompletedTask;
        }
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

        public Task<int> SaveAsync(ProductType entity)
        {
            _items[entity.Id] = entity;
            return Task.FromResult(entity.Id);
        }

        public Task DeleteAsync(int id)
        {
            _items.Remove(id);
            return Task.CompletedTask;
        }

        public Task<bool> IsInUseAsync(int id) => Task.FromResult(false);
    }
}
