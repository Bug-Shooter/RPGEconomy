using FluentAssertions;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Application.Services;
using RPGEconomy.Domain.Population;
using RPGEconomy.Domain.Resources;
using RPGEconomy.Domain.World;

namespace RPGEconomy.Application.Tests;

public class PopulationGroupServiceTests
{
    [Fact]
    public async Task CreateAsync_Should_Save_Group_And_Sync_Settlement_Population()
    {
        var settlement = new Settlement(1, 1, "Town", 0);
        var settlementRepo = new SettlementRepositoryFake(settlement);
        var service = new PopulationGroupService(
            new PopulationGroupRepositoryFake(),
            settlementRepo,
            new ProductTypeRepositoryFake(new ProductType(10, "Bread", "Desc", 10m, 1)));

        var result = await service.CreateAsync(
            settlement.Id,
            "Peasants",
            50,
            3m,
            [new ConsumptionProfileItemDto(10, 0.5m)]);

        result.IsSuccess.Should().BeTrue();
        settlementRepo.Entities[settlement.Id].Population.Should().Be(50);
        result.Value!.ReserveCoverageTicks.Should().Be(3m);
        result.Value!.ConsumptionProfile.Should().ContainSingle(x => x.ProductTypeId == 10 && x.AmountPerPersonPerTick == 0.5m);
    }

    [Fact]
    public async Task DeleteAsync_Should_Recalculate_Settlement_Population()
    {
        var settlement = new Settlement(1, 1, "Town", 100);
        var firstGroup = new PopulationGroup(1, 1, "Peasants", 40);
        var secondGroup = new PopulationGroup(2, 1, "Workers", 60);
        var settlementRepo = new SettlementRepositoryFake(settlement);
        var groupRepo = new PopulationGroupRepositoryFake(firstGroup, secondGroup);
        var service = new PopulationGroupService(
            groupRepo,
            settlementRepo,
            new ProductTypeRepositoryFake());

        await service.DeleteAsync(secondGroup.Id);

        settlementRepo.Entities[settlement.Id].Population.Should().Be(40);
    }

    private sealed class PopulationGroupRepositoryFake : IPopulationGroupRepository
    {
        private readonly Dictionary<int, PopulationGroup> _items = [];
        private int _nextId = 1;

        public PopulationGroupRepositoryFake(params PopulationGroup[] items)
        {
            foreach (var item in items)
            {
                var id = item.Id == 0 ? _nextId++ : item.Id;
                _items[id] = Clone(item, id);
                _nextId = Math.Max(_nextId, id + 1);
            }
        }

        public Task<PopulationGroup?> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<PopulationGroup>> GetBySettlementIdAsync(int settlementId) =>
            Task.FromResult((IReadOnlyList<PopulationGroup>)_items.Values.Where(x => x.SettlementId == settlementId).ToList().AsReadOnly());

        public Task<int> SaveAsync(PopulationGroup entity)
        {
            var id = entity.Id == 0 ? _nextId++ : entity.Id;
            _items[id] = Clone(entity, id);
            return Task.FromResult(id);
        }

        public Task DeleteAsync(int id)
        {
            _items.Remove(id);
            return Task.CompletedTask;
        }

        private static PopulationGroup Clone(PopulationGroup entity, int id)
        {
            var clone = new PopulationGroup(id, entity.SettlementId, entity.Name, entity.PopulationSize, entity.ReserveCoverageTicks);
            clone.Update(
                entity.Name,
                entity.PopulationSize,
                entity.ReserveCoverageTicks,
                entity.ConsumptionProfile.Select(item => (item.ProductTypeId, item.AmountPerPersonPerTick)));
            foreach (var item in entity.StockItems)
                clone.ReceiveReserveStock(item.ProductTypeId, item.Quantity);
            return clone;
        }
    }

    private sealed class SettlementRepositoryFake : ISettlementRepository
    {
        public Dictionary<int, Settlement> Entities { get; } = [];

        public SettlementRepositoryFake(params Settlement[] items)
        {
            foreach (var item in items)
                Entities[item.Id] = new Settlement(item.Id, item.WorldId, item.Name, item.Population);
        }

        public Task<Settlement?> GetByIdAsync(int id) => Task.FromResult(Entities.GetValueOrDefault(id));

        public Task<IReadOnlyList<Settlement>> GetByWorldIdAsync(int worldId) =>
            Task.FromResult((IReadOnlyList<Settlement>)Entities.Values.Where(x => x.WorldId == worldId).ToList().AsReadOnly());

        public Task<int> SaveAsync(Settlement entity)
        {
            Entities[entity.Id] = new Settlement(entity.Id, entity.WorldId, entity.Name, entity.Population);
            return Task.FromResult(entity.Id);
        }

        public Task DeleteAsync(int id)
        {
            Entities.Remove(id);
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

        public Task<int> SaveAsync(ProductType entity) => Task.FromResult(entity.Id);

        public Task DeleteAsync(int id)
        {
            _items.Remove(id);
            return Task.CompletedTask;
        }
    }
}
