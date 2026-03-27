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
        var worldRepo = new WorldRepositoryFake(WorldEntity.Create("World", "Desc"), 1);
        var warehouseRepo = new WarehouseRepositoryFake();
        var marketRepo = new MarketRepositoryFake();
        var service = new SettlementService(settlementRepo, worldRepo, warehouseRepo, marketRepo, new PopulationGroupRepositoryFake());

        var result = await service.CreateAsync(1, "Town", 150);

        result.IsSuccess.Should().BeTrue();
        settlementRepo.Entities.Should().ContainKey(result.Value!.SettlementId);
        warehouseRepo.BySettlementId.Should().ContainKey(result.Value.SettlementId);
        marketRepo.BySettlementId.Should().ContainKey(result.Value.SettlementId);
    }

    [Fact]
    public async Task CreateAsync_Should_Fail_When_World_Does_Not_Exist()
    {
        var service = new SettlementService(
            new SettlementRepositoryFake(),
            new WorldRepositoryFake(),
            new WarehouseRepositoryFake(),
            new MarketRepositoryFake(),
            new PopulationGroupRepositoryFake());

        var result = await service.CreateAsync(999, "Town", 150);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_Should_Compose_Dto_From_Settlement_Warehouse_And_Market()
    {
        var settlement = new Settlement(5, 1, "Town", 200);
        var settlementRepo = new SettlementRepositoryFake(settlement);
        var worldRepo = new WorldRepositoryFake(WorldEntity.Create("World", "Desc"), 1);
        var warehouse = new Warehouse(3, settlement.Id);
        warehouse.AddItem(10, 7, QualityGrade.Normal);
        var warehouseRepo = new WarehouseRepositoryFake(warehouse);
        var market = new Market(4, settlement.Id);
        market.RegisterProduct(10, 12.5m);
        market.UpdateProductState(10, 7, 10);
        var marketRepo = new MarketRepositoryFake(market);
        var service = new SettlementService(settlementRepo, worldRepo, warehouseRepo, marketRepo, new PopulationGroupRepositoryFake());

        var result = await service.GetByIdAsync(settlement.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Warehouse.Should().ContainSingle(x => x.ProductTypeId == 10 && x.Quantity == 7);
        result.Value.Prices.Should().ContainSingle(x => x.ProductTypeId == 10);
    }

    [Fact]
    public async Task UpdateAsync_Should_Reject_Direct_Population_Change_When_Groups_Exist()
    {
        var settlement = new Settlement(5, 1, "Town", 200);
        var service = new SettlementService(
            new SettlementRepositoryFake(settlement),
            new WorldRepositoryFake(WorldEntity.Create("World", "Desc"), 1),
            new WarehouseRepositoryFake(),
            new MarketRepositoryFake(),
            new PopulationGroupRepositoryFake(
                PopulationGroup.Create(settlement.Id, "Peasants", 200, []).Value!));

        var result = await service.UpdateAsync(settlement.Id, "Town", 201);

        result.IsSuccess.Should().BeFalse();
    }

    private sealed class WorldRepositoryFake : IWorldRepository
    {
        private readonly Dictionary<int, WorldEntity> _worlds = new();
        private int _nextId = 1;

        public WorldRepositoryFake(params (WorldEntity World, int Id)[] worlds)
        {
            foreach (var item in worlds)
            {
                _worlds[item.Id] = new WorldEntity(item.Id, item.World.Name, item.World.Description, item.World.CurrentDay);
                _nextId = Math.Max(_nextId, item.Id + 1);
            }
        }

        public WorldRepositoryFake(WorldEntity? world = null, int id = 1)
        {
            if (world is not null)
            {
                _worlds[id] = new WorldEntity(id, world.Name, world.Description, world.CurrentDay);
                _nextId = id + 1;
            }
        }

        public Task<IReadOnlyList<WorldEntity>> GetAllAsync() => Task.FromResult((IReadOnlyList<WorldEntity>)_worlds.Values.ToList().AsReadOnly());
        public Task<WorldEntity?> GetByIdAsync(int id) => Task.FromResult(_worlds.GetValueOrDefault(id));
        public Task<int> SaveAsync(WorldEntity entity)
        {
            var id = entity.Id == 0 ? _nextId++ : entity.Id;
            _worlds[id] = new WorldEntity(id, entity.Name, entity.Description, entity.CurrentDay);
            return Task.FromResult(id);
        }
        public Task DeleteAsync(int id)
        {
            _worlds.Remove(id);
            return Task.CompletedTask;
        }
    }

    private sealed class SettlementRepositoryFake : ISettlementRepository
    {
        public Dictionary<int, Settlement> Entities { get; } = new();
        private int _nextId = 1;

        public SettlementRepositoryFake(params Settlement[] settlements)
        {
            foreach (var settlement in settlements)
            {
                Entities[settlement.Id] = settlement;
                _nextId = Math.Max(_nextId, settlement.Id + 1);
            }
        }

        public Task<Settlement?> GetByIdAsync(int id) => Task.FromResult(Entities.GetValueOrDefault(id));
        public Task<IReadOnlyList<Settlement>> GetByWorldIdAsync(int worldId) =>
            Task.FromResult((IReadOnlyList<Settlement>)Entities.Values.Where(x => x.WorldId == worldId).ToList().AsReadOnly());

        public Task<int> SaveAsync(Settlement entity)
        {
            var id = entity.Id == 0 ? _nextId++ : entity.Id;
            var stored = new Settlement(id, entity.WorldId, entity.Name, entity.Population);
            Entities[id] = stored;
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
        public Dictionary<int, Warehouse> BySettlementId { get; } = new();
        private int _nextId = 1;

        public WarehouseRepositoryFake(Warehouse? warehouse = null)
        {
            if (warehouse is not null)
            {
                BySettlementId[warehouse.SettlementId] = warehouse;
                _nextId = warehouse.Id + 1;
            }
        }

        public Task<Warehouse?> GetByIdAsync(int id) =>
            Task.FromResult(BySettlementId.Values.FirstOrDefault(x => x.Id == id));

        public Task<Warehouse?> GetBySettlementIdAsync(int settlementId) =>
            Task.FromResult(BySettlementId.GetValueOrDefault(settlementId));

        public Task<int> SaveAsync(Warehouse entity)
        {
            var id = entity.Id == 0 ? _nextId++ : entity.Id;
            var stored = new Warehouse(id, entity.SettlementId);
            foreach (var item in entity.Items)
                stored.AddItem(item.ProductTypeId, item.Quantity, QualityGrade.FromName(item.Quality));

            BySettlementId[entity.SettlementId] = stored;
            return Task.FromResult(id);
        }

        public Task DeleteAsync(int id)
        {
            var warehouse = BySettlementId.Values.First(x => x.Id == id);
            BySettlementId.Remove(warehouse.SettlementId);
            return Task.CompletedTask;
        }
    }

    private sealed class MarketRepositoryFake : IMarketRepository
    {
        public Dictionary<int, Market> BySettlementId { get; } = new();
        private int _nextId = 1;

        public MarketRepositoryFake(Market? market = null)
        {
            if (market is not null)
            {
                BySettlementId[market.SettlementId] = market;
                _nextId = market.Id + 1;
            }
        }

        public Task<Market?> GetByIdAsync(int id) =>
            Task.FromResult(BySettlementId.Values.FirstOrDefault(x => x.Id == id));

        public Task<Market?> GetBySettlementIdAsync(int settlementId) =>
            Task.FromResult(BySettlementId.GetValueOrDefault(settlementId));

        public Task<int> SaveAsync(Market entity)
        {
            var id = entity.Id == 0 ? _nextId++ : entity.Id;
            var stored = new Market(id, entity.SettlementId);
            foreach (var offer in entity.Offers)
            {
                stored.RegisterProduct(offer.ProductTypeId, offer.CurrentPrice);
                stored.UpdateProductState(offer.ProductTypeId, offer.SupplyVolume, offer.DemandVolume);
            }

            BySettlementId[entity.SettlementId] = stored;
            return Task.FromResult(id);
        }

        public Task DeleteAsync(int id)
        {
            var market = BySettlementId.Values.First(x => x.Id == id);
            BySettlementId.Remove(market.SettlementId);
            return Task.CompletedTask;
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
                _items[group.Id == 0 ? _nextId++ : group.Id] = group;
                _nextId = Math.Max(_nextId, group.Id + 1);
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
}
