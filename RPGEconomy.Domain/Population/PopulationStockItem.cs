using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Population;

public class PopulationStockItem : Entity
{
    public int PopulationGroupId { get; private set; }
    public int ProductTypeId { get; private set; }
    public decimal Quantity { get; private set; }

    public PopulationStockItem(int id, int populationGroupId, int productTypeId, decimal quantity) : base(id)
    {
        PopulationGroupId = populationGroupId;
        ProductTypeId = productTypeId;
        Quantity = quantity;
    }

    public static Result<PopulationStockItem> Create(int populationGroupId, int productTypeId, decimal quantity)
    {
        if (productTypeId <= 0)
            return Result<PopulationStockItem>.Failure("Product type id must be greater than zero");

        if (quantity <= 0m)
            return Result<PopulationStockItem>.Failure("Stock quantity must be greater than zero");

        return Result<PopulationStockItem>.Success(new PopulationStockItem(0, populationGroupId, productTypeId, quantity));
    }

    internal void IncreaseQuantity(decimal quantity) => Quantity += quantity;
    internal void DecreaseQuantity(decimal quantity) => Quantity -= quantity;
}
