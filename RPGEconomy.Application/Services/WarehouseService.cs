using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Application.Abstractions.Services;
using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Production;
using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Application.Services;

public class WarehouseService : IWarehouseService
{
    private readonly ISettlementRepository _settlementRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IProductTypeRepository _productTypeRepository;

    public WarehouseService(
        ISettlementRepository settlementRepository,
        IWarehouseRepository warehouseRepository,
        IProductTypeRepository productTypeRepository)
    {
        _settlementRepository = settlementRepository;
        _warehouseRepository = warehouseRepository;
        _productTypeRepository = productTypeRepository;
    }

    public async Task<Result<IReadOnlyList<InventoryItemDto>>> GetBySettlementIdAsync(int settlementId)
    {
        var warehouseResult = await GetWarehouseAsync(settlementId);
        if (!warehouseResult.IsSuccess)
            return Result<IReadOnlyList<InventoryItemDto>>.Failure(warehouseResult.Error!);

        var productNames = await GetProductNamesAsync();
        return Result<IReadOnlyList<InventoryItemDto>>.Success(
            InventoryItemMappings.Map(warehouseResult.Value!.Items, productNames));
    }

    public async Task<Result<IReadOnlyList<InventoryItemDto>>> SetStockItemAsync(
        int settlementId,
        int productTypeId,
        decimal quantity,
        string? quality)
    {
        var warehouseResult = await GetWarehouseAsync(settlementId);
        if (!warehouseResult.IsSuccess)
            return Result<IReadOnlyList<InventoryItemDto>>.Failure(warehouseResult.Error!);

        var productType = await _productTypeRepository.GetByIdAsync(productTypeId);
        if (productType is null)
            return Result<IReadOnlyList<InventoryItemDto>>.Failure($"Товар с Id {productTypeId} не найден");

        var requestedQuality = string.IsNullOrWhiteSpace(quality) ? QualityGrade.Normal.Name : quality.Trim();
        if (!QualityGrade.TryFromName(requestedQuality, out var qualityGrade))
            return Result<IReadOnlyList<InventoryItemDto>>.Failure("Качество должно быть одним из значений: Low, Normal, High");

        var warehouse = warehouseResult.Value!;
        var updateResult = warehouse.SetItemQuantity(productTypeId, quantity, qualityGrade);
        if (!updateResult.IsSuccess)
            return Result<IReadOnlyList<InventoryItemDto>>.Failure(updateResult.Error!);

        await _warehouseRepository.SaveAsync(warehouse);

        var productNames = await GetProductNamesAsync();
        return Result<IReadOnlyList<InventoryItemDto>>.Success(
            InventoryItemMappings.Map(warehouse.Items, productNames));
    }

    private async Task<Result<Warehouse>> GetWarehouseAsync(int settlementId)
    {
        var settlement = await _settlementRepository.GetByIdAsync(settlementId);
        if (settlement is null)
            return Result<Warehouse>.Failure($"Поселение с Id {settlementId} не найдено");

        var warehouse = await _warehouseRepository.GetBySettlementIdAsync(settlementId);
        if (warehouse is null)
            return Result<Warehouse>.Failure($"Для поселения с Id {settlementId} не найден склад");

        return Result<Warehouse>.Success(warehouse);
    }

    private async Task<IReadOnlyDictionary<int, string>> GetProductNamesAsync()
    {
        var products = await _productTypeRepository.GetAllAsync();
        return products.ToDictionary(product => product.Id, product => product.Name);
    }
}
