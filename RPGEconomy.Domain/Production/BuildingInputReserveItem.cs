using RPGEconomy.Domain.Common;

namespace RPGEconomy.Domain.Production;

public class BuildingInputReserveItem : Entity
{
    public int BuildingId { get; private set; }
    public int ProductTypeId { get; private set; }
    public decimal Quantity { get; private set; }

    public BuildingInputReserveItem(int id, int buildingId, int productTypeId, decimal quantity) : base(id)
    {
        BuildingId = buildingId;
        ProductTypeId = productTypeId;
        Quantity = quantity;
    }

    public static Result<BuildingInputReserveItem> Create(int buildingId, int productTypeId, decimal quantity)
    {
        if (productTypeId <= 0)
            return Result<BuildingInputReserveItem>.Failure("Product type id must be greater than zero");

        if (quantity <= 0m)
            return Result<BuildingInputReserveItem>.Failure("Input reserve quantity must be greater than zero");

        return Result<BuildingInputReserveItem>.Success(new BuildingInputReserveItem(0, buildingId, productTypeId, quantity));
    }

    internal void IncreaseQuantity(decimal quantity) => Quantity += quantity;
    internal void DecreaseQuantity(decimal quantity) => Quantity -= quantity;
}
