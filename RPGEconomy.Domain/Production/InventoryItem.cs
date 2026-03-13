using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Domain.Production;

public class InventoryItem : Entity
{
    public int WarehouseId { get; private set; }
    public int ProductTypeId { get; private set; }
    public int Quantity { get; private set; }
    public string Quality { get; private set; }  // хранится как строка в БД

    // Dapper
    public InventoryItem(int id, int warehouseId, int productTypeId,
        int quantity, string quality) : base(id)
    {
        WarehouseId = warehouseId;
        ProductTypeId = productTypeId;
        Quantity = quantity;
        Quality = quality;
    }

    public static InventoryItem Create(int warehouseId, int productTypeId,
        int quantity, QualityGrade quality)
        => new(0, warehouseId, productTypeId, quantity, quality.Name);

    public QualityGrade QualityGrade => QualityGrade.FromName(Quality);

    internal void IncreaseQuantity(int amount) => Quantity += amount;
    internal void DecreaseQuantity(int amount) => Quantity -= amount;
}
