namespace RPGEconomy.Domain.Production;
public class RecipeIngredient
{
    public int ProductTypeId { get; }
    public int Quantity { get; }

    public RecipeIngredient(int productTypeId, int quantity)
    {
        ProductTypeId = productTypeId;
        Quantity = quantity;
    }
}
