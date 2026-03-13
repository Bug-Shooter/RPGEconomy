namespace RPGEconomy.Infrastructure.Persistence.Queries;

internal static class WarehouseQueries
{
    public const string GetById = """
        SELECT id, settlement_id AS "settlementId"
        FROM warehouses
        WHERE id = @Id
        """;

    public const string GetBySettlementId = """
        SELECT id, settlement_id AS "settlementId"
        FROM warehouses
        WHERE settlement_id = @SettlementId
        """;

    public const string Insert = """
        INSERT INTO warehouses (settlement_id)
        VALUES (@SettlementId)
        RETURNING id;
        """;

    public const string GetItems = """
        SELECT id, warehouse_id AS "warehouseId", product_type_id AS "productTypeId", quantity, quality
        FROM inventory_items
        WHERE warehouse_id = @WarehouseId
        """;

    public const string DeleteItems = """
        DELETE FROM inventory_items
        WHERE warehouse_id = @WarehouseId
        """;

    public const string InsertItem = """
        INSERT INTO inventory_items (warehouse_id, product_type_id, quantity, quality)
        VALUES (@WarehouseId, @ProductTypeId, @Quantity, @Quality)
        """;
}
