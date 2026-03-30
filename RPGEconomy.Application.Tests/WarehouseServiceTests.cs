using FluentAssertions;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Services;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.Resources;
using RPGEconomy.Domain.World;

namespace RPGEconomy.Application.Tests;

public class WarehouseServiceTests
{
    [Fact]
    public async Task GetBySettlementIdAsync_Should_Return_Inventory_With_Product_Names()
    {
        var settlement = new Settlement(3, 1, "Town");
        var warehouse = new Warehouse(7, settlement.Id);
        warehouse.AddItem(10, 4m, QualityGrade.Normal);
        var service = CreateService(
            settlements: [settlement],
            warehouses: [warehouse],
            products: [new ProductType(10, "Bread", "Desc", 1m, 1d)]);

        var result = await service.GetBySettlementIdAsync(settlement.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(item =>
            item.ProductTypeId == 10 &&
            item.ProductName == "Bread" &&
            item.Quantity == 4m &&
            item.Quality == "Normal");
    }

    [Fact]
    public async Task SetStockItemAsync_Should_Update_Quantity_And_Return_Updated_Snapshot()
    {
        var settlement = new Settlement(3, 1, "Town");
        var warehouse = new Warehouse(7, settlement.Id);
        warehouse.AddItem(10, 4m, QualityGrade.Normal);
        var warehouseRepository = new WarehouseRepositoryFake(warehouse);
        var service = CreateService(
            settlementRepository: new SettlementRepositoryFake(settlement),
            warehouseRepository: warehouseRepository,
            productRepository: new ProductTypeRepositoryFake(new ProductType(10, "Bread", "Desc", 1m, 1d)));

        var result = await service.SetStockItemAsync(settlement.Id, 10, 9m, "Normal");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(item => item.ProductTypeId == 10 && item.Quantity == 9m);
        warehouseRepository.BySettlementId[settlement.Id].Items.Should().ContainSingle(item => item.Quantity == 9m);
    }

    [Fact]
    public async Task SetStockItemAsync_Should_Remove_Item_When_Quantity_Is_Zero()
    {
        var settlement = new Settlement(3, 1, "Town");
        var warehouse = new Warehouse(7, settlement.Id);
        warehouse.AddItem(10, 4m, QualityGrade.Normal);
        var warehouseRepository = new WarehouseRepositoryFake(warehouse);
        var service = CreateService(
            settlementRepository: new SettlementRepositoryFake(settlement),
            warehouseRepository: warehouseRepository,
            productRepository: new ProductTypeRepositoryFake(new ProductType(10, "Bread", "Desc", 1m, 1d)));

        var result = await service.SetStockItemAsync(settlement.Id, 10, 0m, "Normal");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        warehouseRepository.BySettlementId[settlement.Id].Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SetStockItemAsync_Should_Default_Quality_To_Normal()
    {
        var settlement = new Settlement(3, 1, "Town");
        var warehouseRepository = new WarehouseRepositoryFake(new Warehouse(7, settlement.Id));
        var service = CreateService(
            settlementRepository: new SettlementRepositoryFake(settlement),
            warehouseRepository: warehouseRepository,
            productRepository: new ProductTypeRepositoryFake(new ProductType(10, "Bread", "Desc", 1m, 1d)));

        var result = await service.SetStockItemAsync(settlement.Id, 10, 2m, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(item => item.Quality == "Normal");
    }

    [Fact]
    public async Task SetStockItemAsync_Should_Fail_When_Settlement_Is_Missing()
    {
        var service = CreateService();

        var result = await service.SetStockItemAsync(99, 10, 2m, "Normal");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("не найден");
    }

    [Fact]
    public async Task SetStockItemAsync_Should_Fail_When_Warehouse_Is_Missing()
    {
        var settlement = new Settlement(3, 1, "Town");
        var service = CreateService(
            settlementRepository: new SettlementRepositoryFake(settlement),
            warehouseRepository: new WarehouseRepositoryFake(),
            productRepository: new ProductTypeRepositoryFake(new ProductType(10, "Bread", "Desc", 1m, 1d)));

        var result = await service.SetStockItemAsync(settlement.Id, 10, 2m, "Normal");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("склад");
    }

    [Fact]
    public async Task SetStockItemAsync_Should_Fail_When_Product_Is_Missing()
    {
        var settlement = new Settlement(3, 1, "Town");
        var service = CreateService(
            settlements: [settlement],
            warehouses: [new Warehouse(7, settlement.Id)]);

        var result = await service.SetStockItemAsync(settlement.Id, 10, 2m, "Normal");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("не найден");
    }

    [Fact]
    public async Task SetStockItemAsync_Should_Fail_For_Invalid_Quality()
    {
        var settlement = new Settlement(3, 1, "Town");
        var service = CreateService(
            settlements: [settlement],
            warehouses: [new Warehouse(7, settlement.Id)],
            products: [new ProductType(10, "Bread", "Desc", 1m, 1d)]);

        var result = await service.SetStockItemAsync(settlement.Id, 10, 2m, "Legendary");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Low, Normal, High");
    }

    private static WarehouseService CreateService(
        SettlementRepositoryFake? settlementRepository = null,
        WarehouseRepositoryFake? warehouseRepository = null,
        ProductTypeRepositoryFake? productRepository = null,
        Settlement[]? settlements = null,
        Warehouse[]? warehouses = null,
        ProductType[]? products = null)
    {
        settlementRepository ??= new SettlementRepositoryFake(settlements ?? []);
        warehouseRepository ??= new WarehouseRepositoryFake(warehouses ?? []);
        productRepository ??= new ProductTypeRepositoryFake(products ?? []);

        return new WarehouseService(settlementRepository, warehouseRepository, productRepository);
    }

    private sealed class SettlementRepositoryFake : ISettlementRepository
    {
        private readonly Dictionary<int, Settlement> _items = [];

        public SettlementRepositoryFake(params Settlement[] settlements)
        {
            foreach (var settlement in settlements)
                _items[settlement.Id] = new Settlement(settlement.Id, settlement.WorldId, settlement.Name);
        }

        public Task<Settlement?> GetByIdAsync(int id) => Task.FromResult(_items.GetValueOrDefault(id));

        public Task<IReadOnlyList<Settlement>> GetByWorldIdAsync(int worldId) =>
            Task.FromResult((IReadOnlyList<Settlement>)_items.Values.Where(x => x.WorldId == worldId).ToList().AsReadOnly());

        public Task<int> SaveAsync(Settlement entity)
        {
            _items[entity.Id] = new Settlement(entity.Id, entity.WorldId, entity.Name);
            return Task.FromResult(entity.Id);
        }

        public Task DeleteAsync(int id)
        {
            _items.Remove(id);
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
            BySettlementId[entity.SettlementId] = CloneWarehouse(entity, id);
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
                clone.SetItemQuantity(item.ProductTypeId, item.Quantity, QualityGrade.FromName(item.Quality));

            return clone;
        }
    }

    private sealed class ProductTypeRepositoryFake : IProductTypeRepository
    {
        private readonly Dictionary<int, ProductType> _items = [];

        public ProductTypeRepositoryFake(params ProductType[] products)
        {
            foreach (var product in products)
                _items[product.Id] = product;
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
    }
}
