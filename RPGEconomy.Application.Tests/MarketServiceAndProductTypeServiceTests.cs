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

        var result = await service.UpdateProductStateAsync(1, 1, 5, 9);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Supply.Should().Be(5);
        result.Value.Demand.Should().Be(9);
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
        private readonly Dictionary<int, ProductType> _items = new();

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
            _items[entity.Id == 0 ? 1 : entity.Id] = entity;
            return Task.FromResult(entity.Id == 0 ? 1 : entity.Id);
        }

        public Task DeleteAsync(int id)
        {
            _items.Remove(id);
            return Task.CompletedTask;
        }
    }
}
