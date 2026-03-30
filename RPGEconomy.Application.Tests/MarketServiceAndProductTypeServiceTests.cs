using FluentAssertions;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Services;
using RPGEconomy.Domain.Markets;
using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Application.Tests;

public class MarketServiceAndProductTypeServiceTests
{
    [Fact]
    public async Task RegisterProductAsync_Should_Save_Market_When_Input_Is_Valid()
    {
        var market = Market.Create(1);
        var marketRepo = new MarketRepositoryFake(market);
        var productRepo = new ProductTypeRepositoryFake(new ProductType(1, "Bread", "Desc", 10m, 1));
        var service = new MarketService(marketRepo, productRepo);

        var result = await service.RegisterProductAsync(1, 1, 12m);

        result.IsSuccess.Should().BeTrue();
        marketRepo.Stored.Offers.Should().ContainSingle(x => x.ProductTypeId == 1);
    }

    [Fact]
    public async Task GetProductAsync_Should_Return_NotFound_When_Product_Is_Not_Registered()
    {
        var marketRepo = new MarketRepositoryFake(Market.Create(1));
        var productRepo = new ProductTypeRepositoryFake();
        var service = new MarketService(marketRepo, productRepo);

        var result = await service.GetProductAsync(1, 42);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("не найден");
    }

    [Fact]
    public async Task UpdateProductStateAsync_Should_Update_Supply_Demand_And_Return_Dto()
    {
        var market = Market.Create(1);
        market.RegisterProduct(1, 12m);
        var marketRepo = new MarketRepositoryFake(market);
        var productRepo = new ProductTypeRepositoryFake(new ProductType(1, "Bread", "Desc", 10m, 1));
        var service = new MarketService(marketRepo, productRepo);

        var result = await service.UpdateProductStateAsync(1, 1, 5m, 9m);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Supply.Should().Be(5m);
        result.Value.Demand.Should().Be(9m);
        result.Value.ProductName.Should().Be("Bread");
    }

    [Fact]
    public async Task UpdateAsync_Should_Validate_Name_And_BasePrice_For_ProductType()
    {
        var repo = new ProductTypeRepositoryFake(new ProductType(1, "Bread", "Desc", 10m, 1));
        var service = new ProductTypeService(repo);

        var result = await service.UpdateAsync(1, "", "Desc", 0m, 1);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByNameAsync_Should_Return_Matching_ProductTypes()
    {
        var repo = new ProductTypeRepositoryFake(
            new ProductType(1, "Bread", "Desc", 10m, 1),
            new ProductType(2, "Brew", "Desc", 11m, 1))
        {
            SearchResults =
            [
                new ProductType(1, "Bread", "Desc", 10m, 1)
            ]
        };
        var service = new ProductTypeService(repo);

        var result = await service.SearchByNameAsync("bre");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(x => x.Name == "Bread");
        repo.SearchCalls.Should().Be(1);
    }

    [Fact]
    public async Task SearchByNameAsync_Should_Fallback_To_GetAll_For_Whitespace()
    {
        var repo = new ProductTypeRepositoryFake(
            new ProductType(1, "Bread", "Desc", 10m, 1),
            new ProductType(2, "Ale", "Desc", 8m, 1));
        var service = new ProductTypeService(repo);

        var result = await service.SearchByNameAsync("   ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        repo.GetAllCalls.Should().Be(1);
        repo.SearchCalls.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_Should_Reject_ProductType_That_Is_Still_In_Use()
    {
        var repo = new ProductTypeRepositoryFake(new ProductType(1, "Bread", "Desc", 10m, 1))
        {
            IsInUse = true
        };
        var service = new ProductTypeService(repo);

        var result = await service.DeleteAsync(1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Нельзя удалить");
    }

    private sealed class MarketRepositoryFake : IMarketRepository
    {
        public Market Stored { get; private set; }

        public MarketRepositoryFake(Market stored)
            => Stored = stored;

        public Task<Market?> GetByIdAsync(int id) => Task.FromResult(Stored.Id == id ? Stored : null);

        public Task<Market?> GetBySettlementIdAsync(int settlementId) =>
            Task.FromResult(Stored.SettlementId == settlementId ? Stored : null);

        public Task<int> SaveAsync(Market entity)
        {
            Stored = entity;
            return Task.FromResult(entity.Id == 0 ? 1 : entity.Id);
        }

        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class ProductTypeRepositoryFake : IProductTypeRepository
    {
        private readonly Dictionary<int, ProductType> _items = [];

        public bool IsInUse { get; set; }
        public IReadOnlyList<ProductType> SearchResults { get; set; } = [];
        public int GetAllCalls { get; private set; }
        public int SearchCalls { get; private set; }

        public ProductTypeRepositoryFake(params ProductType[] items)
        {
            foreach (var item in items)
                _items[item.Id] = item;
        }

        public Task<ProductType?> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<ProductType>> GetAllAsync() =>
            Task.FromResult((IReadOnlyList<ProductType>)GetAll());

        public Task<IReadOnlyList<ProductType>> SearchByNameAsync(string search)
        {
            SearchCalls++;
            if (SearchResults.Count > 0)
                return Task.FromResult(SearchResults);

            return Task.FromResult((IReadOnlyList<ProductType>)_items.Values
                .Where(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly());
        }

        public Task<ProductType?> GetByNameAsync(string name) =>
            Task.FromResult(_items.Values.FirstOrDefault(x => x.Name == name));

        public Task<int> SaveAsync(ProductType entity)
        {
            _items[entity.Id == 0 ? 1 : entity.Id] = entity;
            return Task.FromResult(entity.Id == 0 ? 1 : entity.Id);
        }

        public Task DeleteAsync(int id)
        {
            _items.Remove(id);
            return Task.CompletedTask;
        }

        public Task<bool> IsInUseAsync(int id) => Task.FromResult(IsInUse);

        private IReadOnlyList<ProductType> GetAll()
        {
            GetAllCalls++;
            return _items.Values.ToList().AsReadOnly();
        }
    }
}
