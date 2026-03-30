using Dapper;
using RPGEconomy.Application.Abstractions.Repositories;
using RPGEconomy.Domain.Production;
using RPGEconomy.Infrastructure.Persistence.Queries;
using System.Data;
using System.Transactions;

namespace RPGEconomy.Infrastructure.Persistence.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly IDbConnectionFactory _factory;

    public WarehouseRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<Warehouse?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();

        var warehouse = await conn.QueryFirstOrDefaultAsync<Warehouse>(WarehouseQueries.GetById, new { Id = id });
        if (warehouse is null)
            return null;

        await LoadItemsAsync(conn, warehouse);
        return warehouse;
    }

    public async Task<Warehouse?> GetBySettlementIdAsync(int settlementId)
    {
        using var conn = _factory.Create();

        var warehouse = await conn.QueryFirstOrDefaultAsync<Warehouse>(
            WarehouseQueries.GetBySettlementId,
            new { SettlementId = settlementId });
        if (warehouse is null)
            return null;

        await LoadItemsAsync(conn, warehouse);
        return warehouse;
    }

    public async Task<int> SaveAsync(Warehouse warehouse)
    {
        using var conn = _factory.Create();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var useLocalTransaction = Transaction.Current is null;
        using var tx = useLocalTransaction ? conn.BeginTransaction() : null;

        int warehouseId;
        if (warehouse.IsNew)
        {
            warehouseId = await conn.ExecuteScalarAsync<int>(WarehouseQueries.Insert, new { warehouse.SettlementId }, tx);
        }
        else
        {
            warehouseId = warehouse.Id;
        }

        await conn.ExecuteAsync(WarehouseQueries.DeleteItems, new { WarehouseId = warehouseId }, tx);

        if (warehouse.Items.Any())
        {
            await conn.ExecuteAsync(
                WarehouseQueries.InsertItem,
                warehouse.Items.Select(item => new
                {
                    WarehouseId = warehouseId,
                    item.ProductTypeId,
                    item.Quantity,
                    item.Quality
                }),
                tx);
        }

        tx?.Commit();
        return warehouseId;
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var useLocalTransaction = Transaction.Current is null;
        using var tx = useLocalTransaction ? conn.BeginTransaction() : null;
        await conn.ExecuteAsync(WarehouseQueries.Delete, new { Id = id }, tx);
        tx?.Commit();
    }

    private static async Task LoadItemsAsync(IDbConnection conn, Warehouse warehouse)
    {
        var items = await conn.QueryAsync<InventoryItem>(WarehouseQueries.GetItems, new { WarehouseId = warehouse.Id });
        warehouse.LoadItems(items);
    }
}
