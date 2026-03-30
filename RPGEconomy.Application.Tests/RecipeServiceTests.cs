using FluentAssertions;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Application.Services;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Application.Tests;

public class RecipeServiceTests
{
    [Fact]
    public async Task CreateAsync_Should_Reject_Missing_Product_Type()
    {
        var repository = new RecipeRepositoryFake();
        var service = new RecipeService(
            repository,
            new ProductTypeRepositoryFake(new ProductType(1, "Grain", "Desc", 1m, 1)));

        var result = await service.CreateAsync(
            "Bread",
            1,
            [new RecipeIngredientDto(1, 2m)],
            [new RecipeIngredientDto(2, 1m)]);

        result.IsSuccess.Should().BeFalse();
        repository.Stored.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Invalid_Domain_Recipe()
    {
        var repository = new RecipeRepositoryFake();
        var service = new RecipeService(
            repository,
            new ProductTypeRepositoryFake(new ProductType(1, "Grain", "Desc", 1m, 1)));

        var result = await service.CreateAsync(
            "Broken",
            1,
            [new RecipeIngredientDto(1, 2m)],
            []);

        result.IsSuccess.Should().BeFalse();
        repository.Stored.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_Should_Save_Recipe_When_All_ProductTypes_Exist()
    {
        var repository = new RecipeRepositoryFake();
        var service = new RecipeService(
            repository,
            new ProductTypeRepositoryFake(
                new ProductType(1, "Grain", "Desc", 1m, 1),
                new ProductType(2, "Bread", "Desc", 2m, 1)));

        var result = await service.CreateAsync(
            "Bread",
            1,
            [new RecipeIngredientDto(1, 2m)],
            [new RecipeIngredientDto(2, 1m)]);

        result.IsSuccess.Should().BeTrue();
        repository.Stored.Values.Should().ContainSingle();
        repository.Stored.Values.Single().Inputs.Should().ContainSingle(x => x.ProductTypeId == 1 && x.Quantity == 2m);
    }

    [Fact]
    public async Task UpdateAsync_Should_Reject_Duplicate_Outputs()
    {
        var recipe = ProductionRecipe.Create(
            "Bread",
            1,
            [new RecipeIngredient(1, 2m)],
            [new RecipeIngredient(2, 1m)]).Value!;

        var repository = new RecipeRepositoryFake(recipe);
        var service = new RecipeService(
            repository,
            new ProductTypeRepositoryFake(
                new ProductType(1, "Grain", "Desc", 1m, 1),
                new ProductType(2, "Bread", "Desc", 2m, 1)));

        var result = await service.UpdateAsync(
            1,
            "Bread",
            1,
            [new RecipeIngredientDto(1, 2m)],
            [new RecipeIngredientDto(2, 1m), new RecipeIngredientDto(2, 0.5m)]);

        result.IsSuccess.Should().BeFalse();
        repository.Stored[1].Outputs.Should().ContainSingle();
    }

    [Fact]
    public async Task SearchByNameAsync_Should_Return_Matching_Recipes()
    {
        var matchingRecipe = ProductionRecipe.Create(
            "Bread",
            1,
            [new RecipeIngredient(1, 2m)],
            [new RecipeIngredient(2, 1m)]).Value!;
        var repository = new RecipeRepositoryFake(matchingRecipe)
        {
            SearchIds = [1]
        };
        var service = new RecipeService(
            repository,
            new ProductTypeRepositoryFake(
                new ProductType(1, "Grain", "Desc", 1m, 1),
                new ProductType(2, "Bread", "Desc", 2m, 1)));

        var result = await service.SearchByNameAsync("bre");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(x => x.Name == "Bread");
        repository.SearchCalls.Should().Be(1);
    }

    [Fact]
    public async Task SearchByNameAsync_Should_Fallback_To_GetAll_For_Whitespace()
    {
        var firstRecipe = ProductionRecipe.Create(
            "Bread",
            1,
            [new RecipeIngredient(1, 2m)],
            [new RecipeIngredient(2, 1m)]).Value!;
        var secondRecipe = ProductionRecipe.Create(
            "Ale",
            1,
            [new RecipeIngredient(1, 1m)],
            [new RecipeIngredient(2, 1m)]).Value!;
        var repository = new RecipeRepositoryFake(firstRecipe, secondRecipe);
        var service = new RecipeService(
            repository,
            new ProductTypeRepositoryFake(
                new ProductType(1, "Grain", "Desc", 1m, 1),
                new ProductType(2, "Bread", "Desc", 2m, 1)));

        var result = await service.SearchByNameAsync("  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        repository.GetAllCalls.Should().Be(1);
        repository.SearchCalls.Should().Be(0);
    }

    private sealed class RecipeRepositoryFake : IProductionRecipeRepository
    {
        public Dictionary<int, ProductionRecipe> Stored { get; } = [];
        private int _nextId = 1;
        public IReadOnlyList<int> SearchIds { get; set; } = [];
        public int GetAllCalls { get; private set; }
        public int SearchCalls { get; private set; }

        public RecipeRepositoryFake(params ProductionRecipe[] recipes)
        {
            foreach (var recipe in recipes)
            {
                Stored[_nextId] = Clone(_nextId, recipe);
                _nextId++;
            }
        }

        public Task<ProductionRecipe?> GetByIdAsync(int id) => Task.FromResult(Stored.GetValueOrDefault(id));

        public Task<IReadOnlyList<ProductionRecipe>> GetAllAsync() =>
            Task.FromResult((IReadOnlyList<ProductionRecipe>)GetAll());

        public Task<IReadOnlyList<ProductionRecipe>> SearchByNameAsync(string search)
        {
            SearchCalls++;
            var matches = SearchIds.Count == 0
                ? Stored.Values.Where(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList()
                : SearchIds.Select(id => Stored[id]).ToList();
            return Task.FromResult((IReadOnlyList<ProductionRecipe>)matches.AsReadOnly());
        }

        public Task<int> SaveAsync(ProductionRecipe entity)
        {
            var id = entity.Id == 0 ? _nextId++ : entity.Id;
            Stored[id] = Clone(id, entity);
            return Task.FromResult(id);
        }

        public Task DeleteAsync(int id)
        {
            Stored.Remove(id);
            return Task.CompletedTask;
        }

        private static ProductionRecipe Clone(int id, ProductionRecipe entity)
        {
            var clone = new ProductionRecipe(id, entity.Name, entity.LaborDaysRequired);
            clone.Update(
                entity.Name,
                entity.LaborDaysRequired,
                entity.Inputs.Select(i => new RecipeIngredient(i.ProductTypeId, i.Quantity)),
                entity.Outputs.Select(o => new RecipeIngredient(o.ProductTypeId, o.Quantity)));
            return clone;
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

        public Task<IReadOnlyList<ProductType>> SearchByNameAsync(string search) =>
            Task.FromResult((IReadOnlyList<ProductType>)_items.Values
                .Where(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly());

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

        private IReadOnlyList<ProductionRecipe> GetAll()
        {
            GetAllCalls++;
            return Stored.Values.ToList().AsReadOnly();
        }
    }
}
