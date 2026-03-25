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
        var productRepo = new ProductTypeRepositoryFake(new ProductType(1, "Bread", "Desc", 10, 1));
        var service = new MarketService(marketRepo, productRepo);

        var result = await service.RegisterProductAsync(1, 1, 12);

        result.IsSuccess.Should().BeTrue();
        marketRepo.Stored.Offers.Should().ContainSingle(x => x.ProductTypeId == 1);
    }

    [Fact]
    public async Task UpdateAsync_Should_Validate_Name_And_BasePrice_For_ProductType()
    {
        var repo = new ProductTypeRepositoryFake(new ProductType(1, "Bread", "Desc", 10, 1));
        var service = new ProductTypeService(repo);

        var result = await service.UpdateAsync(1, "", "Desc", 0, 1);

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
