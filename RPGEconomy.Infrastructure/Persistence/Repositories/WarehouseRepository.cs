using Dapper;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Domain.Production;
using RPGEconomy.Infrastructure.Persistence.Queries;
using System.Data;

namespace RPGEconomy.Infrastructure.Persistence.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly IDbConnectionFactory _factory;

    public WarehouseRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<Warehouse?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();

        var warehouse = await conn.QueryFirstOrDefaultAsync<Warehouse>(
            WarehouseQueries.GetById, new { Id = id });

        if (warehouse is null) return null;
        await LoadItemsAsync(conn, warehouse);
        return warehouse;
    }

    public async Task<Warehouse?> GetBySettlementIdAsync(int settlementId)
    {
        using var conn = _factory.Create();

        var warehouse = await conn.QueryFirstOrDefaultAsync<Warehouse>(
            WarehouseQueries.GetBySettlementId, new { SettlementId = settlementId });

        if (warehouse is null) return null;
        await LoadItemsAsync(conn, warehouse);
        return warehouse;
    }

    public async Task<int> SaveAsync(Warehouse warehouse)
    {
        using var conn = _factory.Create();

        int warehouseId;

        if (warehouse.IsNew)
        {
            warehouseId = await conn.ExecuteScalarAsync<int>(
                WarehouseQueries.Insert, new { warehouse.SettlementId });
        }
        else
        {
            warehouseId = warehouse.Id;
        }

        // Пересохраняем все items (delete + insert)
        await conn.ExecuteAsync(WarehouseQueries.DeleteItems, new { WarehouseId = warehouseId });

        if (warehouse.Items.Any())
        {
            await conn.ExecuteAsync(WarehouseQueries.InsertItem,
                warehouse.Items.Select(i => new
                {
                    WarehouseId = warehouseId,
                    i.ProductTypeId,
                    i.Quantity,
                    i.Quality
                }));
        }

        return warehouseId;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync(WarehouseQueries.DeleteItems, new { WarehouseId = id });
        await conn.ExecuteAsync("DELETE FROM warehouses WHERE id = @Id", new { Id = id });
    }

    private async Task LoadItemsAsync(IDbConnection conn, Warehouse warehouse)
    {
        var items = await conn.QueryAsync<InventoryItem>(
            WarehouseQueries.GetItems, new { WarehouseId = warehouse.Id });
        warehouse.LoadItems(items);
    }
}
