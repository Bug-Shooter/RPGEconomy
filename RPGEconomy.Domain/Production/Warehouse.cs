using RPGEconomy.Domain.Common;
using RPGEconomy.Domain.Resources;

namespace RPGEconomy.Domain.Production;

public class Warehouse : AggregateRoot
{
    private readonly List<InventoryItem> _items = new();

    public int SettlementId { get; private set; }
    public IReadOnlyList<InventoryItem> Items => _items.AsReadOnly();

    // Dapper
    public Warehouse(int id, int settlementId) : base(id)
    {
        SettlementId = settlementId;
    }

    public static Warehouse Create(int settlementId)
        => new(0, settlementId);

    public Result AddItem(int productTypeId, decimal quantity, QualityGrade quality)
    {
        if (quantity <= 0m) return Result.Failure("Количество должно быть больше нуля");

        var existing = _items.FirstOrDefault(i =>
            i.ProductTypeId == productTypeId && i.Quality == quality.Name);

        if (existing is not null)
            existing.IncreaseQuantity(quantity);
        else
            _items.Add(InventoryItem.Create(Id, productTypeId, quantity, quality));

        return Result.Success();
    }

    public Result Withdraw(int productTypeId, decimal quantity, QualityGrade quality)
    {
        var item = _items.FirstOrDefault(i =>
            i.ProductTypeId == productTypeId && i.Quality == quality.Name);

        if (item is null || item.Quantity < quantity)
            return Result.Failure("Недостаточно товара на складе");

        item.DecreaseQuantity(quantity);
        if (item.Quantity == 0m) _items.Remove(item);

        return Result.Success();
    }

    public bool CanFulfill(IEnumerable<RecipeIngredient> ingredients) =>
        ingredients.All(ing =>
            _items.Any(i =>
                i.ProductTypeId == ing.ProductTypeId &&
                i.Quality == QualityGrade.Normal.Name &&
                i.Quantity >= ing.Quantity));

    internal void LoadItems(IEnumerable<InventoryItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }
}
