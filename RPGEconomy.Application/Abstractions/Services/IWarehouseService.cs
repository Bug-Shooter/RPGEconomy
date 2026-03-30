using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Common;

namespace RPGEconomy.Application.Abstractions.Services;

public interface IWarehouseService
{
    Task<Result<IReadOnlyList<InventoryItemDto>>> GetBySettlementIdAsync(int settlementId);
    Task<Result<IReadOnlyList<InventoryItemDto>>> SetStockItemAsync(int settlementId, int productTypeId, decimal quantity, string? quality);
}
