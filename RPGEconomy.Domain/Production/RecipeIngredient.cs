namespace RPGEconomy.Domain.Production;
public class RecipeIngredient
{
    public int ProductTypeId { get; }
    public decimal Quantity { get; }

    public RecipeIngredient(int productTypeId, decimal quantity)
    {
        ProductTypeId = productTypeId;
        Quantity = quantity;
    }
}
