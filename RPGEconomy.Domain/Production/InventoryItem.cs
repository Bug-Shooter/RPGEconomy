using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Domain.Production;

public class InventoryItem : Entity
{
    public int WarehouseId { get; private set; }
    public int ProductTypeId { get; private set; }
    public decimal Quantity { get; private set; }
    public string Quality { get; private set; }  // хранится как строка в БД

    // Dapper
    public InventoryItem(int id, int warehouseId, int productTypeId,
        decimal quantity, string quality) : base(id)
    {
        WarehouseId = warehouseId;
        ProductTypeId = productTypeId;
        Quantity = quantity;
        Quality = quality;
    }

    public static InventoryItem Create(int warehouseId, int productTypeId,
        decimal quantity, QualityGrade quality)
        => new(0, warehouseId, productTypeId, quantity, quality.Name);

    public QualityGrade QualityGrade => QualityGrade.FromName(Quality);

    internal void IncreaseQuantity(decimal amount) => Quantity += amount;
    internal void DecreaseQuantity(decimal amount) => Quantity -= amount;
    internal void SetQuantity(decimal amount) => Quantity = amount;
}
