using FluentAssertions;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Services;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.World;

namespace RPGEconomy.Application.Tests;

public class BuildingServiceTests
{
    [Fact]
    public async Task CreateAsync_Should_Validate_Input_And_Persist_Building()
    {
        var service = new BuildingService(
            new BuildingRepositoryFake(),
            new SettlementRepositoryFake(new Settlement(1, 1, "Town")),
            new RecipeRepositoryFake(
                ProductionRecipe.Create("Bread", 1, [], [new RecipeIngredient(2, 1)]).Value!));

        var result = await service.CreateAsync(1, "Bakery", 1, 3, 2m);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Bakery");
        result.Value.InputReserveCoverageTicks.Should().Be(2m);
    }

    [Fact]
    public async Task ActivateAsync_Should_Reject_Missing_Building()
    {
        var service = new BuildingService(
            new BuildingRepositoryFake(),
            new SettlementRepositoryFake(),
            new RecipeRepositoryFake());

        var result = await service.ActivateAsync(42);

        result.IsSuccess.Should().BeFalse();
    }

    private sealed class BuildingRepositoryFake : IBuildingRepository
    {
        private readonly Dictionary<int, Building> _items = [];
        private int _nextId = 1;

        public Task<Building?> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<Building>> GetBySettlementIdAsync(int settlementId) =>
            Task.FromResult((IReadOnlyList<Building>)_items.Values.Where(x => x.SettlementId == settlementId).ToList().AsReadOnly());

        public Task<int> SaveAsync(Building entity)
        {
            var id = entity.Id == 0 ? _nextId++ : entity.Id;
            var clone = new Building(id, entity.Name, entity.SettlementId, entity.RecipeId, entity.WorkerCount, entity.IsActive, entity.InputReserveCoverageTicks);
            foreach (var item in entity.InputReserveItems)
                clone.ReceiveInputReserve(item.ProductTypeId, item.Quantity);
            _items[id] = clone;
            return Task.FromResult(id);
        }

        public Task DeleteAsync(int id)
        {
            _items.Remove(id);
            return Task.CompletedTask;
        }
    }

    private sealed class SettlementRepositoryFake : ISettlementRepository
    {
        private readonly Dictionary<int, Settlement> _items = [];

        public SettlementRepositoryFake(params Settlement[] items)
        {
            foreach (var item in items)
                _items[item.Id] = item;
        }

        public Task<Settlement?> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<Settlement>> GetByWorldIdAsync(int worldId) =>
            Task.FromResult((IReadOnlyList<Settlement>)_items.Values.Where(x => x.WorldId == worldId).ToList().AsReadOnly());

        public Task<int> SaveAsync(Settlement entity) => Task.FromResult(entity.Id);

        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class RecipeRepositoryFake : IProductionRecipeRepository
    {
        private readonly Dictionary<int, ProductionRecipe> _items = [];

        public RecipeRepositoryFake(params ProductionRecipe[] recipes)
        {
            var index = 1;
            foreach (var recipe in recipes)
            {
                var stored = new ProductionRecipe(index, recipe.Name, recipe.LaborDaysRequired);
                stored.Update(
                    recipe.Name,
                    recipe.LaborDaysRequired,
                    recipe.Inputs.Select(i => new RecipeIngredient(i.ProductTypeId, i.Quantity)),
                    recipe.Outputs.Select(o => new RecipeIngredient(o.ProductTypeId, o.Quantity)));
                _items[index] = stored;
                index++;
            }
        }

        public Task<ProductionRecipe?> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<ProductionRecipe>> GetAllAsync() =>
            Task.FromResult((IReadOnlyList<ProductionRecipe>)_items.Values.ToList().AsReadOnly());

        public Task<int> SaveAsync(ProductionRecipe entity) => Task.FromResult(entity.Id);

        public Task DeleteAsync(int id) => Task.CompletedTask;
    }
}
